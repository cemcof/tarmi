namespace Tarmi.Devices.Thorlabs.FilterWheel.Implementation;

internal static class Commands
{
    public static string GetCurrentPositionCommand() => $"";

    public static string SetGoToPositionCommand(long position) => $"{position:X8}";

    public static string IsDeviceActiveCommand() => $"";
}
