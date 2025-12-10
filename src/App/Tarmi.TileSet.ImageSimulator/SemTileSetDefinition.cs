using Tarmi.Models;
using Tarmi.Configuration.Holders;

namespace Tarmi.TileSet.ImageSimulator;

internal sealed class SemTileSetDefinition : TileSetDefinition
{
    protected override string TileSetImagePath { get; } = """sem\tileset-raw.tiff""";
    protected override string DefaultImagePath { get; } = """sem\default-raw.tiff""";
    protected override IntPoint TileSetGridCenter { get; } = new() { X = 4759, Y = 4235 };

    private SemTileSetDefinition()
        : base()
    {
    }

    public static SemTileSetDefinition Create(Holder holder)
    {
        var instance = new SemTileSetDefinition();
        instance.Initialize(holder);
        return instance;
    }
}
