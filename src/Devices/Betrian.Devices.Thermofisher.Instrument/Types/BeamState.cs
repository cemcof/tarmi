using Betrian.Models;
using UnitsNet;

namespace Betrian.Devices.Thermofisher.Instrument.Types;

public record BeamState
{
    public static BeamState Zero { get; } = new()
    {
        IsOn = false,
        IsBlanked = false,
        HV = ElectricPotential.Zero,
        FreeWorkingDistance = Length.Zero,
        VerticalFieldWidth = Length.Zero,
        HorizontalFieldWidth = Length.Zero,
        BeamShift = LengthPoint.Zero,
        Stigmator = LengthPoint.Zero,
        ScanRotation = Angle.Zero,
        DwellTime = Duration.Zero,
        Resolution = Resolution.Zero,
        PixelSize = LengthPoint.Zero,
        LensMode = string.Empty,
        LineIntegration = 1,
        ScanInterlacing = 1,
        Gas = string.Empty,
        BeamCurrents = [],
        BeamCurrentIndex = 1
    };

    public required bool IsOn { get; init; }
    public required bool IsBlanked { get; init; }
    public required ElectricPotential HV { get; init; }
    public required Length FreeWorkingDistance { get; init; }
    public required Length VerticalFieldWidth { get; init; }
    public required Length HorizontalFieldWidth { get; init; }
    public required LengthPoint BeamShift { get; init; }
    public required LengthPoint Stigmator { get; init; }
    public required Angle ScanRotation { get; init; }
    public required Duration DwellTime { get; init; }
    public required Resolution Resolution { get; init; }
    public required LengthPoint PixelSize { get; init; }
    public required string LensMode { get; init; }
    public required int LineIntegration { get; init; }
    public required int ScanInterlacing { get; init; }
    public required string Gas { get; init; }
    public required ElectricCurrent[] BeamCurrents { get; init; }
    public required int BeamCurrentIndex { get; init; }
}
