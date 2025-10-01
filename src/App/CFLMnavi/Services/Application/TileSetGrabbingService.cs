using System.Reactive.Linq;
using System.Reactive.Subjects;
using Betrian.Imaging.Algorithms.Tileset;
using Betrian.Imaging.Common;
using Betrian.Models;
using CFLMnavi.Configuration.Holders;
using CFLMnavi.Projects;
using CFLMnavi.Projects.Implementation;
using CFLMnavi.Projects.Transactions;
using CFLMnavi.VirtualDevices;
using CFLMnavi.VirtualDevices.Implementation;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace Betrian.CflmNavi.App.Services.Application;

public class TileSetGrabbingService
{
    private readonly ILogger _logger;
    private readonly IVirtualDevice _virtualDevice;
    private readonly BehaviorSubject<bool> _tileSetGrabbingRunning = new(false);
    private readonly FocusingService _focusingService;

    public OpenCvSharp.Scalar Background { get; set; } = OpenCvSharp.Scalar.Black;
    public IObservable<bool> TileSetGrabbingRunning => _tileSetGrabbingRunning.AsObservable();

    public TileSetGrabbingService(ILogger logger, IVirtualDevice virtualDevice, FocusingService focusingService)
    {
        _logger = logger;
        _virtualDevice = virtualDevice;
        _focusingService = focusingService;
    }

    // TODO: refactor
    public async Task ReacquireTileSet(
        ObservableProject project, IStageNavigation stageNavigation, ISafeStageControlling safeStageControlling, IImagingPipelineGrabber pipelineGrabber,
        TileSetDescriptor descriptor, IProgress<(string, Ratio)> progress, ILogger logger, CancellationToken cancellationToken)
    {
        using var tileSetLayerTransaction = new TileSetCreationTransaction(project, safeStageControlling.ActiveCameraView, stageNavigation.GetPlanePosition, descriptor);
        await GrabTileSetAsyncImplementation(stageNavigation, safeStageControlling, pipelineGrabber, tileSetLayerTransaction, descriptor.GrabbingOptions, progress, logger, cancellationToken);
    }

    public async Task GrabTileSetAsync(
        ObservableProject project, IStageNavigation stageNavigation, ISafeStageControlling safeStageControlling, IImagingPipelineGrabber pipelineGrabber,
        RegionOfInterest roi, TileSetOptions options, IProgress<(string, Ratio)> progress, ILogger logger, CancellationToken cancellationToken)
    {
        using var tileSetLayerTransaction = new TileSetCreationTransaction(project, safeStageControlling.ActiveCameraView, stageNavigation.GetPlanePosition, roi, options);
        await GrabTileSetAsyncImplementation(stageNavigation, safeStageControlling, pipelineGrabber, tileSetLayerTransaction, options, progress, logger, cancellationToken);
    }

    private async Task GrabTileSetAsyncImplementation(IStageNavigation stageNavigation, ISafeStageControlling safeStageControlling, IImagingPipelineGrabber pipelineGrabber,
        TileSetCreationTransaction tileSetLayerTransaction, TileSetOptions options, IProgress<(string, Ratio)> progress, ILogger logger, CancellationToken cancellationToken)
    {
        _tileSetGrabbingRunning.OnNext(true);
        using var tileSetLayerTransactionCancellation = cancellationToken.Register(tileSetLayerTransaction.Cancel);
        try
        {
            _virtualDevice.StopGrabbing();
            await GrabTileSetAsync(
                stageNavigation, safeStageControlling, safeStageControlling.ActiveCameraView, pipelineGrabber,
                tileSetLayerTransaction.AddImage, options, progress, logger, cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tileset grabbing failed.");
        }
        finally
        {
            _tileSetGrabbingRunning.OnNext(false);
        }
        await ProcessTileSetImages(tileSetLayerTransaction, stageNavigation.GetPlanePosition, progress, default);
    }

    public async Task ProcessTileSetImages(TileSetCreationTransaction transaction, Func<StagePosition, StageCameraView, LengthPoint> getPlanePosition, IProgress<(string, Ratio)> progress, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(() => ProcessTileSetImagesImplementation(transaction, getPlanePosition, progress), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tileset stitching failed.");
        }
    }

    private void ProcessTileSetImagesImplementation(
        TileSetCreationTransaction transaction, Func<StagePosition, StageCameraView, LengthPoint> getPlanePosition, IProgress<(string, Ratio)> progress)
    {
        progress.Report(("Stitching tileset image.", Ratio.FromPercent(100)));
        IEnumerable<string> imagePaths = transaction.GetFilePaths();
        if (imagePaths.Any()) 
        {
            using ImageWithMetadata stitchedImage = Stitcher.StitchImage(imagePaths, Background, getPlanePosition);
            transaction.AddStitchedImage(stitchedImage);

            using var thumbnail = CreateThumbnail(stitchedImage);
            transaction.AddStitchedImageThumbnail(thumbnail);
        }
    }

    private static ImageWithMetadata CreateThumbnail(ImageWithMetadata stitchedImage)
    {
        const double MaxSize = 512.0;
        var scale = MaxSize / stitchedImage.Image.Width;
        var thumbnail = stitchedImage.Image.Resize(scale, interpolation: OpenCvSharp.InterpolationFlags.Area);

        return stitchedImage with
        {
            Image = thumbnail,
            Coordinates = stitchedImage.Coordinates with
            {
                ImageSize = new()
                {
                    Width = thumbnail.Width,
                    Height = thumbnail.Height
                },
                PixelSize = new PixelSize()
                {
                    X = stitchedImage.Coordinates.PixelSize.X / scale,
                    Y = stitchedImage.Coordinates.PixelSize.Y / scale
                }
            }
        };
    }

    private async Task GrabTileSetAsync(
        IStageNavigation stageNavigation, ISafeStageControlling safeStageControlling, StageCameraView stageCameraView, IImagingPipelineGrabber pipelineGrabber, Action<ImageWithMetadata, int> saveAction, TileSetOptions options, IProgress<(string, Ratio)> progress, ILogger logger, CancellationToken cancellationToken
    )
    {
        var initialPosition = await _virtualDevice.GetStagePositionAsync();
        LengthPoint[] coordinates = options.AcquisitionStrategy switch
        {
            AcquisitionStrategy.Linear => [.. GetLinearCoordinates(options.AreaOfInterest, options.Overlap)],
            AcquisitionStrategy.Spiral => [.. GetSpiralCoordinates(options.AreaOfInterest, options.Overlap)],
            _ => throw new NotImplementedException()
        };

        logger.LogInformation("Starting tileset acquisition with {TileSetOptions} strategy.", options);
        logger.LogInformation("Acquiring {TileCount} tiles.", coordinates.Length);
        foreach (var point in coordinates)
        {
            logger.LogInformation("Selected tile at {TilePosition}.", point);
        }

        for (int imageIndex = 0; imageIndex < coordinates.Length; ++imageIndex)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var point = coordinates[imageIndex];
            progress.Report(($"Acquiring Tileset Image {imageIndex + 1}/{coordinates.Length}", Ratio.FromDecimalFractions((double)imageIndex / coordinates.Length)));

            var stagePosition = stageNavigation.GetStagePosition(point, stageCameraView);
            var successfulMove = await _virtualDevice.MoveStageAsync(stagePosition);

            if (!successfulMove)
            {
                logger.LogWarning("Failed to move stage to the desired position.");
            }

            if (options.FocusStrategy == FocusStrategy.Auto)
            {
                await _focusingService.FocusAutomaticallyAsync(stageNavigation, safeStageControlling, pipelineGrabber, options.FocusPoints, new ProgressMock(), cancellationToken);
            }

            using var image = await pipelineGrabber.GrabOneWithResultCopyAsync(ImageProcessingStage.FilteredInput);
            saveAction(image, imageIndex);
        }

        // move stage back to the initial position, and acquire image on the initial position
        _ = await _virtualDevice.MoveStageAsync(initialPosition);
        await pipelineGrabber.GrabOneAsync();
    }

    private IEnumerable<LengthPoint> GetLinearCoordinates(AreaOfInterest areaOfInterest, Ratio overlap)
    {
        var xOffset = (1 - overlap.DecimalFractions) * _virtualDevice.HorizontalFieldWidth;
        var yOffset = (1 - overlap.DecimalFractions) * _virtualDevice.VerticalFieldWidth;

        var centerXOffset = xOffset / 2;
        var centerYOffset = yOffset / 2;

        var limits = areaOfInterest.BoundingRectangle;
        
        var xStepCount = (int)double.Ceiling(limits.Width / xOffset);
        var yStepCount = (int)double.Ceiling(limits.Height / yOffset);

        var xOverstep = (xStepCount * xOffset) - limits.Width;
        var yOverstep = (yStepCount * yOffset) - limits.Height;
        limits = new()
        {
            Top = limits.Top - yOverstep / 2,
            Left = limits.Left - xOverstep / 2,
            Bottom = limits.Bottom + yOverstep / 2,
            Right = limits.Right + xOverstep / 2,
        };

        var top = limits.Bottom - yOffset;
        Length left;

        for (int yStep = 0; yStep < yStepCount; yStep += 2)
        {
            left = limits.Left;
            for (int xStep = 0; xStep < xStepCount; xStep++)
            {
                if (ContainsSampleArea(areaOfInterest, top, left))
                {
                    yield return new()
                    {
                        X = left + centerXOffset,
                        Y = top + centerYOffset
                    };
                }
                left += xOffset;
            }
            top -= yOffset;
            if (yStep + 1 == xStepCount)
            {
                continue;
            }
            for (int xStep = 0; xStep < xStepCount; xStep++)
            {
                left -= xOffset;
                if (ContainsSampleArea(areaOfInterest, top, left))
                {
                    yield return new()
                    {
                        X = left + centerXOffset,
                        Y = top + centerYOffset
                    };
                }
            }
            top -= yOffset;
        }
    }

    private IEnumerable<LengthPoint> GetSpiralCoordinates(AreaOfInterest areaOfInterest, Ratio overlap)
    {
        var xOffset = (1 - overlap.DecimalFractions) * _virtualDevice.HorizontalFieldWidth;
        var yOffset = (1 - overlap.DecimalFractions) * _virtualDevice.VerticalFieldWidth;

        var centerXOffset = xOffset / 2;
        var centerYOffset = yOffset / 2;

        var limits = areaOfInterest.BoundingRectangle;

        var top = (limits.Top + limits.Bottom - yOffset) / 2;
        var left = (limits.Left + limits.Right - xOffset) / 2;

        // Middle area
        if (ContainsSampleArea(areaOfInterest, top, left))
        {
            yield return new()
            {
                X = left + centerXOffset,
                Y = top + centerYOffset
            };
        }
        var offset = 1;
        top += yOffset;

        bool gridOverlapped = true;
        while (gridOverlapped)
        {
            gridOverlapped = false;
            for (int column = 0; column < 2 * offset; column++)
            {
                if (ContainsSampleArea(areaOfInterest, top, left))
                {
                    yield return new()
                    {
                        X = left + centerXOffset,
                        Y = top + centerYOffset
                    };
                    gridOverlapped = true;
                }
                left += xOffset;
            }
            left -= xOffset;
            top -= yOffset;
            for (int row = 0; row < 2 * offset; row++)
            {
                if (ContainsSampleArea(areaOfInterest, top, left))
                {
                    yield return new()
                    {
                        X = left + centerXOffset,
                        Y = top + centerYOffset
                    };
                    gridOverlapped = true;
                }
                top -= yOffset;
            }
            top += yOffset;
            left -= xOffset;
            for (int column = 0; column < 2 * offset; column++)
            {
                if (ContainsSampleArea(areaOfInterest, top, left))
                {
                    yield return new()
                    {
                        X = left + centerXOffset,
                        Y = top + centerYOffset
                    };
                    gridOverlapped = true;
                }
                left -= xOffset;
            }
            left += xOffset;
            top += yOffset;
            for (int row = 0; row < 2 * offset; row++)
            {
                if (ContainsSampleArea(areaOfInterest, top, left))
                {
                    yield return new()
                    {
                        X = left + centerXOffset,
                        Y = top + centerYOffset
                    };
                    gridOverlapped = true;
                }
                top += yOffset;
            }
            offset++;
        }
    }

    private bool ContainsSampleArea(AreaOfInterest areaOfInterest, Length top, Length left)
    {
        var imageArea = new LengthRectangle()
        {
            Top = top,
            Left = left,
            Right = left + _virtualDevice.HorizontalFieldWidth,
            Bottom = top + _virtualDevice.VerticalFieldWidth,
        };
        return areaOfInterest.Overlaps(imageArea);
    }
}
