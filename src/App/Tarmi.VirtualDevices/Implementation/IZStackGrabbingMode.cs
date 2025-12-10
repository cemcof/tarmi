using Tarmi.Imaging.Algorithms.Utilities;
using Tarmi.Imaging.Common;
using Tarmi.Imaging.Common.Metadata.Luminescence;
using Tarmi.Models;
using Tarmi.Projects;
using UnitsNet;

namespace Tarmi.VirtualDevices.Implementation;

public interface IZStackGrabbingMode : ILuminescenceTubeControllingMode
{
    async Task GrabZStackAsync(ZStackOptions options, StageCameraView stageCameraView, IImagingPipelineGrabber pipelineGrabber, Action<ImageWithMetadata, int> saveAction, Action<ImageWithMetadata> saveMipAction, IProgress<(string, Ratio)> progress, CancellationToken cancellationToken)
    {
        Length initialPosition = CurrentLinearStagePosition;
        var defaultStackInfo = new StackInfo()
        {
            Step = options.Step,
            StepCount = options.NumberOfSteps,
        };

        ImageWithMetadata maxIntensityImage = ImageWithMetadata.Empty;

        for (int stepIndex = 0; stepIndex < options.NumberOfSteps; stepIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress.Report(($"Acquiring z-stack {stepIndex + 1}/{options.NumberOfSteps}", Ratio.FromDecimalFractions(stepIndex) / (options.NumberOfSteps + 1)));

            var currentPosition = options.StartPosition - stepIndex * options.Step;
            await MoveLinearStageToAsync(currentPosition, cancellationToken);

            // TODO: Is double dispose ok?
            using var temp = await pipelineGrabber.GrabOneWithResultCopyAsync(ImageProcessingStage.FilteredInput);
            using var image =
            stageCameraView == StageCameraView.LM
                ? temp with
                {
                    LuminescenceMetadata = temp.LuminescenceMetadata! with
                    {
                        StackInfo = defaultStackInfo with
                        {
                            CurrentStep = stepIndex,
                        }
                    }
                }
                : temp with
                {
                    ConfocalMetadata = temp.ConfocalMetadata! with
                    {
                        StackInfo = new Tarmi.Imaging.Common.Metadata.Confocal.StackInfo()
                        {
                            Step = options.Step,
                            StepCount = options.NumberOfSteps,
                            CurrentStep = stepIndex,
                        }
                    }
                };
            saveAction(image, stepIndex);
            maxIntensityImage = stepIndex == 0
                ? image with { ImageId = Guid.NewGuid(), Image = image.Image.Clone() }
                : Overlays.GetMaxIntensityImage(maxIntensityImage, image);
        }

        progress.Report(("Saving z-stack MIP image", Ratio.FromDecimalFractions(options.NumberOfSteps) / (options.NumberOfSteps + 1)));

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
