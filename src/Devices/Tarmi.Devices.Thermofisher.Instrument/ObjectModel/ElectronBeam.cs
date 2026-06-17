using System.Reactive.Linq;
using Tarmi.Devices.Thermofisher.Instrument.ObjectModel.Abstractions;
using Fei.XT.Common.gen;
using Fei.XT.Instrument.gen;
using Microsoft.Extensions.Logging;

namespace Tarmi.Devices.Thermofisher.Instrument.ObjectModel;

internal sealed class ElectronBeam : InstrumentBeamBase
{
    private static readonly InstrumentBeamObjectPaths OmPaths = new()
    {
        Beam = PathLiterals.Instrument.Beams.ElectronBeam.AsString,
        EmissionCurrent = PathLiterals.Instrument.Beams.ElectronBeam.Source.EmissionCurrent.AsString,
        BeamIsOn = PathLiterals.Instrument.Beams.ElectronBeam.BeamIsOn.AsString,
        BeamIsBlanked = PathLiterals.Instrument.Beams.ElectronBeam.IsBlanked.AsString,
        HV = PathLiterals.Instrument.Beams.ElectronBeam.HV.AsString,
        BeamShift = PathLiterals.Instrument.Beams.ElectronBeam.BeamShift.AsString,
        Stigmator = PathLiterals.Instrument.Beams.ElectronBeam.Stigmator.AsString,
        ScanRotation = PathLiterals.Instrument.Beams.ElectronBeam.ScanRotation.AsString,
        WorkingDistance = PathLiterals.Instrument.Beams.ElectronBeam.WorkingDistance.AsString,
        DwellTime = PathLiterals.Instrument.Beams.ElectronBeam.Scanning.DwellTime.AsString,
        HFW = PathLiterals.Instrument.Beams.ElectronBeam.HorizontalFieldWidth.AsString,
        LineIntegration = PathLiterals.Instrument.Beams.ElectronBeam.Scanning.LineIntegration.AsString,
        ReducedAreaLineIntegration = PathLiterals.Instrument.Beams.ElectronBeam.Scanning.ReducedAreaSettings.LineIntegration.AsString,
        ScanInterlacing = PathLiterals.Instrument.Beams.ElectronBeam.Scanning.ScanInterlacing.AsString,
        BeamCurrents = PathLiterals.Instrument.Beams.ElectronBeam.BeamCurrentsList.AsString,
        BeamCurrentIndex = PathLiterals.Instrument.Beams.ElectronBeam.BeamCurrentIndex.AsString
    };

    private readonly IXtObjectHandle<IAction> _degaussAction;
    private readonly IXtObjectHandle<ElectronBeamLensMode> _electronBeamLensMode;

    public ElectronBeam(ILogger<ElectronBeam> logger, IXtObjectsCollection xtObjectsCollection)
        : base(logger, xtObjectsCollection, OmPaths)
    {
        _degaussAction = xtObjectsCollection.GetObject<IAction>(PathLiterals.Instrument.Beams.ElectronBeam.Degauss.AsString);
        _electronBeamLensMode = xtObjectsCollection.GetObject<ElectronBeamLensMode>(PathLiterals.Instrument.Beams.ElectronBeam.LensMode.AsString);

        if (_beam.IsConnected)
        {
            Connect();
        }
        else
        {
            xtObjectsCollection.ConnectObjects();
        }
    }

    private static string LensModeToString(enELensMode mode) =>
        mode switch
        {
            enELensMode.enELensModeHR => "Field Free",
            enELensMode.enELensModeUHR => "Immersion",
            enELensMode.enELensModeEDX => "EDX",
            _ => "Unknown"
        };

    private void ConnectElectronBeam()
    {
        _disposables.Add(
            Observable.FromEvent<enELensMode>(
                h => _electronBeamLensMode.Object.OnValueChanged += new IElectronBeamLensModeControlEvents_OnValueChangedEventHandler(h),
                h => _ = h // not necessary when COM object is disconnected
            ).Subscribe(value => _logger.Swallow(() => _lensModeSubject.OnNext(LensModeToString(value))))
        );
        _logger.Swallow(() => _lensModeSubject.OnNext(LensModeToString(_electronBeamLensMode.Object.Value)));
    }

    protected override void Connect()
    {
        base.Connect();
        ConnectElectronBeam();
    }

    //protected override void Disconnect()
    //{
    //    base.Disconnect();
    //}

    public Result Degauss()
    {
        try
        {
            if (_degaussAction.Object.IsStartable)
            {
                _degaussAction.Object.Start(enCallType.enCallTypeSynchronous);
                _logger.LogDebug("Degauss completed");
                return Result.Success;
            }
            throw new InvalidOperationException();
        }
        catch (Exception ex)
        {
            return ex.MapToResult();
        }
    }
}
