using Tarmi.Imaging.Common;
using Tarmi.Models;
using Tarmi.PointMapping;
using Tarmi.Configuration;
using Tarmi.Configuration.Alignments;
using Tarmi.Configuration.Holders;
using Tarmi.Projects;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using UnitsNet;
using static Tarmi.PointMapping.MapPointExtensions;

namespace Tarmi.VirtualDevices.Implementation;

internal class StageNavigation : IStageNavigation
{
    private static readonly Angle Angle180 = Angle.FromDegrees(180);
    private static readonly Angle Angle52 = Angle.FromDegrees(52);

    private readonly ILogger _logger;
    private readonly IDisposable _disposable;
    private readonly InstrumentAlignment _alignment;
    private Holder? _holder;
    private OpenCvSharp.Vec2d _semMoveVector;
    private OpenCvSharp.Vec2d _lmMoveVector;
    private OpenCvSharp.Vec2d _fibRightAngleMoveVector;
    private OpenCvSharp.Vec2d _fibMillingMoveVector;
    private OpenCvSharp.Vec2d _confocalMoveVector;
    private StagePosition? _semPlanePosition;
    private StagePosition? _lmPlanePosition;
    private StagePosition? _fibMillingPlanePosition;
    private StagePosition? _fibRightAnglePlanePosition;
    private StagePosition? _confocalPlanePosition;

    public StageNavigation(ILogger<StageNavigation> logger, IProjectManager projectManager, ApplicationConfig applicationConfig)
    {
        _logger = logger;
        _alignment = applicationConfig.Microscope.Alignment;
        _holder = projectManager.GetActiveProject()?.Holder;
        _disposable = projectManager.ActiveProject.Subscribe(project => AssignHolder(project?.Holder));
    }

    private bool IsNotInitialized => _holder is null;

    private void AssignHolder(Holder? holder)
    {
        if (holder is null)
        {
            // project was closed ignore or app is starting
            return;
        }

        _holder = holder;
        _semPlanePosition = holder.SemModePlanePoint;
        _lmPlanePosition = holder.LmModePlanePoint;
        _fibRightAnglePlanePosition = holder.FibRightAngleModePlanePoint;
        _fibMillingPlanePosition = holder.FibMillingModePlanePoint;
        _confocalPlanePosition = holder.ConfocalModePlanePoint;

        _semMoveVector = _semPlanePosition.GetInitYZMoveVector(false, null);
        _lmMoveVector = _lmPlanePosition.GetInitYZMoveVector(true, _holder.PreTilt);
        _fibRightAngleMoveVector = _fibRightAnglePlanePosition.GetInitYZMoveVector(true, _holder.PreTilt);
        _fibMillingMoveVector = _fibMillingPlanePosition.GetInitYZMoveVector(false, null);
        _confocalMoveVector = _confocalPlanePosition.GetInitYZMoveVector(true, _holder.PreTilt);
    }

    private StageCameraAlignment GetViewAlignment(StageCameraView sourceView)
    {
        return sourceView switch
        {
            StageCameraView.SEM => _alignment.Sem,
            StageCameraView.FIB_Milling => _alignment.FibMilling,
            StageCameraView.FIB_RightAngle => _alignment.FibRightAngle,
            StageCameraView.LM => _alignment.Lm,
            StageCameraView.Confocal => _alignment.Confocal,
            _ => throw new NotImplementedException()
        };
    }

    private static Angle CalculateViewTilt(StageCameraAlignment alignment, Angle preTilt, StageCameraView targetView)
    {
        return targetView switch
        {
            StageCameraView.SEM or StageCameraView.FIB_Milling => preTilt + alignment.OffsetTilt,
            StageCameraView.FIB_RightAngle or StageCameraView.LM or StageCameraView.Confocal => Angle52 - preTilt + alignment.OffsetTilt,
            _ => throw new NotImplementedException()
        };
    }

    private StagePosition ConvertViewPositionToViewNeutralPosition(StagePosition stagePosition, StageCameraView sourceView)
    {
        if (sourceView == StageCameraView.Unknown)
        {
            return stagePosition;
        }

        var alignment = GetViewAlignment(sourceView);
        var newPosition = sourceView switch
        {
            StageCameraView.SEM => stagePosition.MapNeutralPointFromStageYZPlane(_semPlanePosition!, _semMoveVector),
            StageCameraView.LM => stagePosition.MapNeutralPointFromStageYZPlane(_lmPlanePosition!, _lmMoveVector),
            StageCameraView.FIB_RightAngle => stagePosition.MapNeutralPointFromStageYZPlane(_fibRightAnglePlanePosition!, _fibRightAngleMoveVector),
            StageCameraView.FIB_Milling => stagePosition.MapNeutralPointFromStageYZPlane(_fibMillingPlanePosition!, _fibMillingMoveVector),
            StageCameraView.Confocal => stagePosition.MapNeutralPointFromStageYZPlane(_confocalPlanePosition!, _confocalMoveVector),
            _ => throw new NotSupportedException()
        };

        newPosition = newPosition with
        {
            X = newPosition.X - alignment.OffsetX,
            Y = newPosition.Y - alignment.OffsetY,
        };

        var xReverse = alignment.OffsetRotation.IsInTolerance(Angle180);
        newPosition = newPosition with
        {
            X = xReverse ? -newPosition.X : newPosition.X,
            Y = xReverse ? newPosition.Y : -newPosition.Y
        };


        return newPosition;
    }

    private StagePosition ConvertNeutralPositionToCameraViewPosition(StagePosition stagePosition, StageCameraView targetView)
    {
        var alignment = GetViewAlignment(targetView);
        var newPosition = stagePosition with
        {
            X = stagePosition.X,
            Y = stagePosition.Y,
            Z = Length.Zero,
            Rotation = alignment.OffsetRotation,
            Tilt = CalculateViewTilt(alignment, _holder!.PreTilt, targetView)
        };

        var xReverse = alignment.OffsetRotation.IsInTolerance(Angle180);
        newPosition = newPosition with
        {
            X = xReverse ? -newPosition.X : newPosition.X,
            Y = xReverse ? newPosition.Y : -newPosition.Y
        };

        newPosition = newPosition with
        {
            X = newPosition.X + alignment.OffsetX,
            Y = newPosition.Y + alignment.OffsetY,
        };

        return targetView switch
        {
            StageCameraView.SEM => newPosition.MapNeutralPointInStageYZPlane(_semPlanePosition!, _semMoveVector),
            StageCameraView.LM => newPosition.MapNeutralPointInStageYZPlane(_lmPlanePosition!, _lmMoveVector),
            StageCameraView.FIB_RightAngle => newPosition.MapNeutralPointInStageYZPlane(_fibRightAnglePlanePosition!, _fibRightAngleMoveVector),
            StageCameraView.FIB_Milling => newPosition.MapNeutralPointInStageYZPlane(_fibMillingPlanePosition!, _fibMillingMoveVector),
            StageCameraView.Confocal => newPosition.MapNeutralPointInStageYZPlane(_confocalPlanePosition!, _confocalMoveVector),
            _ => throw new NotSupportedException()
        };
    }

    private LengthRectangle GetImagePlaneDimensions(ImageMetadata metadata)
    {
        var imageStagePosition = metadata.GetStagePosition();
        var imageCameraView = metadata.GetSource();
        var imagePlaneCenter = GetPlanePosition(imageStagePosition, imageCameraView);
        var fieldWidthHalf = (((double)metadata.Coordinates.ImageSize.Width / 2.0) * metadata.Coordinates.PixelSize.X);
        var fieldHeightHalf = (((double)metadata.Coordinates.ImageSize.Height / 2.0) * metadata.Coordinates.PixelSize.Y);
        return new LengthRectangle
        {
            Left = imagePlaneCenter.X - fieldWidthHalf,
            Right = imagePlaneCenter.X + fieldWidthHalf,
            Top = imagePlaneCenter.Y - fieldHeightHalf,
            Bottom = imagePlaneCenter.Y + fieldHeightHalf
        };
    }

    public StagePosition TransformPosition(StagePosition position, StageCameraView sourceView, StageCameraView targetView)
    {
        Guard.IsFalse(targetView == StageCameraView.Unknown);
        if (IsNotInitialized)
        {
            return StagePosition.Zero;
        }

        if (sourceView == StageCameraView.Unknown)
        {
            return GetInitialStageCenterPosition(targetView);
        }

        var neutralPosition = ConvertViewPositionToViewNeutralPosition(position, sourceView);
        var targetPosition = ConvertNeutralPositionToCameraViewPosition(neutralPosition, targetView);

        _logger.LogInformation(
            "Transform Position {SourceView} -> {TargetView}, {SourcePosition} -> {NeutralPosition} -> {TargetPosition}",
            sourceView, targetView, position, neutralPosition, targetPosition
        );

        return targetPosition;
    }

    public StagePosition GetInitialStageCenterPosition(StageCameraView targetView)
    {
        Guard.IsFalse(targetView == StageCameraView.Unknown);

        if (IsNotInitialized)
        {
            return StagePosition.Zero;
        }

        LengthPoint gridPosition;
        if (_holder!.Grids.Count > 0)
        {
            gridPosition = _holder!.Grids[0].GetDefaultViewPosition();
            _logger.LogWarning("Selecting first grid navigation position.");
        }
        else
        {
            gridPosition = LengthPoint.Zero;
            _logger.LogWarning("No grids found in the holder.");
        }
        var targePosition = GetStagePosition(gridPosition, targetView);
        return targePosition;
    }

    public StagePosition GetStagePosition(LengthPoint planePosition, StageCameraView targetView)
    {
        Guard.IsFalse(targetView == StageCameraView.Unknown);
        if (IsNotInitialized)
        {
            return StagePosition.Zero;
        }

        var neutralPosition = new StagePosition
        {
            X = planePosition.X,
            Y = planePosition.Y,
            Z = Length.Zero,
            Rotation = Angle.Zero,
            Tilt = Angle.Zero
        };

        var targetPosition = ConvertNeutralPositionToCameraViewPosition(neutralPosition, targetView);

        _logger.LogInformation(
            "GetStagePosition PlanePosition -> {TargetView}, {PlanePosition} -> {TargetPosition}",
            targetView, planePosition, targetPosition
        );
        return targetPosition;
    }

    public LengthPoint GetPlanePosition(StagePosition stagePosition, StageCameraView sourceView)
    {
        if (IsNotInitialized || sourceView == StageCameraView.Unknown)
        {
            _logger.LogWarning(
                "GetPlanePosition {StagePosition} [{CameraView}] -> _holder is not initialized or camera view is Unknown.",
                stagePosition, sourceView
            );

            return LengthPoint.Zero;
        }

        var neutralPosition = ConvertViewPositionToViewNeutralPosition(stagePosition, sourceView);
        var planePosition = new LengthPoint
        {
            X = neutralPosition.X,
            Y = neutralPosition.Y
        };

        _logger.LogDebug(
            "GetPlanePosition {StagePosition} [{CameraView}] -> {PlanePosition}",
            stagePosition, sourceView, planePosition
        );

        return planePosition;
    }

    public LengthPoint GetPlanePositionFromImageLocation(RatioPoint imagePosition, ImageMetadata metadata)
    {
        // image axes orientation
        // x - left to right
        // y - top to bottom
        var planeRectangle = GetImagePlaneDimensions(metadata);
        var finalX = planeRectangle.Left + imagePosition.X.DecimalFractions * (planeRectangle.Right - planeRectangle.Left);
        var finalY = planeRectangle.Top + imagePosition.Y.DecimalFractions * (planeRectangle.Bottom - planeRectangle.Top);
        var planePosition = new LengthPoint
        {
            X = finalX,
            Y = finalY
        };

        _logger.LogInformation(
            "GetPlanePositionFromImageLocation [{RatioPoint}], {StagePosition} [{CameraView}] {ImageSize}, {ImagePixelSize} -> {PlanePosition}",
            imagePosition, metadata.GetStagePosition(), metadata.GetSource(), metadata.Coordinates.ImageSize, metadata.GetPixelSize(), planePosition
        );

        return planePosition;
    }

    public StagePosition GetStagePositionFromImageLocation(RatioPoint imagePosition, ImageMetadata metadata, StageCameraView targetView)
    {
        Guard.IsFalse(targetView == StageCameraView.Unknown);

        var planePosition = GetPlanePositionFromImageLocation(imagePosition, metadata);
        var neutralPosition = new StagePosition
        {
            X = planePosition.X,
            Y = planePosition.Y,
            Z = Length.Zero,
            Rotation = Angle.Zero,
            Tilt = Angle.Zero
        };

        var stagePosition = ConvertNeutralPositionToCameraViewPosition(neutralPosition, targetView);

        _logger.LogInformation(
           "GetStagePositionFromImageLocation [{RatioPoint}], {StagePosition} [{CameraView}] {ImageSize}, {ImagePixelSize} -> {PlanePosition} -> {TranslatedStagePosition} [{TargetCameraView}]",
           imagePosition, metadata.GetStagePosition(), metadata.GetSource(), metadata.Coordinates.ImageSize, metadata.GetPixelSize(), planePosition, stagePosition, targetView
       );

        return stagePosition;
    }

    public StagePosition GetStagePositionFromPoint(DoublePoint imagePoint, ImageMetadata metadata, StageCameraView targetView)
    {
        Guard.IsFalse(targetView == StageCameraView.Unknown);

        var imagePosition = new RatioPoint
        {
            X = Ratio.FromDecimalFractions(imagePoint.X / metadata.Coordinates.ImageSize.Width),
            Y = Ratio.FromDecimalFractions(imagePoint.Y / metadata.Coordinates.ImageSize.Height)
        };

        _logger.LogInformation("GetStagePositionFromPoint: {ImagePoint}", imagePoint);

        return GetStagePositionFromImageLocation(imagePosition, metadata, targetView);
    }

    public DoublePoint GetImageLocationFromStagePosition(StagePosition stagePosition, ImageMetadata imageMetadata, StageCameraView stageView)
    {
        Guard.IsFalse(stageView == StageCameraView.Unknown);

        var planePosition = GetPlanePosition(stagePosition, stageView);
        return GetImageLocationFromPlanePosition(planePosition, imageMetadata);
    }

    public DoublePoint GetImageLocationFromPlanePosition(LengthPoint planePosition, ImageMetadata imageMetadata)
    {
        var imageDimensions = GetImagePlaneDimensions(imageMetadata);
        if (!imageDimensions.IsPointInsideRectangle(planePosition))
        {
            return DoublePoint.Invalid;
        }

        // image axes orientation
        // x - left to right
        // y - top to bottom
        var xRatio = (planePosition.X - imageDimensions.Left) / (imageDimensions.Right - imageDimensions.Left);
        var yRatio = (planePosition.Y - imageDimensions.Top) / (imageDimensions.Bottom - imageDimensions.Top);
        var imageLocation = new DoublePoint
        {
            X = imageMetadata.Coordinates.ImageSize.Width * xRatio,
            Y = imageMetadata.Coordinates.ImageSize.Height * yRatio
        };

        _logger.LogInformation("GetImageLocationFromPlanePosition {PlanePosition}, {StagePosition} [{CameraView}] {ImageSize}, {ImagePixelSize} -> {ImageLocation}",

            planePosition, imageMetadata.GetStagePosition(), imageMetadata.GetSource(), imageMetadata.Coordinates.ImageSize, imageMetadata.GetPixelSize(), imageLocation
         );

        return imageLocation;
    }

    public bool IsStagePositionValidForView(StagePosition position, StageCameraView view)
    {
        Guard.IsFalse(view == StageCameraView.Unknown);

        var alignment = GetViewAlignment(view);

        var expectedTilt = CalculateViewTilt(alignment, _holder!.PreTilt, view);
        var absoluteTiltRange = _holder.SafeTiltRange with
        {
            Min = expectedTilt + _holder.SafeTiltRange.Min,
            Max = expectedTilt + _holder.SafeTiltRange.Max
        };

        return
            alignment.OffsetRotation.IsInTolerance(position.Rotation) &&
            absoluteTiltRange.IsValueInRange(position.Tilt);
    }

    public IReadOnlyDictionary<StageCameraView, Angle> GetViewsPretilt()
    {
        var values = new Dictionary<StageCameraView, Angle>();
        if (IsNotInitialized)
        {
            return values;
        }

        foreach (var view in Enum.GetValues<StageCameraView>())
        {
            if (view == StageCameraView.Unknown)
            {
                values.Add(view, _holder!.PreTilt);
                continue;
            }
            else
            {
                var angle = CalculateViewTilt(GetViewAlignment(view), _holder!.PreTilt, view);
                values.Add(view, angle);
            }
        }

        return values;
    }

    public bool IsPlanePositionInImage(LengthPoint planePosition, ImageMetadata metadata)
    {
        if (IsNotInitialized || metadata.ImageId == Guid.Empty)
        {
            return false;
        }

        var imagePlaneRect = GetImagePlaneDimensions(metadata);
        return imagePlaneRect.IsPointInsideRectangle(planePosition);
    }

    public void Dispose() => _disposable.Dispose();

    public Length SafeUnknownMoveZ => _holder?.SafeUnknownMoveZ ?? Length.FromMeters(0.020);
}
