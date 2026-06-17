using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;

namespace Tarmi.Devices.Thorlabs.Light.Implementation;
internal static class Commands
{
    public static string SetBrightnessModeCommand(bool on) => $"m {on.ToInt()}";
    
    public static string SetSingleLightModeCommand(bool on) => $"sm {on.ToInt()}";

    public static string SetLightStateCommand(LightColor color, bool on) => $"o {color.ToChannel()} {on.ToInt()}";

    public static string SetLightBrightnessCommand(LightColor color, Ratio brightness) => string.Create(CultureInfo.InvariantCulture, $"bp {color.ToChannel()} {brightness.Percent:N2}");
}
