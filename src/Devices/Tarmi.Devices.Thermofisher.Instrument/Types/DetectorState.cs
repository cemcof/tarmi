using UnitsNet;

namespace Tarmi.Devices.Thermofisher.Instrument.Types;

public record DetectorState
{
    public static DetectorState Zero { get; } = new()
    {
        Name = string.Empty,
        Contrast = Ratio.Zero,
        Brightness = Ratio.Zero
    };

    public required string Name { get; init; }
    public required Ratio Contrast { get; init; }
    public required Ratio Brightness { get; init; }
}
