using Betrian.Imaging.Common;
using Betrian.Imaging.Common.OpenCvWrapper;
using Betrian.Models;
using CFLMnavi.Configuration.Holders;
using OpenCvSharp;
using UnitsNet;

namespace Betrian.TileSet.ImageSimulator;

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

    private ImageWithMetadata _defaultImage = ImageWithMetadata.Empty;
    private IImage _tileSetImage = new Image<Gray, byte>(0, 0);
    private PixelSize _pixelSize = PixelSize.Zero;
    private IntSize2d _imageSize = IntSize2d.Zero;
    private List<GridDescriptor> _grids = [];

    protected TileSetDefinition()
    {
    }

    protected void Initialize(Holder holder)
    {
        _defaultImage = TiffImage.Load(DefaultImagePath);
        var mat = Cv2.ImRead(TileSetImagePath, ImreadModes.Unchanged);
        _tileSetImage = mat.Type() == MatType.CV_8UC1 ? Image<Gray, byte>.FromMat(mat) : Image<Bgr, byte>.FromMat(mat);
        _pixelSize = _defaultImage.GetPixelSize();
        _imageSize = new IntSize2d { Width = _defaultImage.Image.Width, Height = _defaultImage.Image.Height };

        foreach (var grid in holder.Grids)
        {
            var gridRectangle = grid.BoundingRectangle;
            var gridCenter = gridRectangle.GetCenter();
            var isXAxesReversed = _defaultImage.GetRawImageRotationAngle().IsInTolerance(Angle.FromDegrees(0), Angle.FromDegrees(1));
            var isYAxesReversed = !isXAxesReversed;

            var lengthToTop = TileSetGridCenter.Y * _pixelSize.Y;
            var lengthToLeft = TileSetGridCenter.X * _pixelSize.X;
            var lengthToRight = (_tileSetImage.Width - TileSetGridCenter.X) * _pixelSize.X;
            var lengthToBottom = (_tileSetImage.Height - TileSetGridCenter.Y) * _pixelSize.Y;

            var tileSetRectangle = new LengthRectangle
            {
                Top = isYAxesReversed ? gridCenter.Y + lengthToTop : gridCenter.Y - lengthToTop,
                Left = isXAxesReversed ? gridCenter.X + lengthToLeft : gridCenter.X - lengthToLeft,
                Right = isXAxesReversed ? gridCenter.X - lengthToRight : gridCenter.X + lengthToRight,
                Bottom = isYAxesReversed ? gridCenter.Y - lengthToBottom : gridCenter.Y + lengthToBottom,
            };

            var descriptor = new GridDescriptor
            {
                GridCenterPosition = gridCenter,
                GridCenterImagePosition = TileSetGridCenter,
                IsXAxesReversed = isXAxesReversed,
                IsYAxesReversed = isYAxesReversed,
                GridTileSetRectangle = tileSetRectangle
            };

            _grids.Add(descriptor);
        }
    }

    public ImageWithMetadata GetDefaultImage() => _defaultImage;

    private GridDescriptor? LocateGrid(LengthPoint point)
    {
        foreach (var grid in _grids)
        {
            if (grid.GridTileSetRectangle.IsPointInsideRectangle(point))
            {
                return grid;
            }
        }
        return null;
    }

    private static (Length X, Length Y) GetDistanceFromCenter(LengthPoint point, GridDescriptor grid)
    {
        var x = grid.IsXAxesReversed ?
            grid.GridCenterPosition.X - point.X :
            point.X - grid.GridCenterPosition.X;

        var y = grid.IsYAxesReversed ?
            grid.GridCenterPosition.Y - point.Y :
            point.Y - grid.GridCenterPosition.Y;
        return (-x, -y);
    }

    private IImage GetSubImage(IImage image, LengthPoint point, GridDescriptor grid)
    {
        var (xLenDiff, yLenDiff) = GetDistanceFromCenter(point, grid);
        var xPointDiff = xLenDiff / _pixelSize.X;
        var yPointDiff = yLenDiff / _pixelSize.Y;

        var x = grid.GridCenterImagePosition.X + (int)xPointDiff;
        var y = grid.GridCenterImagePosition.Y + (int)yPointDiff;

        var rowStart = y - _imageSize.Height / 2;
        var rowStartAdjusted = Math.Max(rowStart, 0);
        var rowEnd = y + _imageSize.Height / 2;
        var rowEndAdjusted = Math.Min(rowEnd, image.Height);
        var checkWidth = rowEndAdjusted - rowStartAdjusted;
        if (checkWidth > _imageSize.Height)
        {
            rowEndAdjusted -= _imageSize.Height - checkWidth;
        }

        var colStart = x - _imageSize.Width / 2;
        var colStartAdjusted = Math.Max(colStart, 0);
        var colEnd = x + _imageSize.Width / 2;
        var colEndAdjusted = Math.Min(colEnd, image.Width);
        var checkHeight = colEndAdjusted - colStartAdjusted;
        if (checkHeight > _imageSize.Width)
        {
            colEndAdjusted -= _imageSize.Width - checkHeight;
        }

        // TODO: Check the calculation, once the loc gets to 0, the sub matrix is not what's expected
        using var subMat = _tileSetImage.GetSubRect(new Rect { Left = colStartAdjusted, Width = colEndAdjusted - colStartAdjusted, Top = rowStartAdjusted, Height = rowEndAdjusted - rowStartAdjusted });

        if (subMat.Width == _imageSize.Width && subMat.Height == _imageSize.Height)
        {
            return subMat.Clone();
        }

        var top = rowStart < rowStartAdjusted ? _imageSize.Height - subMat.Height : 0;
        var bottom = rowEnd > rowEndAdjusted ? _imageSize.Height - (_imageSize.Height - subMat.Height) : _imageSize.Height;
        var left = colStart < colStartAdjusted ? _imageSize.Width - subMat.Width : 0;
        var right = colEnd > colEndAdjusted ? _imageSize.Width - (_imageSize.Width - subMat.Width) : _imageSize.Width;

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
        var grid = LocateGrid(center);
        if (grid is not null)
        {
            var subImage = GetSubImage(_tileSetImage, center, grid);
            return _defaultImage with { Image = subImage.Clone() };
        }
        return _defaultImage with { Image = _defaultImage.Image.Clone() };
    }


    public void Dispose()
    {
        _defaultImage.Dispose();
        _tileSetImage.Dispose();
        GC.SuppressFinalize(this);
    }
}

