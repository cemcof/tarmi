using Betrian.Imaging.Algorithms.Focus;
using Betrian.Imaging.Algorithms.Focus.Sharpness;
using Betrian.Imaging.Common;
using Betrian.Models;
using CFLMnavi.Configuration;
using CFLMnavi.Configuration.Application;
using CFLMnavi.Projects;
using CFLMnavi.VirtualDevices;
using DynamicData;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using UnitsNet;
using UnitsNet.Units;

namespace Betrian.CflmNavi.App.Services.Application;
public class FocusingService
{
    private readonly ILogger _logger;
    private readonly IVirtualDevice _virtualDevice;
    private readonly ILimits _limits;
    private readonly FocusFunctions _focusConfig;

    public FocusingService(ILogger logger, IVirtualDevice virtualDevice, ILimits limits, ApplicationConfig applicationConfig)
    {
        _logger = logger;
        _virtualDevice = virtualDevice;
        _limits = limits;
        _focusConfig = applicationConfig.UserPreferences.Algorithms.FocusFunctions;
    }

    private static readonly Duration LowDwellTime = Duration.FromNanoseconds(300);

    public async Task FocusAutomaticallyAsync(IStageNavigation stageNavigation, ISafeStageControlling safeStageControlling, IImagingPipelineGrabber pipelineGrabber, List<FocusPoint> focusPoints, IProgress<(string, Ratio)> progress, CancellationToken cancellationToken)
    {
        _virtualDevice.StopGrabbing();

        using var image = await pipelineGrabber.GrabOneWithResultCopyAsync();
        List<FocusPoint> focusPointsInImage = focusPoints.Where(point => stageNavigation.IsPlanePositionInImage(point.PlaneLocation, image)).ToList();
        Length optimalPosition;
        switch (_virtualDevice)
        {
            case IBeamMode beamMode when focusPointsInImage is []:
                {
                    var reducedArea = new RatioRectangle
                    {
                        Left = Ratio.FromDecimalFractions(0.25),
                        Top = Ratio.FromDecimalFractions(0.25),
                        Right = Ratio.FromDecimalFractions(0.75),
                        Bottom = Ratio.FromDecimalFractions(0.75)
                    };
                    using var dwellTimeGuard = beamMode.UseFullFrameSettings(LowDwellTime, Devices.Thermofisher.Instrument.Types.ImageFilterType.None, 1, 1);
                    //using var reducedAreaGuard = beamMode.UseReducedArea(reducedArea, LowDwellTime, Devices.Thermofisher.Instrument.Types.ImageFilterType.None, 1, 1);
                    optimalPosition = await BeamFocusAutomaticallyAsync(pipelineGrabber, progress, cancellationToken);
                }
                break;
            case IBeamMode beamMode:
                {
                    using var dwellTimeGuard = beamMode.UseFullFrameSettings(LowDwellTime, Devices.Thermofisher.Instrument.Types.ImageFilterType.None, 1, 1);
                    optimalPosition = await FocusAutomaticallyWithFocusPointsAsync<LinearSearch>(stageNavigation, safeStageControlling, pipelineGrabber, focusPointsInImage, progress, cancellationToken);
                }
                break;
            default:
                optimalPosition = await FocusAutomaticallyWithFocusPointsAsync<TertiumSearch>(stageNavigation, safeStageControlling, pipelineGrabber, focusPointsInImage, progress, cancellationToken);
                break;
        }

        await _virtualDevice.FocusAtAsync(optimalPosition, cancellationToken);

        foreach (FocusPoint point in focusPointsInImage)
        {
            FocusPoint updatedPoint = point with { Z = optimalPosition };
            focusPoints.Replace(point, updatedPoint);
        }

        await pipelineGrabber.GrabOneAsync();
    }

    private async Task<Length> BeamFocusAutomaticallyAsync(IImagingPipelineGrabber pipelineGrabber, IProgress<(string, Ratio)> progress, CancellationToken cancellationToken)
    {
        var range = _limits.GetAutoFocusRangeForActiveBeam();
        return await LinearSearch.FindMaximumAsync<Length, LengthUnit>((workingDistance, cancellationToken) => FocusAtReducedAndCalculateFocusMetric(pipelineGrabber, workingDistance, cancellationToken), range, progress, _logger, cancellationToken);
    }

    private async Task<double> FocusAtReducedAndCalculateFocusMetric(IImagingPipelineGrabber pipelineGrabber, Length focusLength, CancellationToken cancellationToken)
    {
        await _virtualDevice.FocusAtAsync(focusLength, cancellationToken);
        using var reducedImage = await pipelineGrabber.GrabOneWithResultCopyAsync(ImageProcessingStage.FilteredInput);
        var size = reducedImage.Image.Size;
        using var area = reducedImage.Image.Mat.SubMat(new Rect()
        {
            X = size.Width / 4,
            Y = size.Height / 4,
            Width = size.Width / 2,
            Height = size.Height / 2,
        });
        return Tenengrad.CalculateIndex(area);
    }

    private async Task<Length> FocusAutomaticallyWithFocusPointsAsync<TSearch>(IStageNavigation stageNavigation, ISafeStageControlling safeStageControlling, IImagingPipelineGrabber pipelineGrabber, List<FocusPoint> relevantFocusPoints, IProgress<(string, Ratio)> progress, CancellationToken cancellationToken)
        where TSearch : IMaximumSearch
    {
        using var image = await pipelineGrabber.GrabOneWithResultCopyAsync(ImageProcessingStage.FilteredInput);

        var range = _limits.GetAutoFocusRangeForActiveBeam();
        return await TSearch.FindMaximumAsync<Length, LengthUnit>(
            (position, cancellationToken) => FocusAtAndCalculateFocusMetric(stageNavigation, safeStageControlling, pipelineGrabber, relevantFocusPoints, position, cancellationToken),
        range, progress, _logger, cancellationToken);
    }


    private async Task<double> FocusAtAndCalculateFocusMetric(IStageNavigation stageNavigation, ISafeStageControlling safeStageControlling, IImagingPipelineGrabber pipelineGrabber, List<FocusPoint> focusPoints, Length focusLength, CancellationToken cancellationToken)
    {
        await _virtualDevice.FocusAtAsync(focusLength, cancellationToken);
        using var image = await pipelineGrabber.GrabOneWithResultCopyAsync();
        var topLeftCorners = GetCurrentFocusAreasTopLeftCorners(stageNavigation, safeStageControlling, focusPoints, image);
        var focusSize = GetFocusAreaSize(image.Image.Size);

        // Use the whole image when no focus point is present.
        if (topLeftCorners is [])
        {
            return Tenengrad.CalculateIndex(image.Image.Mat);
        }

        // Harmonic mean
        var sum = topLeftCorners.Sum(corner =>
        {
            var focusArea = new Rect(corner, focusSize);
            using var subMat = image.Image.Mat.SubMat(focusArea);
            return 1.0 / Tenengrad.CalculateIndex(subMat);
        });
        return topLeftCorners.Length / sum;
    }

    private Point[] GetCurrentFocusAreasTopLeftCorners(IStageNavigation stageNavigation, ISafeStageControlling safeStageControlling, IEnumerable<FocusPoint> focusPoints, ImageWithMetadata image)
    {
        var position = stageNavigation.GetPlanePosition(image.GetStagePosition(), safeStageControlling.ActiveCameraView);
        var imageSize = image.Image.Size;
        var focusSize = GetFocusAreaSize(imageSize);
        var fieldSize = image.GetFieldSize();
        return focusPoints
            .Where(point => stageNavigation.IsPlanePositionInImage(point.PlaneLocation, image))
            // Conversion to pixel coordinate 
            .Select(point => new Point()
            {
                X = (int)(((point.PlaneLocation.X - position.X) / fieldSize.Width + 0.5) * imageSize.Width),
                Y = (int)(((point.PlaneLocation.Y - position.Y) / fieldSize.Height + 0.5) * imageSize.Height)
            })
            // Fit to image
            .Select(point => new Point()
            {
                X = Math.Clamp(point.X - focusSize.Width / 2, 0, imageSize.Width - focusSize.Width - 1),
                Y = Math.Clamp(point.Y - focusSize.Height / 2, 0, imageSize.Height - focusSize.Height - 1)
            })
            .ToArray();
    }

    public Size GetFocusAreaSize(Size imageSize)
    {
        var ratio = _focusConfig.CenterFocusAreaSize;
        imageSize.Width = (int)(imageSize.Width * ratio.DecimalFractions);
        imageSize.Height = (int)(imageSize.Height * ratio.DecimalFractions);
        // Rounding
        imageSize.Width -= imageSize.Width % 8;
        imageSize.Height -= imageSize.Height % 8;
        return imageSize;
    }

    public async Task TiltStageAutomaticallyAsync(ISafeStageControlling safeStageControlling, IImagingPipelineGrabber pipelineGrabber, IProgress<(string, Ratio)> progress, CancellationToken cancellationToken)
    {
        _virtualDevice.StopGrabbing();
        var position = await _virtualDevice.GetStagePositionAsync();
        var range = _limits.GetAutoTiltRangeForView(safeStageControlling.ActiveCameraView);

        var tilt = await SectionSearch.FindMaximumAsync<Angle, AngleUnit>(
            (angle, cancellationToken) => TiltAndCalculateMetric(safeStageControlling, pipelineGrabber, angle, cancellationToken),
            range, progress, _logger, cancellationToken
        );

        _ = await _virtualDevice.MoveStageAsync(position with { Tilt = tilt });
        await pipelineGrabber.GrabOneAsync();
    }

    private async Task<double> TiltAndCalculateMetric(ISafeStageControlling safeStageControlling, IImagingPipelineGrabber pipelineGrabber, Angle angle, CancellationToken cancellationToken)
    {
        var position = await _virtualDevice.GetStagePositionAsync();
        var targetPosition = position with { Tilt = angle };
        _ = await safeStageControlling.MoveStageAsync(targetPosition, cancellationToken);

        using var image = await pipelineGrabber.GrabOneWithResultCopyAsync(ImageProcessingStage.FilteredInput);
        var mat = image.Image.Mat;

        var focusArea = GetFocusAreaSize(mat.Size());

        var topLeft = mat.SubMat(0, focusArea.Height, 0, focusArea.Width);
        var bottomRight = mat.SubMat(mat.Height - focusArea.Height, mat.Height, mat.Width - focusArea.Width, mat.Width);
        return 2 / (1 / Tenengrad.CalculateIndex(topLeft) + 1 / Tenengrad.CalculateIndex(bottomRight));
    }
}
