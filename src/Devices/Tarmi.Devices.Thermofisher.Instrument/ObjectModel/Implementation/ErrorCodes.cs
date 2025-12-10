namespace Tarmi.Devices.Thermofisher.Instrument.ObjectModel.Implementation;

internal static class ErrorCodes
{
    public const string XtServerNotRunningMessage = "xT server is not running";
    public static int CannotConnectToMicroscope = 10000;

    // when xT server is going down
    private const int RPC_E_DISCONNECTED = unchecked((int)0x80010108);
    // when xT server is down
    public const int RPC_E_SERVER_UNAVAILABLE = unchecked((int)0x800706BE);
}

