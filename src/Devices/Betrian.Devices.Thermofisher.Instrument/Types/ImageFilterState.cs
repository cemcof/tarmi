namespace Betrian.Devices.Thermofisher.Instrument.Types;

public record ImageFilterState
{
    public static ImageFilterState Zero { get; } = new()
    {
        Type = ImageFilterType.None,
        Frames = 1
    };

    public ImageFilterType Type { get; init; }
    public int Frames { get; init; }
}
