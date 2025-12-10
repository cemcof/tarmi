namespace Tarmi.Devices.Thorlabs.Light.Implementation;

internal static class TypesMappingExtensions
{
    public static int ToChannel(this LightColor color) => (int)color;

    public static int ToInt(this bool value) => Convert.ToInt32(value);
}
