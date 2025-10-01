using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;

namespace Betrian.Devices.Thorlabs.Light;
public interface ILightController : IDisposable
{
    LightColor? ActiveLight { get; }
    Ratio Brightness { get; }
    IObservable<LightColor?> CurrentActiveLight { get; }
    IObservable<Ratio> CurrentBrightness { get; }

    Task Deinitialize(CancellationToken cancellationToken);
    Task Initialize(CancellationToken cancellationToken);
    Task SetActiveLightAsync(LightColor? color, CancellationToken cancellationToken);
    Task SetBrightnessAsync(Ratio brightness, CancellationToken cancellationToken);
}
