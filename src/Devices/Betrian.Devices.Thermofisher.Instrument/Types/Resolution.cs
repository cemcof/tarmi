namespace Betrian.Devices.Thermofisher.Instrument.Types;

public record Resolution
{
    public static Resolution Zero { get; } = new()
    {
        Width = 0,
        Height = 0,
        Depth = UnknownDepth
    };

    public const int UnknownDepth = -1;
    public const int Mono8Depth = 8;
    public const int Mono16Depth = 16;
    public const int Color24Depth = 24;

    public int Width { get; init; }
    public int Height { get; init; }
    public int Depth { get; init; }

    public bool HasValidResolution => Depth > 0 && Depth % 8 == 0;
    public bool HasValidBeamResolution => Depth is Mono8Depth || Depth is Mono16Depth;

    internal static Resolution FromXtType(Fei.Imaging.gen.Resolution omResolution)
    {
        return new Resolution
        {
            Width = omResolution.x,
            Height = omResolution.y,
            Depth = UnknownDepth
        };
    }

    internal static Resolution FromXtType(Fei.XT.Instrument.gen.IScanning omScanning)
    {
        return new Resolution
        {
            Width = omScanning.ResolutionWidth,
            Height = omScanning.ResolutionHeight,
            Depth = omScanning.ResolutionDepth
        };
    }


}
