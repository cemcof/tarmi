using Betrian.Imaging.Common;
using Betrian.Models;
using UnitsNet;

namespace CFLMnavi.VirtualDevices;

public interface IStageNavigation : IDisposable
{
    /// <summary>
    /// Transform stage position from source view in target view.
    /// </summary>
    /// <param name="position">Stage position.</param>
    /// <param name="sourceView">Source view.</param>
    /// <param name="targetView">Target view.</param>
    /// <returns>Stage position in target view.</returns>
    StagePosition TransformPosition(StagePosition position, StageCameraView sourceView, StageCameraView targetView);

    /// <summary>
    /// Get initial safe stage center position for view.
    /// </summary>
    /// <param name="targetView">Target view.</param>
    /// <returns>Stage position in target view.</returns>
    StagePosition GetInitialStageCenterPosition(StageCameraView targetView);

    /// <summary>
    /// Get stage position from plane position in absolute coordinates.
    /// </summary>
    /// <param name="planePosition">Plane position in absolute coordinates.</param>
    /// <param name="targetView">Target view.</param>
    /// <returns>Stage position in target view.</returns>
    StagePosition GetStagePosition(LengthPoint planePosition, StageCameraView targetView);

    /// <summary>
    /// Get plane position - absolute X, Y position.
    /// </summary>
    /// <param name="stagePosition">Stage position.</param>
    /// <param name="sourceView">Source view.</param>
    /// <returns>Plane position in absolute coordinate system.</returns>
    LengthPoint GetPlanePosition(StagePosition stagePosition, StageCameraView sourceView);

    /// <summary>
    /// Get plane position from image location.
    /// </summary>
    /// <param name="imagePosition">Image position in ratio point.</param>
    /// <param name="metadata"Image metadata.</param>
    /// <returns>Plane position in absolute coordinate system.</returns>
    LengthPoint GetPlanePositionFromImageLocation(RatioPoint imagePosition, ImageMetadata metadata);

    /// <summary>
    /// Get stage position from image location.
    /// </summary>
    /// <param name="imagePosition">Image position in ratio point.</param>
    /// <param name="metadata">Image metadata.</param>
    /// <param name="targetView">Stage target view.</param>
    /// <returns>Stage position of image point in target view.</returns>
    StagePosition GetStagePositionFromImageLocation(RatioPoint imagePosition, ImageMetadata metadata, StageCameraView targetView);


    /// <summary>
    /// Get stage position from image pixel location.
    /// </summary>
    /// <param name="imagePoint">Image point in pixels, measured from left up corner.</param>
    /// <param name="metadata">Image metadata.</param>
    /// <param name="targetView">Stage target view.</param>
    /// <returns>Stage position of image point in target view.</returns>
    StagePosition GetStagePositionFromPoint(DoublePoint imagePoint, ImageMetadata metadata, StageCameraView targetView);

    /// <summary>
    /// CHeck whether stage position is valid for view.
    /// </summary>
    /// <param name="position">Stage position.</param>
    /// <param name="view">Stage view to check.</param>
    /// <returns>True in case the position is valid for the view, False otherwise.</returns>
    bool IsStagePositionValidForView(StagePosition position, StageCameraView view);

    /// <summary>
    /// Is plane position (in absolute coordinates) in image.
    /// </summary>
    /// <param name="planePosition">Plane position in absolute coordinates.</param>
    /// <param name="metadata">Image metadata.</param>
    /// <returns>True if position in in the image.</returns>
    bool IsPlanePositionInImage(LengthPoint planePosition, ImageMetadata metadata);

    /// <summary>
    /// Get image location (pixels from left up corner) from stage position.
    /// </summary>
    /// <param name="stagePosition">Stage position in source view coordinates.</param>
    /// <param name="imageMetadata">Image metadata.</param>
    /// <returns>Image point in pixels, measured from left up corner.</returns>
    DoublePoint GetImageLocationFromStagePosition(StagePosition stagePosition, ImageMetadata imageMetadata, StageCameraView stageView);

    /// <summary>
    /// Get image location (pixels from left up corner) from plane position.
    /// </summary>
    /// <param name="planePosition">Plane position in absolute coordinates.</param>
    /// <param name="imageMetadata">Image metadata.</param>
    /// <returns>Image point in pixels, measured from left up corner.</returns>
    DoublePoint GetImageLocationFromPlanePosition(LengthPoint planePosition, ImageMetadata imageMetadata);

    /// <summary>
    /// Get pretilt for all views with alignments.
    /// </summary>
    /// <returns>List of views pretilts.</returns>
    IReadOnlyDictionary<StageCameraView, Angle> GetViewsPretilt();

    /// <summary>
    /// Safe stage Z position for safe move between modes or during initialization.
    /// </summary>
    Length SafeUnknownMoveZ { get; }
}
