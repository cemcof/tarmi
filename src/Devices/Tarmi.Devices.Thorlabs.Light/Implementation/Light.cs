using UnitsNet;

namespace Tarmi.Devices.Thorlabs.Light.Implementation;
public record Light
{
    public bool LightOn { get; set; } = false;

    public Ratio Brightness { get; set; } = Ratio.Zero;
}
