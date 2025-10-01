using Betrian.Models;

namespace Betrian.Devices.Thermofisher.Instrument.Types;

public record StageState
{
    public static StageState Zero { get; } = new()
    {
        IsLinked = false,
        IsMoving = false,
        IsInError = false,
        CurrentPosition = StagePosition.Zero
    };

    public required bool IsLinked { get; init; }
    public required bool IsMoving { get; init; }
    public required bool IsInError { get; init; }
    public required StagePosition CurrentPosition { get; init; }
}
