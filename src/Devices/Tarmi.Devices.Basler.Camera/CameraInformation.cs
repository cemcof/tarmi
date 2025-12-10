namespace Tarmi.Devices.Basler.Camera;

public class CameraInformation
{
    public required string Name { get; init; }
    public required string FullName { get; init; }
    public required string UserDefinedName { get; init; }
    public required string Vendor { get; init; }
    public required string Model { get; init; }
    public required bool IsEmulated { get; init; }
}
