using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Tarmi.App.Infrastructure;
using Tarmi.App.Services.Application;
using Tarmi.App.ViewModels.FocusPoints;
using Tarmi.App.ViewModels.ROIs;
using Tarmi.Configuration;
using Tarmi.Devices.Thermofisher.Instrument.Types;
using Tarmi.ImagePipeline.Pipelines;
using Tarmi.Imaging.Common;
using Tarmi.Models;
using Tarmi.Projects;
using Tarmi.VirtualDevices;
using Tarmi.App.WPF;
using UnitsNet;

namespace Tarmi.App.ViewModels.Modes.FIB;

public partial class IonBeamModeViewModel : BeamModeViewModelBase
{
    private readonly IIonBeamMode _virtualDevice;
    protected override string ModeName => "FIB";
    protected override StageCameraView CameraView => SelectedViewMode == IonBeamViewMode.Milling ? StageCameraView.FIB_Milling : StageCameraView.FIB_RightAngle;
    // TODO: move to configuration
    private readonly Angle DefaultMillingBeamRotation = Angle.FromDegrees(180);
    private readonly Angle DefaultRightAngleBeamRotation = Angle.FromDegrees(0);


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TileSetGrabbingEnabled))]
    [NotifyCanExecuteChangedFor(nameof(AutoTiltCommand))]
    public partial IonBeamViewMode SelectedViewMode { get; set; }

    public bool TileSetGrabbingEnabled => SelectedViewMode == IonBeamViewMode.RightAngle;

    protected override bool CanAutoTilt() => SelectedViewMode == IonBeamViewMode.RightAngle && base.CanAutoTilt();

    public IonBeamModeViewModel(
        IIonBeamMode ionBeamMode,
        IWindowService windowService,
        IProjectManager projectManager,
        ILoggerFactory loggerFactory,
        IStageNavigation stageNavigation,
        ISafeStageControlling safeStageControlling,
        ApplicationConfig applicationConfig,
        ILimits limits,
        OverviewImageViewModel overviewImageViewModel,
        RoiControlViewModel roiControlViewModel,
        FocusPointControlViewModel focusPointControlViewModel
    )
        : base(
            loggerFactory.CreateLogger<IonBeamModeViewModel>(),
            ionBeamMode,
            windowService,
            projectManager,
            new IonImagingPipeline(
                loggerFactory.CreateLogger<IonImagingPipeline>(),
                ionBeamMode,
                projectManager,
                stageNavigation
            ),
            stageNavigation,
            safeStageControlling,
            applicationConfig,
            limits,
            overviewImageViewModel,
            roiControlViewModel,
            focusPointControlViewModel
        )
    {
        _virtualDevice = ionBeamMode;
        SelectedViewMode = ionBeamMode.ViewMode;
    }

    protected override string CreateActivityName([CallerMemberName] string methodName = "")
        => $"{nameof(IonBeamModeViewModel)}::{methodName}";

    async partial void OnSelectedViewModeChanged(IonBeamViewMode value)
    {
        if (IsInitialized)
        {
            using (_windowService.ShowBusyMessage(Messages.StageMoveBusyMessage))
            {
                await _virtualDevice.SwitchViewModeAsync(value);
                SelectedViewMode = _virtualDevice.ViewMode;
                _virtualDevice.SetBeamRotation(value == IonBeamViewMode.Milling ? DefaultMillingBeamRotation : DefaultRightAngleBeamRotation);
                RoiControl.ImagesStateManager.UpdateCanNavigateTo();
            }
        }
    }

    protected override async Task InitializeInternalAsync(ApplicationMode prevMode, CancellationToken cancellationToken)
    {
        await base.InitializeInternalAsync(prevMode, cancellationToken);
        await _virtualDevice.SwitchViewModeAsync(SelectedViewMode);
        _virtualDevice.SetBeamRotation(SelectedViewMode == IonBeamViewMode.Milling ? DefaultMillingBeamRotation : DefaultRightAngleBeamRotation);
    }

    private static RatioRectangle TransformRectangle(RatioRectangle rectangle, ImageMetadata imageMetadata)
    {
        if (imageMetadata.Coordinates.ImageIsFlippedOnY)
        {
            rectangle = rectangle with
            {
                Left = Ratio.FromDecimalFractions(1 - rectangle.Right.DecimalFractions),
                Right = Ratio.FromDecimalFractions(1 - rectangle.Left.DecimalFractions)
            };
        }
        if (imageMetadata.Coordinates.ImageIsFlippedOnX == false)
        {
            rectangle = rectangle with
            {
                Top = Ratio.FromDecimalFractions(1 - rectangle.Bottom.DecimalFractions),
                Bottom = Ratio.FromDecimalFractions(1 - rectangle.Top.DecimalFractions)
            };
        }

        return rectangle;
    }

    public async Task TransferMillingAreas(ImageMetadata imageMetadata, IEnumerable<MillingAreaInfo> millingAreas)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        if (!imageMetadata.GetSource().IsOneOf(StageCameraView.FIB_Milling, StageCameraView.FIB_RightAngle))
        {
            return;
        }
        await Task.Run(async () =>
        {
            _logger.Swallow(() => _virtualDevice.ClearMillingDefinitions());

            var position = imageMetadata.GetStagePosition();
            _ = await _virtualDevice.MoveStageAsync(position);

            var hfw = Length.FromMeters(imageMetadata.FeiXmlMetadata!.Optics!.ScanFieldOfView!.X);
            _logger.Swallow(() => _virtualDevice.SetHorizontalFieldWidth(hfw));

            var wd = Length.FromMeters(imageMetadata.FeiXmlMetadata!.Optics!.WorkingDistance!.Value);
            _logger.Swallow(() => _virtualDevice.SetWorkingDistance(wd));

            var imageSize = imageMetadata.FeiXmlMetadata!.ScanSettings!.ScanSize;
            var resolution  = new Resolution { Width = imageSize.Width, Height = imageSize.Height, Depth = 8 };
            _logger.Swallow(() => _virtualDevice.SetResolution(resolution));

            var scanRotation = Angle.FromRadians(imageMetadata.FeiXmlMetadata!.ScanSettings!.ScanRotation!.Value);
            _logger.Swallow(() => _virtualDevice.SetBeamRotation(scanRotation));

            foreach (var millingArea in millingAreas)
            {
                var ratioRectangle = millingArea.Definition;

                // tested only against simulation where y axis for milling should be transformed only,
                // but x axis needed to be transformed as well, if not working as expected, update the transform method
                ratioRectangle = TransformRectangle(ratioRectangle, imageMetadata);
                _logger.Swallow(() => _virtualDevice.AddMillingDefinition(ratioRectangle));
            }
        });
    }

    public async Task RestoreImageState(ImageMetadata imageMetadata)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        if (_safeStageControlling.ActiveCameraView == imageMetadata.GetSource())
        {
            await _virtualDevice.RestoreImageState(imageMetadata, default);
        }
    }
}
