using Betrian.Imaging.Algorithms.Utilities;
using Betrian.Imaging.Common;
using Betrian.Imaging.Common.Metadata.Luminescence;
using Betrian.Models;
using UnitsNet;

namespace CFLMnavi.VirtualDevices.Implementation;

public class ZStackSettings
{
    public Length StartPosition { get; init; }
    public Length Step { get; init; }
    public int NumberOfSteps { get; init; }
}

public interface IZStackGrabbingMode : ILuminescenceTubeControllingMode
{
    public async Task GrabZStackAsync(ZStackSettings stackSettings, StageCameraView stageCameraView, IImagingPipelineGrabber pipelineGrabber, Action<ImageWithMetadata, int> saveAction, Action<ImageWithMetadata> saveMipAction, IProgress<(string, Ratio)> progress, CancellationToken cancellationToken)
    {
        Length initialPosition = CurrentLinearStagePosition;
        var defaultStackInfo = new StackInfo()
        {
            Step = stackSettings.Step,
            StepCount = stackSettings.NumberOfSteps,
        };

        ImageWithMetadata maxIntensityImage = ImageWithMetadata.Empty;

        for (int stepIndex = 0; stepIndex < stackSettings.NumberOfSteps; stepIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress.Report(($"Acquiring z-stack {stepIndex + 1}/{stackSettings.NumberOfSteps}", Ratio.FromDecimalFractions(stepIndex) / (stackSettings.NumberOfSteps + 1)));

            var currentPosition = stackSettings.StartPosition - stepIndex * stackSettings.Step;
            await MoveLinearStageToAsync(currentPosition, cancellationToken);

            // TODO: Is double dispose ok?
            using var temp = await pipelineGrabber.GrabOneWithResultCopyAsync(ImageProcessingStage.FilteredInput);
            using var image = temp with
            {
                LuminescenceMetadata = temp.LuminescenceMetadata! with
                {
                    StackInfo = defaultStackInfo with
                    {
                        CurrentStep = stepIndex,
                    }
                }
            };

            saveAction(image, stepIndex);
            maxIntensityImage = stepIndex == 0
                ? image with { ImageId = Guid.NewGuid(), Image = image.Image.Clone() }
                : Overlays.GetMaxIntensityImage(maxIntensityImage, image);
        }

        progress.Report(("Saving z-stack MIP image", Ratio.FromDecimalFractions(stackSettings.NumberOfSteps) / (stackSettings.NumberOfSteps + 1)));

        saveMipAction(maxIntensityImage with
        {
            LuminescenceMetadata = maxIntensityImage.LuminescenceMetadata! with
            {
                StackInfo = defaultStackInfo with
                {
                    CurrentStep = -1,
                }
            }
        });

        await MoveLinearStageToAsync(initialPosition, cancellationToken);
        await pipelineGrabber.GrabOneAsync();
    }
}
