using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Tarmi.App.Infrastructure;
using Tarmi.Devices.Thermofisher.Instrument;
using Tarmi.Devices.Thermofisher.Instrument.Types;
using Tarmi.Imaging.Common;
using Tarmi.Models;
using UnitsNet;

namespace Tarmi.VirtualDevices.Implementation;

public sealed class ElectronBeamMode : StageControllingModeBase, IElectronBeamMode
{
    private CancellationTokenSource? _grabbingTokenSource;
    private readonly ILimits _limits;
    private readonly BehaviorSubject<bool> _grabbingActive = new(false);

    protected override string CreateActivityName([CallerMemberName] string methodName = "")
        => $"{nameof(ElectronBeamMode)}::{methodName}";

    public IObservable<BeamState> Beam => _instrument.Beam;
    public IObservable<DetectorState> Detector => _instrument.Detector;
    public IObservable<ImageFilterState> ImageFilter => _instrument.ImageFilter;
    public Length HorizontalFieldWidth => _instrument.CurrentBeamState.HorizontalFieldWidth;
    public Length VerticalFieldWidth => _instrument.CurrentBeamState.VerticalFieldWidth;
    public BeamState CurrentBeamState => _instrument.CurrentBeamState;
    public DetectorState CurrentDetectorState => _instrument.CurrentDetectorState;
    public ImageFilterState CurrentImageFilterState => _instrument.CurrentImageFilterState;
    public ElectricCurrent[] AvailableBeamCurrents => _instrument.CurrentBeamState.BeamCurrents;
    public StageState StageState => _instrument.CurrentStageState;
    public IObservable<StageState> Stage => _instrument.Stage;
    public IObservable<bool> GrabbingActiveChanges => _grabbingActive.AsObservable().DistinctUntilChanged();
    public bool IsGrabbingActive => _grabbingActive.Value;

    public IObservable<ImageWithMetadata> Image =>
        _instrument.ImageStream.Select(im => im with
        {
            Coordinates = im.Coordinates with
            {
                CameraView = _safeStageControlling.ActiveCameraView
            }
        }).AsObservable();

    public ElectronBeamMode(IInstrument instrument, ISafeStageControlling safeStageControlling, ILimits limits)
        : base(instrument, safeStageControlling)
    {
        _limits = limits;
    }

    public async Task StopMovementsAsync(CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        await _instrument.StageStopMoving();
    }

    public async Task EnableAsync(CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        await _instrument.SwitchMode(InstrumentMode.Sem);
        _ = await SwitchStageViewAsync(StageCameraView.SEM);
    }

    public async Task DisableAsync(CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        if (_grabbingTokenSource is not null)
        {
            await _grabbingTokenSource.CancelAsync();
        }

        await _instrument.SwitchMode(InstrumentMode.StageOnly);
    }

    public Task FocusAsync(double change, CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        var originalDistance = _instrument.GetBeamFreeWorkingDistance();
        var range = _limits.GetFocusRangeForActiveBeam();
        var newDistance = UnitMath.Clamp(originalDistance + change * range.Step, range.Min, range.Max);
        cancellationToken.ThrowIfCancellationRequested();
        _instrument.SetBeamFreeWorkingDistance(newDistance);

        return Task.CompletedTask;
    }

    public Task<ImageWithMetadata> GrabImageAsync()
    {
        return Task.Run(() =>
        {
            using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

            var image = _instrument.GrabImage();
            image = image with { Coordinates = image.Coordinates with { CameraView = _safeStageControlling.ActiveCameraView } };
            return image;
        });
    }

    // TODO: fix -> this is not async
    public Task StartGrabbingAsync(CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        if (_grabbingTokenSource is not null)
        {
            throw new InvalidOperationException("Grabbing was already started.");
        }
        _grabbingTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _grabbingTokenSource.Token;
        _ = _grabbingTokenSource.Token.Register(() =>
        {
            using var cts = _grabbingTokenSource;
            _grabbingTokenSource = null;
            _grabbingActive.OnNext(false);
        });
        _instrument.SetBeamOn(true);
        _instrument.StartImageStream(token);
        _grabbingActive.OnNext(true);
        return Task.CompletedTask;
    }

    public void StopGrabbing() => _grabbingTokenSource?.Cancel();

    public void SetBeamCurrentIndex(int currentIndex)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());
        if (currentIndex != -1)
        {
            _instrument.SetBeamCurrentIndex(currentIndex);
        }
    }

    public async Task RestoreImageState(ImageMetadata imageMetadata, CancellationToken cancellation)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        if (
            imageMetadata.GetSource() != StageCameraView.SEM ||
            imageMetadata.FeiXmlMetadata == null
        )
        {
            return;
        }

        _ = await _safeStageControlling.MoveStageAsync(imageMetadata.GetStagePosition(), cancellation);

        var imageSize = imageMetadata.FeiXmlMetadata!.ScanSettings!.ScanSize;
        var resolution = new Resolution { Width = imageSize.Width, Height = imageSize.Height, Depth = 8 };
        _instrument.SetResolution(resolution);

        var hfw = Length.FromMeters(imageMetadata.FeiXmlMetadata!.Optics!.ScanFieldOfView!.X);
        _instrument.SetHorizontalFieldWidth(hfw);

        var wd = Length.FromMeters(imageMetadata.FeiXmlMetadata!.Optics!.WorkingDistance!.Value);
        _instrument.SetBeamFreeWorkingDistance(wd);
    }

    public IDisposable UseReducedArea(RatioRectangle rectangle, Duration dwellTime, ImageFilterType imageFilterType, int frames, int lineIntegration)
    {
        var currentDwellTime = _instrument.CurrentBeamState.DwellTime;
        var currentLineIntegration = _instrument.CurrentBeamState.LineIntegration;
        var currentImageFilterType = _instrument.CurrentImageFilterState.Type;
        var currentFrames = _instrument.CurrentImageFilterState.Frames;

        var beamRectangle = new BeamCoordinatesRectangle
        {
            Left = rectangle.Left.DecimalFractions,
            Right = rectangle.Right.DecimalFractions,
            Top = rectangle.Top.DecimalFractions,
            Bottom = rectangle.Bottom.DecimalFractions
        };
        _instrument.SetReducedArea(beamRectangle, dwellTime, imageFilterType, frames, lineIntegration);

        return Disposable.Create(() => _instrument.SetFullFrameMode(currentDwellTime, currentImageFilterType, currentFrames, currentLineIntegration));
    }

    public IDisposable UseFullFrameSettings(Duration dwellTime, ImageFilterType imageFilterType, int frames, int lineIntegration)
    {
        var currentDwellTime = _instrument.CurrentBeamState.DwellTime;
        var currentLineIntegration = _instrument.CurrentBeamState.LineIntegration;
        var currentImageFilterType = _instrument.CurrentImageFilterState.Type;
        var currentFrames = _instrument.CurrentImageFilterState.Frames;

        _instrument.SetFullFrameMode(dwellTime, imageFilterType, frames, lineIntegration);
        
        return Disposable.Create(() => _instrument.SetFullFrameMode(currentDwellTime, currentImageFilterType, currentFrames, currentLineIntegration));
    }

    public void SetBeamRotation(Angle rotation)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());
        _instrument.SetBeamRotation(rotation);
    }
}
