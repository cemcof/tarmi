using Betrian.Devices.SmarAct.Stage;
using Betrian.Devices.Thermofisher.Instrument;
using Betrian.Devices.Thermofisher.Instrument.Types;
using Betrian.Models;
using CFLMnavi.Configuration;
using CFLMnavi.Configuration.Holders;
using CFLMnavi.Projects;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace CFLMnavi.VirtualDevices.Implementation;

internal class Limits : ILimits, IDisposable
{
    private readonly ILogger _logger;
    private readonly IInstrument _instrument;
    private readonly ILinearStage _linearStage;
    private readonly IStageNavigation _stageNavigation;
    private readonly ApplicationConfig _applicationConfig;
    private readonly IDisposable _disposable;
    private Holder? _holder;


    public Limits(ILogger<Limits> logger, IInstrument instrument, ILinearStage linearStage, IProjectManager projectManager, IStageNavigation stageNavigation, ApplicationConfig applicationConfig)
    {
        _logger = logger;
        _instrument = instrument;
        _linearStage = linearStage;
        _stageNavigation = stageNavigation;
        _applicationConfig = applicationConfig;
        _holder = projectManager.GetActiveProject()?.Holder;
        _disposable = projectManager.ActiveProject.Subscribe(project => AssignHolder(project?.Holder));
    }

    private void ThrowIfHolderNotInitialized()
    {
        if (_holder is null)
        {
            throw new InvalidOperationException("Holder is not initialized.");
        }
    }

    private void AssignHolder(Holder? holder)
    {
        if (holder is null)
        {
            // project was closed ignore or app is starting
            return;
        }

        _holder = holder;
    }

    public AngleRangeDescriptorWithStep GetTiltRangeForView(StageCameraView view)
    {
        ThrowIfHolderNotInitialized();

        var position = _stageNavigation.GetInitialStageCenterPosition(view);
        return new AngleRangeDescriptorWithStep()
        {
            Min = position.Tilt + _holder!.SafeTiltRange.Min,
            Max = position.Tilt + _holder!.SafeTiltRange.Max,
            Step = Angle.FromDegrees(0.1)
        };
    }

    public LengthRangeDescriptorWithStep GetFocusRangeForActiveBeam()
    {
        if (_instrument.ActiveMode == InstrumentMode.StageOnly)
        {
            return new()
            {
                Min = _applicationConfig.Microscope.Alignment.LinearStage.FocusMinimum,
                Max = _applicationConfig.Microscope.Alignment.LinearStage.FocusMaximum,
                Step = _applicationConfig.Microscope.Alignment.LinearStage.FocusStep
            };
        }
        else
        {
            var step = _instrument.ActiveMode switch
            {
                // TODO: Add to configuration (where exactly - alignment/focus functions?)
                InstrumentMode.Sem => _applicationConfig.UserPreferences.Algorithms.FocusFunctions.SEMAutoFocusStep,
                InstrumentMode.Fib => _applicationConfig.UserPreferences.Algorithms.FocusFunctions.FIBAutoFocusStep,
                _ => throw new NotImplementedException()
            };

#pragma warning disable CS0618 // Type or member is obsolete
            var range = _instrument.GetBeamFreeWorkingDistanceRange();
#pragma warning restore CS0618 // Type or member is obsolete
            _logger.LogInformation("Focus range for {InstrumentMode}: {Range}", _instrument.ActiveMode, range);
            range = range with { Step = step };
            _logger.LogInformation("Focus range for {InstrumentMode} after modification: {Range}", _instrument.ActiveMode, range);
            return range;
        }
    }

    public AngleRangeDescriptorWithStep GetAutoTiltRangeForView(StageCameraView view)
        => GetTiltRangeForView(view);

    public LengthRangeDescriptorWithStep GetAutoFocusRangeForActiveBeam()
    {
        var fullRange = GetFocusRangeForActiveBeam();
        var focusConfig = _applicationConfig.UserPreferences.Algorithms.FocusFunctions;

        var delta = _instrument.ActiveMode switch
        {
            InstrumentMode.StageOnly => focusConfig.LMFocusRange,
            _ => GetAutoFocusRangeBasedOnHFW()
        };
        delta /= 2;

        // Use default values for beam modes?
        var distance = _instrument.ActiveMode switch
        {
            InstrumentMode.StageOnly => _linearStage.CurrentPosition,
            InstrumentMode.Sem => _applicationConfig.UserPreferences.Algorithms.FocusFunctions.SEMDefaultWorkingDistance,
            InstrumentMode.Fib => _applicationConfig.UserPreferences.Algorithms.FocusFunctions.FIBDefaultWorkingDistance,
            _ => throw new NotImplementedException()
        };

        // TODO: when by the edges enlarge the area on other side

        return fullRange with
        {
            Min = UnitMath.Max(fullRange.Min, distance - delta),
            Max = UnitMath.Min(fullRange.Max, distance + delta),
        };
    }

    private static readonly Length MinimumConsideredHFW = Length.FromMicrometers(30);
    private static readonly Length MaximumConsideredHFW = Length.FromMillimeters(1.5);
    private const double BaseRangeRatio = 0.25;

    private Length GetAutoFocusRangeBasedOnHFW()
    {
        var focusRange = _instrument.ActiveMode switch
        {
            InstrumentMode.Sem => _applicationConfig.UserPreferences.Algorithms.FocusFunctions.SEMFocusRange,
            InstrumentMode.Fib => _applicationConfig.UserPreferences.Algorithms.FocusFunctions.FIBFocusRange,
            _ => throw new NotImplementedException()
        };
        
        // Linear dependence
        var hfwRatio = double.Clamp((_instrument.GetHorizontalFieldWidth() - MinimumConsideredHFW) / (MaximumConsideredHFW - MinimumConsideredHFW), 0, 1);

        return (BaseRangeRatio + (1 - BaseRangeRatio) * hfwRatio) * focusRange;
    }

    public void Dispose() => _disposable.Dispose();
}
