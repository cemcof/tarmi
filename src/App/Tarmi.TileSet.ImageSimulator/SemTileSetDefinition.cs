using Tarmi.Models;
using Tarmi.Configuration.Holders;

namespace Tarmi.TileSet.ImageSimulator;

internal sealed class SemTileSetDefinition : TileSetDefinition
{
    protected override string TileSetImagePath { get; } = @"sem\tileset-raw.tiff";
    protected override string DefaultImagePath { get; } = @"sem\default-raw.tiff";
    protected override IntPoint TileSetGridCenter { get; } = new() { X = 4759, Y = 4235 };

    public SemTileSetDefinition(Holder holder)
        : base(holder)
    {
    }
}
