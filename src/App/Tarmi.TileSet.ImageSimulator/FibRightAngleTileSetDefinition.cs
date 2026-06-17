using Tarmi.Models;
using Tarmi.Configuration.Holders;

namespace Tarmi.TileSet.ImageSimulator;

internal sealed class FibRightAngleTileSetDefinition : TileSetDefinition
{
    protected override string TileSetImagePath { get; } = @"fib\tileset-raw.tiff";
    protected override string DefaultImagePath { get; } = @"fib\default-raw.tiff";
    protected override IntPoint TileSetGridCenter { get; } = new() { X = 6906, Y = 6908 };

    public FibRightAngleTileSetDefinition(Holder holder)
        : base(holder)
    {
    }
}
