using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Tarmi.Devices.Thermofisher.Instrument.ObjectModel.Abstractions;
using Tarmi.Models;
using CommunityToolkit.Diagnostics;
using Fei.XT.Common.gen;
using Fei.XT.Instrument.gen;
using Microsoft.Extensions.Logging;
using UnitsNet;
using Resolution = Tarmi.Devices.Thermofisher.Instrument.Types.Resolution;

namespace Tarmi.Devices.Thermofisher.Instrument.ObjectModel;

internal record InstrumentBeamObjectPaths
{
    public required string Beam { get; init; }
    public required string EmissionCurrent { get; init; }
    public required string BeamIsOn { get; init; }
    public required string BeamIsBlanked { get; init; }
    public required string HV { get; init; }
    public required string BeamShift { get; init; }
    public required string Stigmator { get; init; }
    public required string ScanRotation { get; init; }
    public required string WorkingDistance { get; init; }
    public required string DwellTime { get; init; }
    public required string HFW { get; init; }
    public required string LineIntegration { get; init; }
    public required string ReducedAreaLineIntegration { get; init; }
    public required string ScanInterlacing { get; init; }
    public required string BeamCurrents { get; init; }
    public required string BeamCurrentIndex { get; init; }
}

internal abstract class InstrumentBeamBase
{
    private readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(1);

    protected readonly ILogger _logger;
    protected readonly IXtObjectHandle<ICPBeam> _beam;
    protected readonly IXtObjectHandle<ParameterFloat> _emissionCurrent;
    protected readonly IXtObjectHandle<Fei.XT.Common.gen.Action> _wakeupAction;
    protected readonly IXtObjectHandle<ControlItemBoolean> _beamIsOn;
    protected readonly IXtObjectHandle<ControlItemBoolean> _beamIsBlanked;
    protected readonly IXtObjectHandle<ControlItemFloat> _beamHV;
    protected readonly IXtObjectHandle<ControlItemFloatPair> _beamShift;
    protected readonly IXtObjectHandle<ControlItemFloatPair> _stigmator;
    protected readonly IXtObjectHandle<ControlItemFloat> _scanRotation;
    protected readonly IXtObjectHandle<ControlItemFloat> _workingDistance;
    protected readonly IXtObjectHandle<ParameterFloat> _dwellTime;
    protected readonly IXtObjectHandle<ControlItemFloat> _hfw;
    protected readonly IXtObjectHandle<ParameterInteger> _lineIntegration;
    protected readonly IXtObjectHandle<ParameterInteger> _reducedAreaLineIntegration;
    protected readonly IXtObjectHandle<ParameterInteger> _scanInterlacing;
    protected readonly IXtObjectHandle<BeamCurrents> _beamCurrents;
    protected readonly IXtObjectHandle<ControlItemInteger> _beamCurrentIndex;
    private IDisposable? _pollingDisposable;

    private readonly ConcurrentBag<Resolution> _allowedResolutions = [];
    protected CompositeDisposable _disposables = [];

    private readonly BehaviorSubject<bool> _beamOnSubject = new(false);
    private readonly BehaviorSubject<bool> _beamBlankSubject = new(false);
    private readonly BehaviorSubject<ElectricPotential> _hvSubject = new(ElectricPotential.Zero);
    private readonly BehaviorSubject<LengthPoint> _beamShiftSubject = new(LengthPoint.Zero);
    private readonly BehaviorSubject<LengthPoint> _stigmatorSubject = new(LengthPoint.Zero);
    private readonly BehaviorSubject<Angle> _scanRotationSubject = new(Angle.Zero);
    private readonly BehaviorSubject<Length> _fwdSubject = new(Length.Zero);
    private readonly BehaviorSubject<Duration> _dwellSubject = new(Duration.Zero);
    private readonly BehaviorSubject<Length> _hfwSubject = new(Length.Zero);
    private readonly BehaviorSubject<Length> _vfwSubject = new(Length.Zero);
    private readonly BehaviorSubject<Resolution> _scanResolutionSubject = new(Resolution.Zero);
    private readonly BehaviorSubject<LengthPoint> _scanPixelSizeSubject = new(LengthPoint.Zero);
    protected readonly BehaviorSubject<string> _lensModeSubject = new(string.Empty);
    protected readonly BehaviorSubject<string> _gasSubject = new(string.Empty);
    protected readonly BehaviorSubject<int> _lineIntegrationSubject = new(1);
    protected readonly BehaviorSubject<int> _scanInterlacingSubject = new(1);
    protected readonly BehaviorSubject<ElectricCurrent[]> _beamCurrentsSubject = new([]);
    private readonly BehaviorSubject<int> _beamCurrentIndexSubject = new(1);

    private static readonly Angle[] AllowedScanRotations =
    [
        Angle.FromDegrees(0),
        Angle.FromDegrees(180)
    ];


    protected InstrumentBeamBase(
        ILogger logger,
        IXtObjectsCollection xtObjectsCollection,
        InstrumentBeamObjectPaths objectsPaths
    )
    {
        _logger = logger;
        _beam = xtObjectsCollection.GetObject<ICPBeam>(objectsPaths.Beam);
        _emissionCurrent = xtObjectsCollection.GetObject<ParameterFloat>(objectsPaths.EmissionCurrent);
        _wakeupAction = xtObjectsCollection.GetObject<Fei.XT.Common.gen.Action>(PathLiterals.Instrument.Wake.AsString);
        _beamIsOn = xtObjectsCollection.GetObject<ControlItemBoolean>(objectsPaths.BeamIsOn);
        _beamIsBlanked = xtObjectsCollection.GetObject<ControlItemBoolean>(objectsPaths.BeamIsBlanked);
        _beamHV = xtObjectsCollection.GetObject<ControlItemFloat>(objectsPaths.HV);
        _beamShift = xtObjectsCollection.GetObject<ControlItemFloatPair>(objectsPaths.BeamShift);
        _stigmator = xtObjectsCollection.GetObject<ControlItemFloatPair>(objectsPaths.Stigmator);
        _scanRotation = xtObjectsCollection.GetObject<ControlItemFloat>(objectsPaths.ScanRotation);
        _workingDistance = xtObjectsCollection.GetObject<ControlItemFloat>(objectsPaths.WorkingDistance);
        _dwellTime = xtObjectsCollection.GetObject<ParameterFloat>(objectsPaths.DwellTime);
        _hfw = xtObjectsCollection.GetObject<ControlItemFloat>(objectsPaths.HFW);
        _lineIntegration = xtObjectsCollection.GetObject<ParameterInteger>(objectsPaths.LineIntegration);
        _reducedAreaLineIntegration = xtObjectsCollection.GetObject<ParameterInteger>(objectsPaths.ReducedAreaLineIntegration);
        _scanInterlacing = xtObjectsCollection.GetObject<ParameterInteger>(objectsPaths.ScanInterlacing);
        _beamCurrents = xtObjectsCollection.GetObject<BeamCurrents>(objectsPaths.BeamCurrents);
        _beamCurrentIndex = xtObjectsCollection.GetObject<ControlItemInteger>(objectsPaths.BeamCurrentIndex);

        _beam.Connected += (obj, args) => Connect();
        _beam.Disconnecting += (obj, args) => Disconnect();
    }

    public IObservable<bool> BeamIsOn => _beamOnSubject.AsObservable().DistinctUntilChanged();
    public IObservable<bool> BeamIsBlanked => _beamBlankSubject.AsObservable().DistinctUntilChanged();
    public IObservable<int> BeamCurrentIndex => _beamCurrentIndexSubject.AsObservable().DistinctUntilChanged();
    public IObservable<ElectricPotential> HV => _hvSubject.AsObservable().DistinctUntilChanged();
    public IObservable<LengthPoint> BeamShift => _beamShiftSubject.AsObservable();
    public IObservable<LengthPoint> Stigmator => _stigmatorSubject.AsObservable();
    public IObservable<Angle> ScanRotation => _scanRotationSubject.AsObservable().DistinctUntilChanged();
    public IObservable<Length> FreeWorkingDistance => _fwdSubject.AsObservable().DistinctUntilChanged();
    public IObservable<Duration> DwellTime => _dwellSubject.AsObservable().DistinctUntilChanged();
    public IObservable<Length> HorizontalFieldWidth => _hfwSubject.AsObservable().DistinctUntilChanged();
    public IObservable<Length> VerticalFieldWidth => _vfwSubject.AsObservable().DistinctUntilChanged();
    public IObservable<Resolution> ScanResolution => _scanResolutionSubject.AsObservable();
    public IObservable<LengthPoint> ScanPixelSize => _scanPixelSizeSubject.AsObservable();
    public IObservable<string> LensMode => _lensModeSubject.AsObservable().DistinctUntilChanged();
    public IObservable<string> Gas => _gasSubject.AsObservable().DistinctUntilChanged();
    public IObservable<int> LineIntegration => _lineIntegrationSubject.AsObservable().DistinctUntilChanged();
    public IObservable<int> ScanInterlacing => _scanInterlacingSubject.AsObservable().DistinctUntilChanged();
    public IObservable<ElectricCurrent[]> BeamCurrents => _beamCurrentsSubject.AsObservable();

    protected virtual void Connect()
    {
        ConnectBaseBeam();
    }

    public virtual Task Activate()
    {
        if (_wakeupAction.Object.IsStartable)
        {
            _wakeupAction.Object.Start(enCallType.enCallTypeSynchronous);
        }


        if (!_beam.Object.BeamIsOn.Value)
        {
            _beam.Object.BeamIsOn.SetTargetValue(true);
        }

        _pollingDisposable = Observable.Interval(PollingInterval).Subscribe(_ => PollingValues());

        return Task.CompletedTask;
    }

    public virtual Task Deactivate()
    {
        _pollingDisposable?.Dispose();

        // disabled on customers request
        //if (_beam.Object.BeamIsOn.Value)
        //{
        //    _beam.Object.BeamIsOn.SetTargetValue(false);
        //}

        return Task.CompletedTask;
    }

    private void PollingValues()
    {
        try
        {
            var resolution = GetResolution().Value!;
            _logger.Swallow(() => _scanResolutionSubject.OnNext(new Resolution { Width = resolution.Width, Height = resolution.Height, Depth = resolution.Depth }));
        }
        catch { }
    }

    private void ConnectBaseBeam()
    {
        InitializeResolutions();

        _disposables.Add(
            Observable.FromEvent<bool>(
                h => _beamIsOn.Object.OnValueChanged += new Fei.XT.Common.gen.IControlItemBooleanEvents_OnValueChangedEventHandler(h),
                h => _ = h // not necessary when COM object is disconnected
            ).Subscribe(value => _logger.Swallow(() => _beamOnSubject.OnNext(value)))
        );
        _logger.Swallow(() => _beamOnSubject.OnNext(_beam.Object.BeamIsOn.Value));

        _disposables.Add(
            Observable.FromEvent<bool>(
                h => _beamIsBlanked.Object.OnValueChanged += new Fei.XT.Common.gen.IControlItemBooleanEvents_OnValueChangedEventHandler(h),
                h => _ = h // not necessary when COM object is disconnected
            ).Subscribe(value => _logger.Swallow(() => _beamBlankSubject.OnNext(value)))
        );
        _logger.Swallow(() => _beamBlankSubject.OnNext(_beam.Object.IsBlanked.Value));

        _beamCurrentIndex.Object.GetLogicalLimits(out var min, out var _);
        
        _disposables.Add(
            Observable.FromEvent<int>(
                h => _beamCurrentIndex.Object.OnValueChanged += new Fei.XT.Common.gen.IControlItemIntegerEvents_OnValueChangedEventHandler(h),
                h => _ = h // not necessary when COM object is disconnected
            ).Subscribe(value => _logger.Swallow(() => _beamCurrentIndexSubject.OnNext(ConvertCurrentIndexToZeroIndex(value, min))))
        );

        _beamCurrentIndexSubject.OnNext(ConvertCurrentIndexToZeroIndex(_beamCurrentIndex.Object.Value, min));

        _disposables.Add(
            Observable.FromEvent<double>(
                h => _beamHV.Object.OnValueChanged += new Fei.XT.Common.gen.IControlItemFloatEvents_OnValueChangedEventHandler(h),
                h => _ = h // not necessary when COM object is disconnected
            ).Subscribe(value => _logger.Swallow(() => _hvSubject.OnNext(ElectricPotential.FromVolts(value))))
        );
        _logger.Swallow(() => _hvSubject.OnNext(ElectricPotential.FromVolts(_beam.Object.HV.Value)));

        // HACK: No clean way how to use Observable.FromEvent with two parameters
        _beamShift.Object.OnValueChanged += new IControlItemFloatPairEvents_OnValueChangedEventHandler((x, y) =>
        {
            _logger.Swallow(() => _beamShiftSubject.OnNext(new LengthPoint
            {
                X = Length.FromMeters(x),
                Y = Length.FromMeters(y)
            }));
        });
        _beam.Object.BeamShift.GetValue(out var x, out var y);
        _beamShiftSubject.OnNext(new LengthPoint
        {
            X = Length.FromMeters(x),
            Y = Length.FromMeters(y)
        });

        // HACK: No clean way how to use Observable.FromEvent with two parameters
        _stigmator.Object.OnValueChanged += new IControlItemFloatPairEvents_OnValueChangedEventHandler((x, y) =>
        {
            _logger.Swallow(() => _stigmatorSubject.OnNext(new LengthPoint
            {
                X = Length.FromMeters(x),
                Y = Length.FromMeters(y)
            }));
        });
        _beam.Object.Stigmator.GetValue(out x, out y);
        _logger.Swallow(() => _stigmatorSubject.OnNext(new LengthPoint
        {
            X = Length.FromMeters(x),
            Y = Length.FromMeters(y)
        }));

        _disposables.Add(
            Observable.FromEvent<double>(
                h => _scanRotation.Object.OnValueChanged += new Fei.XT.Common.gen.IControlItemFloatEvents_OnValueChangedEventHandler(h),
                h => _ = h // not necessary when COM object is disconnected
            ).Subscribe(HandleScanRotationChange)
        );
        _logger.Swallow(() => _scanRotationSubject.OnNext(Angle.FromRadians(_beam.Object.ScanRotation.Value)));

        _disposables.Add(
            Observable.FromEvent<double>(
                h => _workingDistance.Object.OnValueChanged += new Fei.XT.Common.gen.IControlItemFloatEvents_OnValueChangedEventHandler(h),
                h => _ = h // not necessary when COM object is disconnected
            ).Subscribe(value => _logger.Swallow(() => _fwdSubject.OnNext(Length.FromMeters(value))))
        );
        _logger.Swallow(() => _fwdSubject.OnNext(Length.FromMeters(_beam.Object.WorkingDistance.Value)));

        _disposables.Add(
            Observable.FromEvent<double>(
                h => _dwellTime.Object.OnValueChanged += new Fei.XT.Common.gen.IParameterFloatEvents_OnValueChangedEventHandler(h),
                h => _ = h // not necessary when COM object is disconnected
            ).Subscribe(value => _logger.Swallow(() => _dwellSubject.OnNext(Duration.FromSeconds(value))))
        );
        _logger.Swallow(() => _dwellSubject.OnNext(Duration.FromSeconds(_beam.Object.Scanning.DwellTime.Value)));

        _disposables.Add(
            Observable.FromEvent<double>(
                h => _hfw.Object.OnValueChanged += new Fei.XT.Common.gen.IControlItemFloatEvents_OnValueChangedEventHandler(h),
                h => _ = h // not necessary when COM object is disconnected
            ).Subscribe(value => {
                try
                {
                    var hfw = Length.FromMeters(value);
                    var vfw = Length.FromMeters(value * _beam.Object.ScanFieldAspect.Value);
                    var resolution = GetResolution().Value!;
                    _logger.Swallow(() => _hfwSubject.OnNext(hfw));
                    _logger.Swallow(() => _vfwSubject.OnNext(vfw));
                    _logger.Swallow(() => _scanResolutionSubject.OnNext(new Resolution { Width = resolution.Width, Height = resolution.Height, Depth = resolution.Depth }));
                    _logger.Swallow(() => _scanPixelSizeSubject.OnNext(new LengthPoint { X = Length.FromMeters(hfw.Meters / resolution.Width), Y = Length.FromMeters(vfw.Meters / resolution.Height) }));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "HFW change processing issue");
                }
            })
        );
        var hfw = Length.FromMeters(_beam.Object.HorizontalFieldWidth.Value);
        var vfw = hfw * _beam.Object.ScanFieldAspect.Value;
        var resolution = GetResolution().Value!;
        _logger.Swallow(() => _hfwSubject.OnNext(hfw));
        _logger.Swallow(() => _vfwSubject.OnNext(vfw));
        _logger.Swallow(() => _scanResolutionSubject.OnNext(new Resolution { Width = resolution.Width, Height = resolution.Height, Depth = resolution.Depth }));
        _logger.Swallow(() => _scanPixelSizeSubject.OnNext(new LengthPoint { X = Length.FromMeters(hfw.Meters / resolution.Width), Y = Length.FromMeters(vfw.Meters / resolution.Height) }));

        _disposables.Add(
            Observable.FromEvent<int>(
                h => _lineIntegration.Object.OnValueChanged += new IParameterIntegerEvents_OnValueChangedEventHandler(h),
                h => _ = h // not necessary when COM object is disconnected
            ).Subscribe(value => _logger.Swallow(() => _lineIntegrationSubject.OnNext(value)))
        );
        _logger.Swallow(() => _lineIntegrationSubject.OnNext(_lineIntegration.Object.Value));

        _disposables.Add(
            Observable.FromEvent<int>(
                h => _scanInterlacing.Object.OnValueChanged += new IParameterIntegerEvents_OnValueChangedEventHandler(h),
                h => _ = h // not necessary when COM object is disconnected
            ).Subscribe(value => _logger.Swallow(() => _scanInterlacingSubject.OnNext(value)))
        );
        _logger.Swallow(() => _scanInterlacingSubject.OnNext(_scanInterlacing.Object.Value));

        _disposables.Add(
            Observable.FromEvent(
                h => _beamCurrents.Object.OnListChanged += new IBeamCurrentsEvents_OnListChangedEventHandler(h),
                h => _ = h // not necessary when COM object is disconnected
            ).Subscribe(_ => _logger.Swallow(() => _beamCurrentsSubject.OnNext([.. GetBeamCurrents()])))
        );
        var currents = GetBeamCurrents().ToArray();
        _logger.Swallow(() => _beamCurrentsSubject.OnNext([.. currents]));
    }

    protected virtual void Disconnect()
    {
        _disposables.Dispose();
        _disposables = [];
        _allowedResolutions.Clear();
    }

    private static bool IsAllowedScanRotation(Angle scanRotation)
    {
        return AllowedScanRotations.Any(allowedRotation => allowedRotation.IsInTolerance(scanRotation));
    }

    private void HandleScanRotationChange(double value)
    {
        var rotation = Angle.FromRadians(value);
        _logger.Swallow(() => _scanRotationSubject.OnNext(rotation));
        if (!IsAllowedScanRotation(rotation))
        {
            _logger.LogWarning("Scan rotation {ScanRotation} is not allowed, enforcing correct one", rotation.Degrees);
            _logger.Swallow(() => _scanRotation.Object.SetTargetValue(AllowedScanRotations[0].Radians));
        }
    }

    public void EnforceScanRotation()
    {
        var rotation = Angle.FromRadians(_scanRotation.Object.Value);
        if (!IsAllowedScanRotation(rotation))
        {
            _logger.LogWarning("Scan rotation {ScanRotation} is not allowed, enforcing correct one", rotation.Degrees);
            _logger.Swallow(() => _scanRotation.Object.SetTargetValue(AllowedScanRotations[0].Radians));
        }
    }

    public Result SetBeamRotation(Angle rotation)
    {
        try
        {
            if (!IsAllowedScanRotation(rotation))
            {
                throw new ArgumentOutOfRangeException(nameof(rotation), "Scan rotation is not allowed");
            }
            _scanRotation.Object.SetTargetValue(rotation.Radians, enCallType.enCallTypeSynchronous);
            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(SetBeamRotation));
            return ex.MapToResult();
        }
    }

    private void InitializeResolutions()
    {
        _allowedResolutions.Clear();

        // Fei COM has enumerator bug, use for loop instead
        for (var index = 1; index <= _beam.Object.Scanning.Resolutions.Count; index++)
        {
            var resolution = _beam.Object.Scanning.Resolutions[index];
            _allowedResolutions.Add(new Resolution { Width = resolution.Width, Height = resolution.Height, Depth = Resolution.Mono8Depth });
            _allowedResolutions.Add(new Resolution { Width = resolution.Width, Height = resolution.Height, Depth = Resolution.Mono16Depth });
        }
    }

    private static int ConvertZeroIndexToCurrentIndex(int index, int currentMin)
        => currentMin + index;

    private static int ConvertCurrentIndexToZeroIndex(int currentIndex, int currentMin)
        => currentIndex - currentMin;

    private IEnumerable<ElectricCurrent> GetBeamCurrents()
    {
        foreach (double beamCurrentValue in _beamCurrents.Object)
        {
            yield return ElectricCurrent.FromAmperes(beamCurrentValue);
        }
    }

    public Result<int> GetBeamCurrentIndex()
    {
        try
        {
            _beamCurrentIndex.Object.GetLogicalLimits(out var min, out var _);
            var result = _beamCurrentIndex.Object.Value;

            result = ConvertCurrentIndexToZeroIndex(result, min);

            _logger.LogDebug("GetBeamCurrentIndex {CurrentIndex}", result);
            return new(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetBeamCurrentIndex));
            return ex.MapToResult<int>();
        }
    }

    public Result SetBeamCurrentIndex(int currentIndex)
    {
        try
        {
            _beamCurrentIndex.Object.GetLogicalLimits(out var min, out var _);

            var targetIndex = ConvertZeroIndexToCurrentIndex(currentIndex, min);

            _beamCurrentIndex.Object.SetTargetValue(targetIndex, enCallType.enCallTypeSynchronous);

            _logger.LogDebug("SetBeamCurrentIndex target set to {CurrentIndex}", targetIndex);
            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(SetBeamCurrentIndex));
            return ex.MapToResult();
        }
    }

    public Result<ElectricCurrent> GetEmissionCurrent()
    {
        try
        {
            var result = ElectricCurrent.FromAmperes(_emissionCurrent.Object.Value);
            _logger.LogDebug("GetEmissionCurrent {EmissionCurrent}", result);
            return new(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetEmissionCurrent));
            return ex.MapToResult<ElectricCurrent>();
        }
    }

    public Result<RangeDescriptor<ElectricCurrent>> GetBeamCurrentRange()
    {
        try
        {
            _beam.Object.BeamCurrent.GetLogicalLimits(out var min, out var max);
            var result = new RangeDescriptor<ElectricCurrent>
            {
                Min = ElectricCurrent.FromAmperes(min),
                Max = ElectricCurrent.FromAmperes(max)
            };
            _logger.LogDebug("GetBeamCurrentRange {CurrentRange}", result);
            return new(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetBeamCurrentRange));
            return ex.MapToResult<RangeDescriptor<ElectricCurrent>>();
        }
    }

    public Result<bool> GetIsBeamBlank()
    {
        try
        {
            var result = _beam.Object.IsBlanked.Value;
            _logger.LogDebug("GetBeamBlank {BeamIsBlanked}", result);
            return new(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetIsBeamBlank));
            return ex.MapToResult<bool>();
        }
    }

    public Result BlankBeam()
    {
        try
        {
            if (!_beam.Object.IsBlanked.Value)
            {
                _beam.Object.IsBlanked.SetTargetValue(true, enCallType.enCallTypeSynchronous);
                _logger.LogDebug("BlankBeam new target set");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(BlankBeam));
            return ex.MapToResult();
        }

        return Result.Success;
    }

    public Result UnblankBeam()
    {
        try
        {
            if (_beam.Object.IsBlanked.Value)
            {
                _beam.Object.IsBlanked.SetTargetValue(false, enCallType.enCallTypeSynchronous);
                _logger.LogDebug("UnblankBeam new target set");
            }

        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(UnblankBeam));
            return ex.MapToResult();
        }

        return Result.Success;
    }

    public Result<LengthPoint> GetBeamShift()
    {
        try
        {
            _beam.Object.BeamShift.GetValue(out var shiftX, out var shiftY);
            var result = new LengthPoint
            {
                X = Length.FromMeters(shiftX),
                Y = Length.FromMeters(shiftY)
            };
            _logger.LogDebug(message: "GetBeamShift {BeamShift}", result);
            return new(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetBeamShift));
            return ex.MapToResult<LengthPoint>();
        }
    }

    public Result<ElectricPotential> GetHighTensionVoltage()
    {
        try
        {
            var result = ElectricPotential.FromVolts(_beam.Object.HV.Value);
            _logger.LogDebug("GetHighTensionVoltage {HV}", result);
            return new(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetHighTensionVoltage));
            return ex.MapToResult<ElectricPotential>();
        }
    }

    public Result<RangeDescriptor<ElectricPotential>> GetHighTensionVoltageRange()
    {
        try
        {
            _beam.Object.HV.GetLogicalLimits(out var min, out var max);

            var result = new RangeDescriptor<ElectricPotential>
            {
                Min = ElectricPotential.FromVolts(min),
                Max = ElectricPotential.FromVolts(max)
            };
            _logger.LogDebug("GetHighTensionVoltageRange {@HVRange}", result);
            return new(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetHighTensionVoltageRange));
            return ex.MapToResult<RangeDescriptor<ElectricPotential>>();
        }
    }

    public Result SetHighTensionVoltage(ElectricPotential voltage)
    {
        try
        {
            var volts = voltage.Volts;
            if (_beam.Object.HV.Value != volts)
            {
                _beam.Object.HV.SetTargetValue(volts, enCallType.enCallTypeSynchronous);
                _logger.LogDebug("SetHighTensionVoltage new target set to {HV}", voltage);
            }
            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(SetHighTensionVoltage));
            return ex.MapToResult();
        }
    }

    public Result<Length> GetFreeWorkingDistance()
    {
        try
        {
            var result = Length.FromMeters(_beam.Object.WorkingDistance.Value);
            _logger.LogDebug("GetFreeWorkingDistance {WD}", result);
            return new(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetFreeWorkingDistance));
            return ex.MapToResult<Length>();
        }
    }

    public Result SetFreeWorkingDistance(Length value)
    {
        try
        {
            if (_beam.Object.WorkingDistance.Value != value.Meters)
            {
                var wd = _beam.Object.WorkingDistance;
                if (wd is INoDegaussControlItemFloat noDegWd)
                {
                    noDegWd.SetTargetValueNoDegauss(value.Meters, enCallType.enCallTypeSynchronous);
                }
                else
                {
                    wd.SetTargetValue(value.Meters, enCallType.enCallTypeSynchronous);
                }
                _logger.LogDebug("SetFreeWorkingDistance new target set to {WD}", value);
            }

            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(SetFreeWorkingDistance));
            return ex.MapToResult<Length>();
        }
    }

    public Result<LengthRangeDescriptorWithStep> GetFreeWorkingDistanceRange()
    {
        try
        {
            _beam.Object.WorkingDistance.GetLogicalLimits(out var min, out var max);
            var step = _beam.Object.WorkingDistance.Sensitivity;
            var result = new LengthRangeDescriptorWithStep
            {
                Min = Length.FromMeters(min),
                Max = Length.FromMeters(max),
                Step = Length.FromMeters(step)
            };
            _logger.LogDebug("GetFreeWorkingDistanceRange {FWDRange}", result);
            return new(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetFreeWorkingDistanceRange));
            return ex.MapToResult<LengthRangeDescriptorWithStep>();
        }
    }

    public Result<bool> GetIsHighTensionVoltageOn()
    {
        try
        {
            var result = _beam.Object.BeamIsOn.Value;
            _logger.LogDebug("GetIsHighTensionVoltageOn {HVOn}", result);
            return new(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetIsHighTensionVoltageOn));
            return ex.MapToResult<bool>();
        }
    }

    public Result<LengthPoint> GetSourceTilt()
    {
        try
        {
            _beam.Object.SourceTilt.GetValue(out var tiltX, out var tiltY);
            var result = new LengthPoint
            {
                X = Length.FromMeters(tiltX),
                Y = Length.FromMeters(tiltY)
            };
            _logger.LogDebug("GetSourceTilt {SourceTilt}", result);
            return new(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetSourceTilt));
            return ex.MapToResult<LengthPoint>();
        }
    }

    public Result SetSourceTilt(LengthPoint stigmator)
    {
        try
        {
            _beam.Object.SourceTilt.GetValue(out var x, out var y);
            if (x != stigmator.X.Meters || y != stigmator.Y.Meters)
            {
                _beam.Object.SourceTilt.SetTargetValue(stigmator.X!.Meters, stigmator.Y!.Meters);
                _logger.LogDebug("SourceTilt new target set to {SourceTilt}", stigmator);
            }
            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(SetSourceTilt));
            return ex.MapToResult<LengthPoint>();
        }
    }

    public Result<RangeDescriptor<LengthPoint>> GetSourceTiltRange()
    {
        try
        {
            _beam.Object.SourceTilt.GetLogicalLimits(out var tiltXMin, out var tiltYMin, out var tiltXMax, out var tiltYMax);
            var result = new RangeDescriptor<LengthPoint>()
            {
                Min = new LengthPoint
                {
                    X = Length.FromMeters(tiltXMin),
                    Y = Length.FromMeters(tiltYMin)
                },
                Max = new LengthPoint
                {
                    X = Length.FromMeters(tiltXMax),
                    Y = Length.FromMeters(tiltYMax)
                }
            };
            _logger.LogDebug("GetSourceTiltRange {SourceTiltRange}", result);
            return new(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetSourceTiltRange));
            return ex.MapToResult<RangeDescriptor<LengthPoint>>();
        }
    }

    public Result<LengthPoint> GetStigmator()
    {
        try
        {
            _beam.Object.Stigmator.GetValue(out var stigmatorX, out var stigmatorY);
            var result = new LengthPoint
            {
                X = Length.FromMeters(stigmatorX),
                Y = Length.FromMeters(stigmatorY)
            };
            _logger.LogDebug("GetStigmator {Stigmator}", result);
            return new(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetStigmator));
            return ex.MapToResult<LengthPoint>();
        }
    }

    public Result SetStigmator(LengthPoint stigmator)
    {
        try
        {
            _beam.Object.Stigmator.GetValue(out var x, out var y);
            if (x != stigmator.X.Meters || y != stigmator.Y.Meters)
            {
                _beam.Object.Stigmator.SetTargetValue(stigmator.X!.Meters, stigmator.Y!.Meters);
                _logger.LogDebug("SetStigmator new target set to {Stigmator}", stigmator);
            }
            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(SetStigmator));
            return ex.MapToResult<LengthPoint>();
        }
    }

    public Result<RangeDescriptor<LengthPoint>> GetStigmatorRange()
    {
        try
        {
            _beam.Object.Stigmator.GetLogicalLimits(out var stigmatorXMin, out var stigmatorYMin, out var stigmatorXMax, out var stigmatorYMax);
            var result = new RangeDescriptor<LengthPoint>()
            {
                Min = new LengthPoint
                {
                    X = Length.FromMeters(stigmatorXMin),
                    Y = Length.FromMeters(stigmatorYMin)
                },
                Max = new LengthPoint
                {
                    X = Length.FromMeters(stigmatorXMax),
                    Y = Length.FromMeters(stigmatorYMax)
                }
            };
            _logger.LogDebug("GetStigmatorRange {StigmatorRange}", result);
            return new Result<RangeDescriptor<LengthPoint>>(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetStigmatorRange));
            return ex.MapToResult<RangeDescriptor<LengthPoint>>();
        }
    }

    public Result<bool> GetIsBeamOn()
    {
        try
        {
            var result = _beam.Object.BeamIsOn.Value;
            _logger.LogDebug("GetBeamOn {BeamIsOn}", result);
            return new Result<bool>(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetIsBeamOn));
            return ex.MapToResult<bool>();
        }
    }

    public Result BeamOn()
    {
        try
        {
            if (!_beam.Object.BeamIsOn.Value)
            {
                _beam.Object.BeamIsOn.SetTargetValue(true, enCallType.enCallTypeSynchronous);
                _logger.LogDebug("BeamOn new target set");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(BeamOn));
            return ex.MapToResult();
        }

        return Result.Success;
    }

    public Result BeamOff()
    {
        try
        {
            if (_beam.Object.BeamIsOn.Value)
            {
                _beam.Object.BeamIsOn.SetTargetValue(false, enCallType.enCallTypeSynchronous);
                _logger.LogDebug("BeamOff new target set");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(BeamOff));
            return ex.MapToResult();
        }

        return Result.Success;
    }

    public Result<LengthSize2d> GetFieldSize()
    {
        try
        {
            var width = _beam.Object.HorizontalFieldWidth.Value;
            var height = width * _beam.Object.ScanFieldAspect.Value;

            var size = new LengthSize2d
            {
                Width = Length.FromMeters(width),
                Height = Length.FromMeters(height)
            };

            _logger.LogDebug("GetFieldSize {FieldSize}", size);

            return new Result<LengthSize2d>(size);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetFieldSize));
            return ex.MapToResult<LengthSize2d>();
        }
    }

    public Result<Length> GetHorizontalFieldWidth()
    {
        try
        {
            var hfw = Length.FromMeters(_beam.Object.HorizontalFieldWidth.Value);
            _logger.LogDebug("GetHorizontalFieldWidth {HFW}", hfw);
            return new Result<Length>(hfw);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetHorizontalFieldWidth));
            return ex.MapToResult<Length>();
        }
    }

    public Result SetHorizontalFieldWidth(Length width)
    {
        try
        {
            var size = width.Meters;
            if (_beam.Object.HorizontalFieldWidth.Value != size)
            {
                _beam.Object.HorizontalFieldWidth.SetTargetValue(width.Meters);
                _logger.LogDebug("SetHorizontalFieldWidth new target set to {HFW}", width);
            }
            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(SetHorizontalFieldWidth));
            return ex.MapToResult();
        }
    }

    public Result<LengthRangeDescriptor> GetHorizontalFieldWidthRange()
    {
        try
        {
            _beam.Object.HorizontalFieldWidth.GetLogicalLimits(out var min, out var max);

            var range = new LengthRangeDescriptor
            {
                Min = Length.FromMeters(min),
                Max = Length.FromMeters(max)
            };

            _logger.LogDebug("GetHorizontalFieldWidthRange new target set to {HFWRange}", range);

            return new Result<LengthRangeDescriptor>(range);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetHorizontalFieldWidthRange));
            return ex.MapToResult<LengthRangeDescriptor>();
        }
    }

    public Result<Angle> GetScanRotation()
    {
        try
        {
            var rotation = Angle.FromRadians(_beam.Object.ScanRotation.Value);
            _logger.LogDebug("GetScanRotation {ScanRotation}", rotation);
            return new Result<Angle>(rotation);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetScanRotation));
            return ex.MapToResult<Angle>();
        }
    }

    public Result SetScanRotation(Angle angle)
    {
        try
        {
            var rads = angle.Radians;
            if (_beam.Object.ScanRotation.Value != rads)
            {
                _beam.Object.ScanRotation.SetTargetValue(rads);
                _logger.LogDebug("SetScanRotation new target set to {ScanRotation}", angle);
            }
            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(SetScanRotation));
            return ex.MapToResult();
        }
    }

    public Result<Resolution> SetResolution(Resolution requestedResolution)
    {
        Guard.IsTrue(_allowedResolutions.Contains(requestedResolution), nameof(requestedResolution), "Invalid resolution size or depth");

        try
        {
            _beam.Object.Scanning.SetResolution(requestedResolution.Width, requestedResolution.Height, requestedResolution.Depth);
            var resolution = Resolution.FromXtType(_beam.Object.Scanning);
            _logger.LogDebug("SetResolution to {Resolution}", requestedResolution);
            return new(resolution);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(SetResolution));
            return ex.MapToResult<Resolution>();
        }
    }

    public Result<Resolution> GetResolution()
    {
        try
        {
            var resolution = Resolution.FromXtType(_beam.Object.Scanning);
            _logger.LogDebug("GetResolution {Resolution}", resolution);
            return new(resolution);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetResolution));
            return ex.MapToResult<Resolution>();
        }
    }

    public Result SetLineMode(BeamCoordinates coordinates, TimeSpan dwellTime)
    {
        try
        {
            Guard.IsTrue(coordinates.IsValid, nameof(coordinates), "Invalid position");

            if (_beam.Object.Scanning.LineModeSettings.Position != coordinates.Y)
            {
                _beam.Object.Scanning.LineModeSettings.Position = coordinates.Y;
            }
            var dt = dwellTime.TotalSeconds;
            if (_beam.Object.Scanning.LineModeSettings.DwellTime.Value != dt)
            {
                _beam.Object.Scanning.LineModeSettings.DwellTime.Value = dt;
            }
            if (_beam.Object.Scanning.ScanMode.Value != enScanMode.enScanModeLine)
            {
                _beam.Object.Scanning.ScanMode.SetTargetValue(enScanMode.enScanModeLine);
            }
            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(SetLineMode));
            return ex.MapToResult();
        }
    }

    public Result SetSpotMode(BeamCoordinates coordinates, TimeSpan dwellTime)
    {
        try
        {
            Guard.IsTrue(coordinates.IsValid, nameof(coordinates), "Invalid position");

            if (
                _beam.Object.Scanning.SpotModeSettings.PositionX != coordinates.X ||
                _beam.Object.Scanning.SpotModeSettings.PositionY != coordinates.Y
            )
            {
                _beam.Object.Scanning.SpotModeSettings.SetPosition(coordinates.X, coordinates.Y);
            }
            var dt = dwellTime.TotalSeconds;
            if (_beam.Object.Scanning.SpotModeSettings.DwellTime.Value != dt)
            {
                _beam.Object.Scanning.SpotModeSettings.DwellTime.Value = dt;
            }

            if (_beam.Object.Scanning.ScanMode.Value != enScanMode.enScanModeSpot)
            {
                _beam.Object.Scanning.ScanMode.SetTargetValue(enScanMode.enScanModeSpot);
            }
            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(SetSpotMode));
            return ex.MapToResult();
        }
    }

    public Result SetReducedAreaMode(Resolution resolution, BeamCoordinatesRectangle rectangle, TimeSpan dwellTime, int lineIntegration)
    {
        try
        {
            if (
                _beam.Object.Scanning.ReducedAreaSettings.ResolutionWidth != resolution.Width ||
                _beam.Object.Scanning.ReducedAreaSettings.ResolutionHeight != resolution.Height ||
                _beam.Object.Scanning.ReducedAreaSettings.ResolutionDepth != resolution.Depth
            )
            {
                _beam.Object.Scanning.ReducedAreaSettings.SetResolution(resolution.Width, resolution.Height, resolution.Depth);
            }
            _beam.Object.Scanning.ReducedAreaSettings.GetPosition(out var left, out var right, out var top, out var bottom);
            if (left != rectangle.Left || right != rectangle.Right || top != rectangle.Top || bottom != rectangle.Bottom)
            {
                _beam.Object.Scanning.ReducedAreaSettings.SetPosition(rectangle.Left, rectangle.Right, rectangle.Top, rectangle.Bottom);
            }
            var dt = dwellTime.TotalSeconds;
            if (_beam.Object.Scanning.ReducedAreaSettings.DwellTime.Value != dt)
            {
                _beam.Object.Scanning.ReducedAreaSettings.DwellTime.Value = dt;
            }
            if (_reducedAreaLineIntegration.Object.Value != lineIntegration)
            {
                _reducedAreaLineIntegration.Object.Value = lineIntegration;
            }

            if (_beam.Object.Scanning.ScanMode.Value != enScanMode.enScanModeReducedArea)
            {
                _beam.Object.Scanning.ScanMode.SetTargetValue(enScanMode.enScanModeReducedArea);
            }

            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(SetReducedAreaMode));
            return ex.MapToResult();
        }
    }

    public Result SetFullFrameMode(TimeSpan dwellTime, int lineIntegration)
    {
        try
        {
            double dt = dwellTime.TotalSeconds;
            if (_beam.Object.Scanning.FullFrameSettings.DwellTime.Value != dt)
            {
                _beam.Object.Scanning.FullFrameSettings.DwellTime.Value = dt;
            }
            if (_lineIntegration.Object.Value != lineIntegration)
            {
                _lineIntegration.Object.Value = lineIntegration;
            }

            if (_beam.Object.Scanning.ScanMode.Value != enScanMode.enScanModeFullFrame)
            {
                _beam.Object.Scanning.ScanMode.SetTargetValue(enScanMode.enScanModeFullFrame);
            }

            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(SetFullFrameMode));
            return ex.MapToResult();
        }
    }

    public Result<Duration> GetDwellTime()
    {
        try
        {
            var dwellTime = Duration.FromSeconds(_beam.Object.Scanning.DwellTime.Value);
            _logger.LogDebug("GetDwellTime {DwellTime}", dwellTime);
            return new(dwellTime);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetDwellTime));
            return ex.MapToResult<Duration>();
        }
    }

    public Result SetDwellTime(Duration dwellTime)
    {
        try
        {
            _beam.Object.Scanning.DwellTime.Value = dwellTime.Seconds;
            _logger.LogDebug("SetDwellTime to {DwellTime}", dwellTime);
            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetDwellTime));
            return ex.MapToResult<Duration>();
        }
    }

    public Result<enScanMode> GetXtScanMode()
    {
        try
        {
            return new Result<enScanMode>(_beam.Object.Scanning.ScanMode.Value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetXtScanMode));
            return ex.MapToResult<enScanMode>();
        }
    }
}
