using Betrian.Models;
using CFLMnavi.Configuration.Holders;

namespace Betrian.TileSet.ImageSimulator;

internal sealed class FibRightAngleTileSetDefinition : TileSetDefinition
{
    protected override string TileSetImagePath { get; } = """fib\tileset-raw.tiff""";
    protected override string DefaultImagePath { get; } = """fib\default-raw.tiff""";
    protected override IntPoint TileSetGridCenter { get; } = new() { X = 6906, Y = 6908 };

    private FibRightAngleTileSetDefinition()
        : base()
    {
    }

    public static FibRightAngleTileSetDefinition Create(Holder holder)
    {
        var instance = new FibRightAngleTileSetDefinition();
        instance.Initialize(holder);
        return instance;
    }
}
