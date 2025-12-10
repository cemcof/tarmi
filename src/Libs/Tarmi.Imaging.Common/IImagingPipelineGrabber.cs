namespace Tarmi.Imaging.Common;

public interface IImagingPipelineGrabber
{
    /// <summary>
    /// Grabs one image from the device to pipeline and returns a required processed stage copy.
    /// </summary>
    /// <param name="processingStage">Processing stage for required image.</param>
    /// <returns>Image with metadata.</returns>
    Task<ImageWithMetadata> GrabOneWithResultCopyAsync(ImageProcessingStage processingStage = ImageProcessingStage.FilteredInput);

    /// <summary>
    /// Grabs one image from the device to pipeline.
    /// </summary>
    /// <returns>Awaitable Task.</returns>
    Task GrabOneAsync();

    /// <summary>
    /// Gets image from the pipeline and returns a required processed stage copy, when no input image is available in pipeline it waits till input is grabbed or set.
    /// </summary>
    /// <param name="processingStage">Processing stage for required image.</param>
    /// <returns>Image with metadata.</returns>
    Task<ImageWithMetadata> GetImageCopyAsync(ImageProcessingStage processingStage);
}
