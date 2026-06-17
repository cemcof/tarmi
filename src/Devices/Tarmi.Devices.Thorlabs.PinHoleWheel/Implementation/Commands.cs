namespace Tarmi.Devices.Thorlabs.PinHoleWheel.Implementation;

internal static class Commands
{
    public static string GetCurrentPositionCommand(int address) => $"/{address.GetAddressString()}?8\r";

    public static string SetGoToPositionCommand(int address, long position) => $"/{address.GetAddressString()}A(0x{position:X8})R\r";

    public static string GetCurrentStepperModeCommand(int address) => $"/{address.GetAddressString()}&R\r";
}
