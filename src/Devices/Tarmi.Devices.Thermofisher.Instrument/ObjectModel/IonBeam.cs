using System.Reactive.Linq;
using Tarmi.Devices.Thermofisher.Instrument.ObjectModel.Abstractions;
using Fei.XT.Common.gen;
using Microsoft.Extensions.Logging;

namespace Tarmi.Devices.Thermofisher.Instrument.ObjectModel;

internal sealed class IonBeam : InstrumentBeamBase
{
    private static readonly InstrumentBeamObjectPaths OmPaths = new()
    {
        Beam = PathLiterals.Instrument.Beams.IonBeam.AsString,
        EmissionCurrent = PathLiterals.Instrument.Beams.IonBeam.Source.EmissionCurrent.AsString,
        BeamIsOn = PathLiterals.Instrument.Beams.IonBeam.BeamIsOn.AsString,
        BeamIsBlanked = PathLiterals.Instrument.Beams.IonBeam.IsBlanked.AsString,
        HV = PathLiterals.Instrument.Beams.IonBeam.HV.AsString,
        BeamShift = PathLiterals.Instrument.Beams.IonBeam.BeamShift.AsString,
        Stigmator = PathLiterals.Instrument.Beams.IonBeam.Stigmator.AsString,
        ScanRotation = PathLiterals.Instrument.Beams.IonBeam.ScanRotation.AsString,
        WorkingDistance = PathLiterals.Instrument.Beams.IonBeam.WorkingDistance.AsString,
        DwellTime = PathLiterals.Instrument.Beams.IonBeam.Scanning.DwellTime.AsString,
        HFW = PathLiterals.Instrument.Beams.IonBeam.HorizontalFieldWidth.AsString,
        LineIntegration = PathLiterals.Instrument.Beams.IonBeam.Scanning.LineIntegration.AsString,
        ReducedAreaLineIntegration = PathLiterals.Instrument.Beams.IonBeam.Scanning.ReducedAreaSettings.LineIntegration.AsString,
        ScanInterlacing = PathLiterals.Instrument.Beams.IonBeam.Scanning.ScanInterlacing.AsString,
        BeamCurrents = PathLiterals.Instrument.Beams.IonBeam.BeamCurrentsList.AsString,
        BeamCurrentIndex = PathLiterals.Instrument.Beams.IonBeam.BeamCurrentIndex.AsString
    };

    private readonly IXtObjectHandle<ControlItemStringState> _gas;

    public IonBeam(ILogger<ElectronBeam> logger, IXtObjectsCollection xtObjectsCollection)
        : base(logger, xtObjectsCollection, OmPaths)
    {
        _gas = xtObjectsCollection.GetObject<ControlItemStringState>(PathLiterals.Instrument.Beams.IonBeam.Source.PlasmaGas.AsString);

        if (_beam.IsConnected)
        {
            Connect();
        }
        else
        {
            xtObjectsCollection.ConnectObjects();
        }
    }

    private void ConnectIonBeam()
    {
        _disposables.Add(
            Observable.FromEvent<string>(
                h => _gas.Object.OnStateChanged += new IControlItemStringStateEvents_OnStateChangedEventHandler(h),
                h => _ = h // not necessary when COM object is disconnected
            ).Subscribe(value => _logger.Swallow(() => _gasSubject.OnNext(value)))
        );
        _logger.Swallow(() => _gasSubject.OnNext(_gas.Object.State));
    }

    protected override void Connect()
    {
        base.Connect();
        ConnectIonBeam();
    }

    protected override void Disconnect()
    {
        base.Disconnect();
    }

    public Result<string> GetActiveIonGas()
    {
        try
        {
            return new(_gas.Object.State);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, nameof(GetActiveIonGas));
            return ex.MapToResult<string>();
        }
    }
}
