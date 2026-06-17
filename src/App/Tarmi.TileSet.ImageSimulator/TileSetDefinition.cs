using Tarmi.Imaging.Common;
using Tarmi.Imaging.Common.OpenCvWrapper;
using Tarmi.Models;
using Tarmi.Configuration.Holders;
using OpenCvSharp;
using UnitsNet;

namespace Tarmi.TileSet.ImageSimulator;

internal record GridDescriptor
{
    public required LengthRectangle GridTileSetRectangle { get; init; }
    public required LengthPoint GridCenterPosition { get; init; }
    public required IntPoint GridCenterImagePosition { get; init; }
    public required bool IsXAxesReversed { get; init; }
    public required bool IsYAxesReversed { get; init; }
}

internal abstract class TileSetDefinition : IDisposable
{
    protected virtual string TileSetImagePath { get; } = string.Empty;
    protected virtual string DefaultImagePath { get; } = string.Empty;
    protected virtual IntPoint TileSetGridCenter { get; } = IntPoint.Zero;

    private readonly ImageWithMetadata _defaultImage;
    private readonly IImage _tileSetImage = new Image<Gray, byte>(0, 0);
    private readonly List<GridDescriptor> _grids = [];

    protected TileSetDefinition(Holder holder)
    {
        _defaultImage = TiffImage.Load(DefaultImagePath);
        var mat = Cv2.ImRead(TileSetImagePath, ImreadModes.Unchanged);
        var matType = mat.Type();
        _tileSetImage = matType switch
        {
            _ when matType == MatType.CV_8UC1 => Image<Gray, byte>.FromMat(mat),
            _ when matType == MatType.CV_8UC3 => Image<Bgr, byte>.FromMat(mat),
            _ => throw new InvalidOperationException($"Unexpected mat type {matType}.")
        };

        _grids.AddRange(holder.Grids.Select(GenerateGridDescriptor));
    }

    private GridDescriptor GenerateGridDescriptor(AreaOfInterest areaOfInterest)
    {
        var pixelSize = _defaultImage.GetPixelSize();

        var gridRectangle = areaOfInterest.BoundingRectangle;
        var gridCenter = gridRectangle.GetCenter();
        var lengthToTop = TileSetGridCenter.Y * pixelSize.Y;
        var lengthToLeft = TileSetGridCenter.X * pixelSize.X;
        var lengthToRight = (_tileSetImage.Width - TileSetGridCenter.X) * pixelSize.X;
        var lengthToBottom = (_tileSetImage.Height - TileSetGridCenter.Y) * pixelSize.Y;

        var tileSetRectangle = new LengthRectangle
        {
            Top = gridCenter.Y - lengthToTop,
            Bottom = gridCenter.Y + lengthToBottom,
            Left = gridCenter.X - lengthToLeft,
            Right = gridCenter.X + lengthToRight,
        };

        return new GridDescriptor
        {
            GridCenterPosition = gridCenter,
            GridCenterImagePosition = TileSetGridCenter,
            IsXAxesReversed = false,
            IsYAxesReversed = false,
            GridTileSetRectangle = tileSetRectangle
        };
    }

    private static (Length X, Length Y) GetVectorFromCenter(LengthPoint point, GridDescriptor grid)
    {
        Length x = point.X - grid.GridCenterPosition.X;
        if (grid.IsXAxesReversed)
        {
            x = x.InvertSign();
        }

        Length y = point.Y - grid.GridCenterPosition.Y;
        if (grid.IsYAxesReversed)
        {
            y = y.InvertSign();
        }

        return (x, y);
    }

    private IImage GetSubImage(IImage image, LengthPoint point, GridDescriptor grid)
    {
        var (xOffset, yOffset) = GetVectorFromCenter(point, grid);
        
        var pixelSize = _defaultImage.GetPixelSize();
        var xPointOffset = (int)(xOffset / pixelSize.X);
        var yPointOffset = (int)(yOffset / pixelSize.Y);

        var x = grid.GridCenterImagePosition.X + xPointOffset;
        var y = grid.GridCenterImagePosition.Y + yPointOffset;

        var height = _defaultImage.Image.Height;
        var width = _defaultImage.Image.Width;

        var rowStart = y - height / 2;
        var rowStartAdjusted = Math.Max(rowStart, 0);
        var rowEnd = y + height / 2;
        var rowEndAdjusted = Math.Min(rowEnd, image.Height);
        var checkHeight = rowEndAdjusted - rowStartAdjusted;
        if (checkHeight > height)
        {
            rowEndAdjusted -= height - checkHeight;
        }

        var colStart = x - width / 2;
        var colStartAdjusted = Math.Max(colStart, 0);
        var colEnd = x + width / 2;
        var colEndAdjusted = Math.Min(colEnd, image.Width);
        var checkWidth = colEndAdjusted - colStartAdjusted;
        if (checkWidth > width)
        {
            colEndAdjusted -= width - checkWidth;
        }

        using var subMat = _tileSetImage.GetSubRect(new Rect 
        {
            Left = colStartAdjusted,
            Width = colEndAdjusted - colStartAdjusted,
            Top = rowStartAdjusted,
            Height = rowEndAdjusted - rowStartAdjusted
        });

        if (subMat.Width == width && subMat.Height == height)
        {
            return subMat.Clone();
        }

        var top = rowStart < rowStartAdjusted ? height - subMat.Height : 0;
        var bottom = rowEnd > rowEndAdjusted ? height - (height - subMat.Height) : height;
        var left = colStart < colStartAdjusted ? width - subMat.Width : 0;
        var right = colEnd > colEndAdjusted ? width - (width - subMat.Width) : width;

        var newMat = _defaultImage.Image.Clone();

        var roi = new Rect()
        {
            Left = left,
            Top = top,
            Width = right - left,
            Height = bottom - top
        };

        using var newImageSubMat = newMat.GetSubRect(roi);
        Cv2.CopyTo(subMat.InputArray, newImageSubMat.OutputArray);

        return newMat;
    }

    public ImageWithMetadata GetImage(LengthPoint center)
    {
        var grid = _grids.FirstOrDefault(
            grid => grid.GridTileSetRectangle.IsPointInsideRectangle(center)
        );
        if (grid is null)
        {
            return GetDefaultImage();
        }
        _defaultImage.GetPixelSize();
        using var subImage = GetSubImage(_tileSetImage, center, grid);
        return _defaultImage with { Image = subImage.Clone() };
    }


    public void Dispose()
    {
        _defaultImage.Dispose();
        _tileSetImage.Dispose();
        GC.SuppressFinalize(this);
    }

    internal ImageWithMetadata GetDefaultImage() => _defaultImage.Clone();
}

