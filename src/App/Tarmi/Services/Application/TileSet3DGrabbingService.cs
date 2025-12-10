using System.Reactive.Linq;
using System.Reactive.Subjects;
using Tarmi.Imaging.Algorithms.Tileset;
using Tarmi.Imaging.Common;
using Tarmi.Models;
using Tarmi.Configuration.Holders;
using Tarmi.Projects;
using Tarmi.Projects.Implementation;
using Tarmi.Projects.Transactions;
using Tarmi.VirtualDevices;
using Tarmi.VirtualDevices.Implementation;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace Tarmi.App.Services.Application;
public class TileSet3DGrabbingService
{
    private readonly ILogger _logger;
    private readonly IVirtualDevice _virtualDevice;
    private readonly IZStackGrabbingMode _zStackGrabbingMode;
    private readonly BehaviorSubject<bool> _tileSetGrabbingRunning = new(false);

    public OpenCvSharp.Scalar Background { get; set; } = OpenCvSharp.Scalar.Black;
    public IObservable<bool> TileSetGrabbingRunning => _tileSetGrabbingRunning.AsObservable();

    public TileSet3DGrabbingService(IZStackGrabbingMode zStackGrabbingMode, ILogger logger, IVirtualDevice virtualDevice)
    {
        _logger = logger;
        _virtualDevice = virtualDevice;
        _zStackGrabbingMode = zStackGrabbingMode;
    }

    public async Task GrabTileSet3DAsync(
        ObservableProject project, IStageNavigation stageNavigation, ISafeStageControlling safeStageControlling, IImagingPipelineGrabber pipelineGrabber,
        RegionOfInterest roi, TileSetOptions tilesetOption, ZStackOptions zStackOptions, Guid? linkId, IProgress<(string, Ratio)> progress, ILogger logger, CancellationToken cancellationToken)
    {
        using var tileSetLayerTransaction = new TileSet3DCreationTransaction(project, safeStageControlling.ActiveCameraView, stageNavigation.GetPlanePosition, roi, tilesetOption, zStackOptions, linkId);
        await GrabTileSet3DAsyncImplementation(stageNavigation, safeStageControlling, pipelineGrabber, tileSetLayerTransaction, tilesetOption, zStackOptions, progress, logger, cancellationToken);
    }

    private async Task GrabTileSet3DAsyncImplementation(
        IStageNavigation stageNavigation, ISafeStageControlling safeStageControlling, IImagingPipelineGrabber pipelineGrabber,
        TileSet3DCreationTransaction tileSetLayerTransaction, TileSetOptions tilesetOptions, ZStackOptions zStackOptions,
        IProgress<(string, Ratio)> progress, ILogger logger, CancellationToken cancellationToken
    )
    {
        _tileSetGrabbingRunning.OnNext(true);
        using var tileSetLayerTransactionCancellation = cancellationToken.Register(tileSetLayerTransaction.Cancel);
        
        try
        {
            _virtualDevice.StopGrabbing();

            await GrabTileSet3DAsync(stageNavigation, safeStageControlling.ActiveCameraView, pipelineGrabber, tilesetOptions, zStackOptions, tileSetLayerTransaction, progress, logger, cancellationToken);
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
        IStageNavigation stageNavigation, StageCameraView stageCameraView, IImagingPipelineGrabber pipelineGrabber,
        TileSetOptions tilesetOptions, ZStackOptions zStackOptions, TileSet3DCreationTransaction tileSet3DTransaction, IProgress<(string, Ratio)> progress,
        ILogger logger, CancellationToken cancellationToken
    )
    {
        var initialPosition = await _virtualDevice.GetStagePositionAsync();
        LengthPoint[] coordinates = tilesetOptions.AcquisitionStrategy switch
        {
            AcquisitionStrategy.Linear => [.. GetLinearCoordinates(tilesetOptions.AreaOfInterest, tilesetOptions.Overlap)],
            AcquisitionStrategy.Spiral => [.. GetSpiralCoordinates(tilesetOptions.AreaOfInterest, tilesetOptions.Overlap)],
            _ => throw new NotImplementedException()
        };

        logger.LogInformation("Starting TileSet3D acquisition with {TileSetOptions} strategy and Z-Stack settings {Settings}.", tilesetOptions, zStackOptions);
        logger.LogInformation("Acquiring {TileCount} tiles.", coordinates.Length);
        
        foreach (var point in coordinates)
        {
            logger.LogInformation("Selected tile at {TilePosition}.", point);
        }

        Length initialLinearStagePosition = _zStackGrabbingMode.CurrentLinearStagePosition;

        await _zStackGrabbingMode.MoveLinearStageToAsync(zStackOptions.StartPosition, cancellationToken);

        for (int imageIndex = 0; imageIndex < coordinates.Length; ++imageIndex)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var point = coordinates[imageIndex];
            double currentProgress = (double)imageIndex / coordinates.Length;
            progress.Report(($"Acquiring TileSet3D Images {(imageIndex + 1) * zStackOptions.NumberOfSteps}/{coordinates.Length * zStackOptions.NumberOfSteps}", Ratio.FromDecimalFractions(currentProgress)));

            var stagePosition = stageNavigation.GetStagePosition(point, stageCameraView);
            var successfulMove = await _virtualDevice.MoveStageAsync(stagePosition);

            if (!successfulMove)
            {
                logger.LogWarning("Failed to move stage to the desired position.");
            }

            var innerProgress = new Progress<(string Message, Ratio Percentage)>(inner => progress.Report((inner.Message, Ratio.FromDecimalFractions(currentProgress) + (inner.Percentage / (coordinates.Length * zStackOptions.NumberOfSteps)))));
            await GrabZStackAsync(stageCameraView, pipelineGrabber, tileSet3DTransaction, zStackOptions, innerProgress, cancellationToken);
        }

        await _zStackGrabbingMode.MoveLinearStageToAsync(initialLinearStagePosition, cancellationToken);
        _ = await _virtualDevice.MoveStageAsync(initialPosition);
    }

    private async Task GrabZStackAsync(
        StageCameraView stageCameraView, IImagingPipelineGrabber pipelineGrabber,
        TileSet3DCreationTransaction tileSet3DTransaction,
        ZStackOptions settings, IProgress<(string, Ratio)> progress, CancellationToken cancellationToken
    )
    {
        using var tileSetLayerTransactionCancellation = cancellationToken.Register(tileSet3DTransaction.Cancel);
        try
        {
            _virtualDevice.StopGrabbing();

            using var stackTransaction = tileSet3DTransaction.CreateZStackTransaction();
            using var ctsRegistration = cancellationToken.Register(stackTransaction.Cancel);
            await _zStackGrabbingMode.GrabZStackAsync(settings, stageCameraView, pipelineGrabber, stackTransaction.AddImage, stackTransaction.AddMipImage, progress, cancellationToken);
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
            if (yStep + 1 == yStepCount)
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
