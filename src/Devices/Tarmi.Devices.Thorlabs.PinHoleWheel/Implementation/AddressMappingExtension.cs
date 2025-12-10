namespace Tarmi.Devices.Thorlabs.PinHoleWheel.Implementation;

internal static class AddressMappingExtension
{
    public static string GetAddressString(this int address)
    {
        return address switch
        {
            <= 0 => throw new NotImplementedException(),
            < 10 => $"{address}",
            10 => ":",
            11 => ";",
            12 => "<",
            13 => "=",
            14 => ">",
            15 => "?",
            16 => "@",
            _ => throw new NotImplementedException()
        };
    }
}
