using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Betrian.Devices.Thermofisher.Instrument.ObjectModel.Abstractions;
using Betrian.Devices.Thermofisher.Instrument.Types;
using Betrian.Models;
using Fei.XT.Instrument.gen;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace Betrian.Devices.Thermofisher.Instrument.ObjectModel;

internal sealed class Chamber
{
    private readonly ILogger _logger;
    private readonly IXtObjectHandle<VacuumCompartment> _vacuum;
    private readonly BehaviorSubject<Pressure> _pressureSubject = new(Pressure.Zero);
    private readonly BehaviorSubject<VacuumCompartmentState> _stateSubject = new(VacuumCompartmentState.Unknown);
    private CompositeDisposable _disposables = [];

    public Chamber(ILogger<Chamber> logger, IXtObjectsCollection xtObjectsCollection)
    {
        _logger = logger;
        _vacuum = xtObjectsCollection.GetObject<VacuumCompartment>(PathLiterals.Instrument.Vacuum.Compartments.Chamber.AsString);

        _vacuum.Connected += (obj, args) => Connect();
        _vacuum.Disconnecting += (obj, args) => Disconnect();

        if (_vacuum.IsConnected)
        {
            Connect();
        }
    }

    public IObservable<Pressure> ChamberPressure => _pressureSubject.AsObservable().DistinctUntilChanged();
    public IObservable<VacuumCompartmentState> State => _stateSubject.AsObservable().DistinctUntilChanged();

    private static VacuumCompartmentState FromXtType(enVacuumCompartmentState state) =>
        state switch
        {
            enVacuumCompartmentState.enVacuumCompartmentStatePumped => VacuumCompartmentState.Pumped,
            enVacuumCompartmentState.enVacuumCompartmentStateVenting => VacuumCompartmentState.Venting,
            enVacuumCompartmentState.enVacuumCompartmentStateBaking => VacuumCompartmentState.Baking,
            enVacuumCompartmentState.enVacuumCompartmentStateBusyUnknown => VacuumCompartmentState.BusyUnknown,
            enVacuumCompartmentState.enVacuumCompartmentStateError => VacuumCompartmentState.Error,
            enVacuumCompartmentState.enVacuumCompartmentStatePumping => VacuumCompartmentState.Pumping,
            enVacuumCompartmentState.enVacuumCompartmentStatePlasmaCleaning => VacuumCompartmentState.PlasmaCleaning,
            enVacuumCompartmentState.enVacuumCompartmentStatePumpedForWaferExchange => VacuumCompartmentState.PumpedForWaferExchange,
            enVacuumCompartmentState.enVacuumCompartmentStateVented => VacuumCompartmentState.Vented,
            _ => VacuumCompartmentState.Unknown
        };

    private void Connect()
    {
        _disposables.Add(
            Observable.FromEvent<double>(
                h => _vacuum.Object.Pressure.OnValueChanged += new Fei.XT.Common.gen.IControlItemFloatEvents_OnValueChangedEventHandler(h),
                h => _ = h // not necessary when COM object is disconnected
            ).Subscribe(value => _logger.Swallow(() => _pressureSubject.OnNext(Pressure.FromPascals(value))))
        );

        _disposables.Add(
            Observable.FromEvent<enVacuumCompartmentState>(
                h => _vacuum.Object.OnStateChanged += new IVacuumCompartmentEvents_OnStateChangedEventHandler(h),
                h => _ = h // not necessary when COM object is disconnected
            ).Subscribe(value => _logger.Swallow(() => _stateSubject.OnNext(FromXtType(value))))
        );

        var pressure = GetChamberPressure();
        _logger.Swallow(() => _pressureSubject.OnNext(pressure.Value));
        var state = GetChamberState();
        _logger.Swallow(() => _stateSubject.OnNext(state.Value));
    }

    private void Disconnect()
    {
        _disposables.Dispose();
        _disposables = [];
    }

    public Result<Pressure> GetChamberPressure()
    {
        try
        {
            var pressure = _vacuum.Object.Pressure.Value;
            return new(Pressure.FromPascals(pressure));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetChamberPressure));
            return ex.MapToResult<Pressure>();
        }
    }

    public Result<RangeDescriptor<Pressure>> GetChamberPressureRange()
    {
        try
        {
            _vacuum.Object.Pressure.GetLogicalLimits(out var min, out var max);
            return new(new RangeDescriptor<Pressure> { Min = Pressure.FromPascals(min), Max = Pressure.FromPascals(max) });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetChamberPressureRange));
            return ex.MapToResult<RangeDescriptor<Pressure>>();
        }
    }

    public Result<VacuumCompartmentState> GetChamberState()
    {
        try
        {
            return new(FromXtType(_vacuum.Object.State));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetChamberState));
            return ex.MapToResult<VacuumCompartmentState>();
        }
    }

    public Result<bool> GetIsChamberPumped()
    {
        try
        {
            return new(_vacuum.Object.State == enVacuumCompartmentState.enVacuumCompartmentStatePumped);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetIsChamberPumped));
            return ex.MapToResult<bool>();
        }
    }
}
