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
public class TileSet3DGrabbingService
{
    private readonly ILogger _logger;
    private readonly IVirtualDevice _virtualDevice;
    private readonly ILuminescenceMode _luminescenceMode;
    private readonly BehaviorSubject<bool> _tileSetGrabbingRunning = new(false);

    public OpenCvSharp.Scalar Background { get; set; } = OpenCvSharp.Scalar.Black;
    public IObservable<bool> TileSetGrabbingRunning => _tileSetGrabbingRunning.AsObservable();

    public TileSet3DGrabbingService(ILuminescenceMode luminescenceMode, ILogger logger, IVirtualDevice virtualDevice)
    {
        _logger = logger;
        _virtualDevice = virtualDevice;
        _luminescenceMode = luminescenceMode;
    }

    public async Task GrabTileSet3DAsync(
        ObservableProject project, IStageNavigation stageNavigation, ISafeStageControlling safeStageControlling, IImagingPipelineGrabber pipelineGrabber,
        RegionOfInterest roi, TileSetOptions options, ZStackSettings settings, Guid? linkId, IProgress<(string, Ratio)> progress, ILogger logger, CancellationToken cancellationToken)
    {
        using var tileSetLayerTransaction = new TileSet3DCreationTransaction(project, safeStageControlling.ActiveCameraView, stageNavigation.GetPlanePosition, roi, options, linkId);
        await GrabTileSet3DAsyncImplementation(project, stageNavigation, safeStageControlling, pipelineGrabber, tileSetLayerTransaction, options, settings, progress, logger, cancellationToken);
    }

    private async Task GrabTileSet3DAsyncImplementation(ObservableProject project, IStageNavigation stageNavigation, ISafeStageControlling safeStageControlling, IImagingPipelineGrabber pipelineGrabber,
        TileSet3DCreationTransaction tileSetLayerTransaction, TileSetOptions options, ZStackSettings settings, IProgress<(string, Ratio)> progress, ILogger logger, CancellationToken cancellationToken)
    {
        _tileSetGrabbingRunning.OnNext(true);
        using var tileSetLayerTransactionCancellation = cancellationToken.Register(tileSetLayerTransaction.Cancel);
        
        try
        {
            _virtualDevice.StopGrabbing();
            await GrabTileSet3DAsync(
                project, stageNavigation, safeStageControlling.ActiveCameraView, pipelineGrabber,
                settings, options, tileSetLayerTransaction, progress, logger, cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TileSet3D grabbing failed.");
        }
        finally
        {
            _tileSetGrabbingRunning.OnNext(false);
        }
        
        await ProcessTileSetMipImages(tileSetLayerTransaction, stageNavigation.GetPlanePosition, progress, default);
    }

    private async Task GrabTileSet3DAsync(
        ObservableProject project, IStageNavigation stageNavigation, StageCameraView stageCameraView, IImagingPipelineGrabber pipelineGrabber,
        ZStackSettings settings, TileSetOptions options, TileSet3DCreationTransaction tileSet3DTransaction, IProgress<(string, Ratio)> progress,
        ILogger logger, CancellationToken cancellationToken
    )
    {
        var initialPosition = await _virtualDevice.GetStagePositionAsync();
        LengthPoint[] coordinates = options.AcquisitionStrategy switch
        {
            AcquisitionStrategy.Linear => [.. GetLinearCoordinates(options.AreaOfInterest, options.Overlap)],
            AcquisitionStrategy.Spiral => [.. GetSpiralCoordinates(options.AreaOfInterest, options.Overlap)],
            _ => throw new NotImplementedException()
        };

        logger.LogInformation("Starting TileSet3D acquisition with {TileSetOptions} strategy and Z-Stack settings {Settings}.", options, settings);
        logger.LogInformation("Acquiring {TileCount} tiles.", coordinates.Length);
        
        foreach (var point in coordinates)
        {
            logger.LogInformation("Selected tile at {TilePosition}.", point);
        }

        Length initialLinearStagePosition = _luminescenceMode.CurrentLinearStagePosition;

        // prepare linear stage position
        await _luminescenceMode.MoveLinearStageToAsync(settings.StartPosition, cancellationToken);

        for (int imageIndex = 0; imageIndex < coordinates.Length; ++imageIndex)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var point = coordinates[imageIndex];
            double currentProgress = (double)imageIndex / (double)coordinates.Length;
            progress.Report(($"Acquiring TileSet3D Images {(imageIndex + 1) * settings.NumberOfSteps}/{coordinates.Length * settings.NumberOfSteps}", Ratio.FromDecimalFractions(currentProgress)));

            var stagePosition = stageNavigation.GetStagePosition(point, stageCameraView);
            var successfulMove = await _virtualDevice.MoveStageAsync(stagePosition);

            if (!successfulMove)
            {
                logger.LogWarning("Failed to move stage to the desired position.");
            }

            var innerProgress = new Progress<(string Message, Ratio Percentage)>(inner => progress.Report((inner.Message, Ratio.FromDecimalFractions(currentProgress) + (inner.Percentage / (coordinates.Length * settings.NumberOfSteps)))));
            await GrabZStackAsync(project, stageNavigation, stageCameraView, pipelineGrabber, tileSet3DTransaction, settings, innerProgress, cancellationToken);
        }

        // move stage back to the initial position, and acquire image on the initial position
        await _luminescenceMode.MoveLinearStageToAsync(initialLinearStagePosition, cancellationToken);
        _ = await _virtualDevice.MoveStageAsync(initialPosition);
        //await pipelineGrabber.GrabOneAsync();
    }

    public async Task GrabZStackAsync(
        ObservableProject project, IStageNavigation stageNavigation, StageCameraView stageCameraView, IImagingPipelineGrabber pipelineGrabber,
        TileSet3DCreationTransaction tileSet3DTransaction,
        ZStackSettings settings, IProgress<(string, Ratio)> progress, CancellationToken cancellationToken
    )
    {
        using var tileSetLayerTransactionCancellation = cancellationToken.Register(tileSet3DTransaction.Cancel);
        try
        {
            _luminescenceMode.StopGrabbing();

            using var stackTransaction = tileSet3DTransaction.CreateZStackTransaction();
            await _luminescenceMode.GrabZStackAsync(settings, stageCameraView, pipelineGrabber, stackTransaction.AddImage, stackTransaction.AddMipImage, progress, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TileSet3D grabbing failed.");
        }
    }

    public async Task ProcessTileSetMipImages(TileSet3DCreationTransaction transaction, Func<StagePosition, StageCameraView, LengthPoint> getPlanePosition, IProgress<(string, Ratio)> progress, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(() => ProcessTileSetMipImagesImplementation(transaction, getPlanePosition, progress), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TileSet3D stitching failed.");
        }
    }

    private void ProcessTileSetMipImagesImplementation(
        TileSet3DCreationTransaction transaction, Func<StagePosition, StageCameraView, LengthPoint> getPlanePosition, IProgress<(string, Ratio)> progress)
    {
        progress.Report(("Stitching tileSet3D mip image.", Ratio.FromPercent(100)));
        IEnumerable<string> imagePaths = transaction.GetMipFilePaths();

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
