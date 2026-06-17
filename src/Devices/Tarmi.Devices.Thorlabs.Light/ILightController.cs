using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;

namespace Tarmi.Devices.Thorlabs.Light;
public interface ILightController : IDisposable
{
    bool IsLightActive { get; }
    LightColor? SelectedLight { get; }
    Ratio Brightness { get; }
    IObservable<bool> CurrentIsLightActive { get; }
    IObservable<LightColor?> CurrentSelectedLight { get; }
    IObservable<Ratio> CurrentBrightness { get; }

    Task Deinitialize(CancellationToken cancellationToken);
    Task Initialize(CancellationToken cancellationToken);
    Task SelectLightAsync(LightColor? color, CancellationToken cancellationToken);
    Task SetBrightnessAsync(Ratio brightness, CancellationToken cancellationToken);
    Task TurnLightOnAsync(CancellationToken cancellationToken);
    Task TurnLightOffAsync(CancellationToken cancellationToken);
}
