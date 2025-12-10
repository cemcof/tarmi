using Tarmi.Models;
using OpenCvSharp;
using UnitsNet;

namespace Tarmi.PointMapping;

public static class MapPointExtensions
{
    /// <summary> 
    /// Minimum homogenous weight tolerance. Prevention before division by zero.
    /// </summary>
    public const double HomogenousWeight = 0.001;

    /// <summary>
    /// Get 3x1 Mat with MatType.CV_64FC1 from 3D point.
    /// </summary>
    /// <param name="point">3D point.</param>
    /// <returns>Mat 3x1 contained 3D point.</returns>
    public static Mat GetMatFrom3DPoint(this Point3d point)
    {
        var mat = new Mat(3, 1, MatType.CV_64FC1);
        mat.Set<double>(0, 0, point.X);
        mat.Set<double>(1, 0, point.Y);
        mat.Set<double>(2, 0, point.Z);
        return mat;
    }

    /// <summary>
    /// Get 3D point from Mat.
    /// </summary>
    /// <param name="mat">Mat contains 3D point.</param>
    /// <returns>3D point.</returns>
    public static Point3d Get3DPointFromMat(this Mat mat) => new(
        (double)mat.At<double>(0, 0),
        (double)mat.At<double>(1, 0),
        (double)mat.At<double>(2, 0));

    /// <summary>
    /// Get stage position from 3D point, rotation and tilt.
    /// </summary>
    /// <param name="point">3D point.</param>
    /// <param name="rotation">Rotation angle.</param>
    /// <param name="tilt">Tilt angle.</param>
    /// <returns>Stage position.</returns>
    public static StagePosition GetStagePosition(this Point3d point, Angle rotation, Angle tilt) => new()
    {
        X = Length.FromNanometers(point.X),
        Y = Length.FromNanometers(point.Y),
        Z = Length.FromNanometers(point.Z),
        Rotation = rotation,
        Tilt = tilt
    };

    /// <summary>
    /// Get 3x1 Mat with MatType.CV_64FC1 from 2D point.
    /// </summary>
    /// <param name="point">2D image point.</param>
    /// <returns>Mat 3x1 contained 2D homogenous point.</returns>
    public static Mat GetHomogenousMatFrom2DPoint(this Point2d point)
    {
        var mat = new Mat(3, 1, MatType.CV_64FC1);
        mat.Set<double>(0, 0, point.X);
        mat.Set<double>(1, 0, point.Y);
        mat.Set<double>(2, 0, 1.0);
        return mat;
    }

    /// <summary>
    /// Get 3D point from StagePosition in nanometers.
    /// </summary>
    /// <param name="point5D">Stage position 5D point.</param>
    /// <returns>3D point in nanometers.</returns>
    public static Point3d Get3DPointInNanometers(this StagePosition point5D) =>
        new((double)point5D.X.Nanometers, (double)point5D.Y.Nanometers, (double)point5D.Z.Nanometers);

    /// <summary>
    /// Get 2f points from array 2d points.
    /// </summary>
    /// <param name="points">Array of 2d points.</param>
    /// <returns>Array of 2f points.</returns>
    public static Point2f[] Get2fPointsFrom2dPoints(this List<DoublePoint> points)
        => [.. points.Select(point => new Point2f((float)point.X, (float)point.Y))];

    /// <summary>
    /// Get position by top left image corner. Update by half image sizes.
    /// </summary>
    /// <param name="point2d">Image position measured from image center.</param>
    /// <param name="imageSize">Image size.</param>
    /// <returns>Image position measured from top left image corner, updated for image size.</returns>
    public static Point2d GetPositionFromTopLeftCorner(this Point2d point2d, Size imageSize)
    {
        var xFromCorner = point2d.X + imageSize.Width / 2;
        var yFromCorner = point2d.Y + imageSize.Height / 2;
        return new Point2d(xFromCorner, yFromCorner);
    }

    /// <summary>
    /// Transform point 2d measured from top left corner to point 2d measured from image center. Update by half image sizes.
    /// </summary>
    /// <param name="point2d">Image point measured from image center.</param>
    /// <param name="imageSize">Image size.</param>
    /// <returns>Image point measured from top left image corner, updated for image size.</returns>
    public static Point2d TransformPointFromLeftTopCornerToImageCenter(this Point2d point2d, Size imageSize)
        => new(point2d.X - imageSize.Width / 2, point2d.Y - imageSize.Height / 2);

    /// <summary>
    /// Get image left top corner point.
    /// </summary>
    /// <param name="center">Image center point.</param>
    /// <param name="imageSize">Image size.</param>
    /// <returns>Image left top corner.</returns>
    public static Point2d GetImageLeftTopCorner(this Point2d center, Size imageSize)
        => center.TransformPointFromLeftTopCornerToImageCenter(imageSize);

    /// <summary>
    /// Get 2d point from Length point.
    /// </summary>
    /// <param name="point">Length point.</param>
    /// <returns>2d point.</returns>
    public static Point2d Get2dPointFromLengthPoint(this LengthPoint point)
        => new(point.X.Nanometers, point.Y.Nanometers);

    /// <summary>
    /// Get init vector for YZ plane - stage move vector (simulated move in 2d).
    /// </summary>
    /// <param name="planeCenter">Plane center point.</param>
    /// <returns>Line vector in YZ plane.</returns>
    public static Vec2d GetInitYZMoveVector(this StagePosition planeCenter, bool positiveZTransformation, Angle? pretilt)
    {
        Angle tilt = pretilt ?? planeCenter.Tilt;

        double transformedZ = Math.Sin(tilt.Radians);
        double transformedY = Math.Cos(tilt.Radians);

        return new Vec2d(transformedY, positiveZTransformation ? transformedZ : -transformedZ).Normalize();
    }

    /// <summary>
    /// Map neutral position in stage YZ plane.
    /// </summary>
    /// <param name="pointToMap">Position to map.</param>
    /// <param name="planeCenter">Plane center point.</param>
    /// <param name="moveVector">Holder move vector in YZ direction.</param>
    /// <returns>Position mapped in stage YZ plane.</returns>
    public static StagePosition MapNeutralPointInStageYZPlane(this StagePosition pointToMap, StagePosition planeCenter, Vec2d moveVector)
    {
        if (pointToMap.Y.Equals(planeCenter.Y, Length.Zero) && pointToMap.Z.Equals(planeCenter.Z, Length.Zero))
        {
            return pointToMap with
            {
                Rotation = pointToMap.Rotation.NormalizeAngle(),
                Tilt = pointToMap.Tilt,
            };
        }

        double pointY = pointToMap.Y.Nanometers;
        double centerY = planeCenter.Y.Nanometers;
        double centerZ = planeCenter.Z.Nanometers;

        return new StagePosition()
        {
            X = pointToMap.X,
            Y = Length.FromMeters(Length.FromNanometers(centerY + (moveVector.Item0 * pointY)).Meters),
            Z = Length.FromMeters(Length.FromNanometers(centerZ + (moveVector.Item1 * pointY)).Meters),
            Rotation = pointToMap.Rotation.NormalizeAngle(),
            Tilt = pointToMap.Tilt,
        };
    }

    /// <summary>
    /// Map stage position from YZ plane to neutral point.
    /// </summary>
    /// <param name="pointToMap">Position to map.</param>
    /// <param name="planeCenter">Plane center point.</param>
    /// <param name="moveVector">Holder move vector in YZ direction.</param>
    /// <returns>Position mapped in XY neutral plane.</returns>
    public static StagePosition MapNeutralPointFromStageYZPlane(this StagePosition pointToMap, StagePosition planeCenter, Vec2d moveVector)
    {
        if (pointToMap.Y.Equals(planeCenter.Y, Length.Zero) && pointToMap.Z.Equals(planeCenter.Z, Length.Zero))
        {
            return pointToMap with
            {
                Rotation = pointToMap.Rotation.NormalizeAngle(),
                Tilt = pointToMap.Tilt,
            };
        }

        return pointToMap with
        {
            Y = Length.FromNanometers((pointToMap.Y.Nanometers - planeCenter.Y.Nanometers) / moveVector.Item0),
            Rotation = pointToMap.Rotation.NormalizeAngle(),
            Tilt = pointToMap.Tilt,
        };
    }

    private static Vec2d Normalize(this Vec2d vector)
    {
        double distance = Math.Sqrt(vector.Item0 * vector.Item0 + vector.Item1 * vector.Item1);
        return new Vec2d(vector.Item0 / distance, vector.Item1 / distance);
    }

    /// <summary>
    /// Get points 2d for image warp.
    /// For warp is need 4 points.
    /// </summary>
    /// <param name="originPoints">Secondary image warp points.</param>
    /// <param name="mainWarpPoints">Main image warp points.</param>
    /// <param name="updatedMainWarpPoints">Main image updated warp points.</param>
    /// <returns>Secondary image updated warp points.</returns>
    public static Point2f[] Get2fPointsForWarp(this Point2d[] originPoints, Point2f[] mainWarpPoints, out Point2f[] updatedMainWarpPoints)
    {
        var warpPoints = new List<Point2f>();
        var upMainWarpPoints = new List<Point2f>();
        int minLength = originPoints.Length <= mainWarpPoints.Length ? originPoints.Length : mainWarpPoints.Length;

        for (int i = 0; i < minLength; i++)
        {
            warpPoints.Add(new Point2f((float)originPoints[i].X, (float)originPoints[i].Y));
            upMainWarpPoints.Add(mainWarpPoints[i]);
        }

        if (warpPoints.Count >= 3)
        {
            updatedMainWarpPoints = [.. upMainWarpPoints];
            return [.. warpPoints];
        }

        if (originPoints.Length == 2)
        {
            Point2f[] minMaxWarp = warpPoints.GetMinMaxPoints();
            Point2f[] minMaxMainWarp = upMainWarpPoints.GetMinMaxPoints();

            for (int i = 0; i < minMaxWarp.Length - 1; i++)
            {
                if (!warpPoints.Contains(minMaxWarp[i]) && !upMainWarpPoints.Contains(minMaxMainWarp[i]))
                {
                    warpPoints.Add(minMaxWarp[i]);
                    upMainWarpPoints.Add(minMaxMainWarp[i]);
                    break;
                }
            }
        }

        updatedMainWarpPoints = [.. upMainWarpPoints];
        return [.. warpPoints];
    }

    private static Point2f[] GetMinMaxPoints(this List<Point2f> points)
    {
        var minX = points.Min(point => point.X);
        var minY = points.Min(point => point.Y);
        var maxX = points.Max(point => point.X);
        var maxY = points.Max(point => point.Y);

        return
        [
            new Point2f(minX, minY),
            new Point2f(maxX, minY),
            new Point2f(maxX, maxY),
            new Point2f(minX, maxY),
            new Point2f(minX, maxY - ((maxY - minY) / 2)),
            new Point2f(minX + ((maxX - minX) / 2), maxY),
            new Point2f(minX + ((maxX - minX) / 2), maxY - ((maxY - minY) / 2)),
        ];
    }
}
