using Betrian.Imaging.Common;
using Betrian.Imaging.Common.OpenCvWrapper;
using CommunityToolkit.Diagnostics;
using OpenCvSharp;

namespace Betrian.Imaging.Algorithms.Utilities;

public static class Overlays
{
    /// <summary>
    /// Take input images, apply LUT by lightFrequencies, apply gamma, make black color as a background, put all images in one.
    /// Than by AddWeighted merge main image and counted image.
    /// </summary>
    /// <param name="mainImage">Main image as a stage for luminescence images.</param>
    /// <param name="inputImages">Input images.</param>
    /// <param name="lightFrequencies">Array of light frequencies. Frequency range 380..780</param>
    /// <param name="positions">Image positions in main image (top left corners). Order via light frequencies array.</param>
    /// <param name="gamma">Current gamma value. Range 0..10</param>
    /// <param name="alpha">Alfa value for AddWeighted method. Main image transparency. Range 0..1</param>
    /// <param name="makeBlackFrom">Colors from value and more dark colors make black.</param>
    /// <returns>Image with highlight lightFrequencies colors merged into main image with alfa transparency.</returns>
    public static IImage ApplyLuminescenceOverlay(
        this IImage mainImage,
        ReadOnlySpan<IImage> inputImages,
        ReadOnlySpan<int> lightFrequencies,
        ReadOnlySpan<Point> positions,
        double gamma = 0.9,
        double alpha = 0.7,
        byte makeBlackFrom = 15
    )
    {
        Guard.HasSizeGreaterThan(lightFrequencies, 0);
        Guard.HasSizeGreaterThan(inputImages, 0);
        Guard.HasSizeGreaterThanOrEqualTo(positions, inputImages.Length);
        Guard.IsBetweenOrEqualTo(alpha, 0.0, 1.0);
        Guard.IsBetweenOrEqualTo(gamma, 0.0, 10.0);

        var preparedImages = new IImage[lightFrequencies.Length];
        for (int i = 0; i < lightFrequencies.Length; i++)
        {
            preparedImages[i] = PrepareLuminescenceOverlayForFrequency(inputImages, lightFrequencies[i], gamma, makeBlackFrom);
        }

        var weightedGamma = 2.9;
        var weighted = mainImage.AddWeightedImages(preparedImages, positions, alpha, weightedGamma, false);
        preparedImages.Dispose();

        return weighted;
    }

    /// <summary>
    /// Take all pictures, apply LUT by lightFrequency, apply gamma, apply black bg color and put all images in one.
    /// </summary>
    /// <param name="inputImages">Input images.</param>
    /// <param name="lightFrequency">Light frequency. Range 380..780.</param>
    /// <param name="gamma">Current gamma value. Range 0..10</param>
    /// <param name="makeBlackFrom">Color and darker staff make black.</param>
    /// <returns>Image with highlight lightFrequency color and black background.</returns>
    public static IImage PrepareLuminescenceOverlayForFrequency(ReadOnlySpan<IImage> inputImages, double lightFrequency, double gamma, byte makeBlackFrom)
    {
        Guard.HasSizeGreaterThan(inputImages, 0);

        // Get lut tables from light frequency
        var lutFilter = lightFrequency.GetLutFilterFromWavelength();
        var editedImages = new IImage[inputImages.Length];

        for (int i = 0; i < inputImages.Length; i++)
        {
            var bgrImage = Image<Bgr, byte>.ConvertFromImage(inputImages[i]);
            bgrImage.LutWithBgrFilterInplace(lutFilter);
            bgrImage.ApplyGammaInplace(gamma);
            bgrImage.ThresholdInplace(
                new() { Blue = makeBlackFrom, Green = makeBlackFrom, Red = makeBlackFrom },
                new() { Blue = 255, Green = 255, Red = 255 },
                ThresholdTypes.Binary | ThresholdTypes.Tozero
            );
            editedImages[i] = bgrImage;
        }

        if (editedImages.Length == 1)
        {
            return editedImages[0];
        }

        var commonImage = editedImages[0].CopyBlank();

        Cv2.Add(editedImages[0].InputArray, editedImages[1].InputArray, commonImage.OutputArray);

        for (int i = 2; i < editedImages.Length; i++)
        {
            Cv2.Add(commonImage.InputArray, editedImages[i].InputArray, commonImage.OutputArray);
        }
        editedImages.Dispose();

        return commonImage;
    }

    /// <summary>
    /// Add weighted images into main image.
    /// </summary>
    /// <param name="mainImage">Main image.</param>
    /// <param name="sourceImages">Images to add.</param>
    /// <param name="positions">Images positions in main.</param>
    /// <param name="alpha">Alfa value in weighted image.</param>
    /// <param name="gamma">Gamma value in weighted image.</param>
    /// <param name="transparentBlack">Default false. True - make black color in source images transparent.</param>
    /// <returns>Merged images with highlight ROI.</returns>
    /// <exception cref="ArgumentException">Apply when method has invalid arguments.</exception>
    public static IImage AddWeightedImages(
        this IImage mainImage,
        ReadOnlySpan<IImage> sourceImages,
        ReadOnlySpan<Point> positions,
        double alpha,
        double gamma,
        bool transparentBlack = false)
    {
        Guard.HasSizeGreaterThanOrEqualTo(sourceImages, 1);
        Guard.HasSizeGreaterThanOrEqualTo(positions, sourceImages.Length);
        Guard.IsBetweenOrEqualTo(alpha, 0.0, 1.0);
        Guard.IsBetweenOrEqualTo(gamma, 0.0, 10.0);

        var index = 0;
        var sourceImage = AddImageIntoBlackCanvas(
            sourceImages[0],
            positions[index++],
            new Size(mainImage.Width, mainImage.Height),
            transparentBlack);

        int positionFrom = 1;
        while (sourceImage == null && positionFrom < sourceImages.Length)
        {
            sourceImage = sourceImages[positionFrom].AddImageIntoBlackCanvas(
                positions[index++],
                new Size(mainImage.Width, mainImage.Height),
                transparentBlack);
            positionFrom++;
        }

        var resultImage = Image<Bgra, byte>.ConvertFromImage(mainImage);
        if (sourceImage == null)
        {
            return resultImage;
        }

        for (int i = positionFrom; i < sourceImages.Length; i++)
        {
            using var updatedImage = sourceImages[i].AddImageIntoBlackCanvas(
                positions[index++],
                new Size(mainImage.Width, mainImage.Height),
                transparentBlack);

            if (updatedImage == null)
            {
                continue;
            }

            Cv2.Add(sourceImage.InputArray, updatedImage.InputArray, sourceImage.OutputArray);
        }

        Cv2.AddWeighted(resultImage.InputArray, alpha, sourceImage.InputArray, 1.0 - alpha, gamma, resultImage.OutputArray);
        sourceImage.Dispose();
        return resultImage;
    }

    /// <summary>
    /// Take input images, get move vector from positions, make transparent background, put all images in one.
    /// Than by AddWeighted merge main image and counted image.
    /// </summary>
    /// <param name="mainImage">Main image.</param>
    /// <param name="inputImages">Input images.</param>
    /// <param name="positions">Image move positions in main image (move vectors according to corresponding image points).</param>
    /// <param name="gamma">Current gamma value. Range 0..10. Default value is 0.9.</param>
    /// <param name="alpha">Alfa value for AddWeighted method. Main image transparency. Range 0..1. Default value is 0.5.</param>
    /// <returns>Moved images merged into main image with alfa transparency.</returns>
    public static IImage ApplyPositioningOverlay(
        this IImage mainImage,
        ReadOnlySpan<IImage> inputImages,
        ReadOnlySpan<Point> positions,
        double gamma = 0.9,
        double alpha = 0.5)
    {
        Guard.HasSizeGreaterThan(inputImages, 0);
        Guard.HasSizeGreaterThanOrEqualTo(positions, inputImages.Length);
        Guard.IsBetweenOrEqualTo(alpha, 0.0, 1.0);
        Guard.IsBetweenOrEqualTo(gamma, 0.0, 10.0);

        //var weightedGamma = 2.9;
        Size imageSize = mainImage.Size;
        IImage? sourceImage1 = inputImages[0].AddImageIntoTransparentCanvas(
            positions[0],
            imageSize);

        int positionFrom = 1;
        while (sourceImage1 == null && positionFrom < inputImages.Length)
        {
            sourceImage1 = inputImages[positionFrom].AddImageIntoTransparentCanvas(
                positions[0],
                imageSize);
            positionFrom++;
        }

        var mainCopy = Image<Bgr, byte>.ConvertFromImage(mainImage);

        if (sourceImage1 == null)
        {
            return mainCopy;
        }

        MatType mainType = mainCopy.Mat.Type();
        using var sourceImage = sourceImage1.Convert(mainType);

        for (int i = positionFrom; i < inputImages.Length; i++)
        {
            using var updatedImage = inputImages[i]
                .AddImageIntoTransparentCanvas(
                    positions[i],
                    imageSize
                );

            if (updatedImage == null)
            {
                continue;
            }

            using var updatedHelp = updatedImage.Convert(mainType);
            Cv2.Add(sourceImage.InputArray, updatedHelp.InputArray, sourceImage.OutputArray);
        }

        using var sourceImageBgr = Image<Bgr, byte>.ConvertFromImage(sourceImage);

        //Cv2.CvtColor(sourceImage, sourceImage, ColorConversionCodes.BGRA2BGR);
        sourceImage1.Dispose();
        //var resultImage = new Mat();
        Image<Bgr, byte> resultImage = new(mainCopy.Size);
        Cv2.AddWeighted(mainCopy.InputArray, alpha, sourceImage.InputArray, 1.0 - alpha, gamma, resultImage.OutputArray);
        return resultImage;
    }

    /// <summary>
    /// Take input images, get move vector from positions, make transparent background, 
    /// put every secondary image in main with opacity.
    /// At the end put all counted images in one.
    /// </summary>
    /// <param name="mainImage">Main image.</param>
    /// <param name="inputImages">Input images.</param>
    /// <param name="positions">Image move positions in main image (move vectors according to corresponding image points).
    /// Images left top corners (measured from main image left top corner).</param>
    /// <param name="opacities">Alfa value for AddWeighted method. Image transparency. Range 0..1. Default value is 0.5.</param>
    /// <param name="gamma">Current gamma value. Range 0..10. Default value is 0.9.</param>
    /// <returns>Moved images merged into main image with defined opacity.</returns>
    public static IImage ApplyPositioningOverlayWithOpacity(
        this IImage mainImage,
        ReadOnlySpan<IImage> inputImages,
        ReadOnlySpan<Point> positions,
        ReadOnlySpan<double> opacities,
        double gamma = 0.9)
    {
        Guard.HasSizeGreaterThan(inputImages, 0);
        Guard.HasSizeGreaterThanOrEqualTo(positions, inputImages.Length);
        Guard.HasSizeGreaterThanOrEqualTo(opacities, inputImages.Length);
        Guard.IsBetweenOrEqualTo(gamma, 0.0, 10.0);

        Size imageSize = mainImage.Size;
        IImage mainCopy = Image<Bgr, byte>.ConvertFromImage(mainImage);

        //MatType mainType = mainCopy.Mat.Type();
        //var resultImage = new Mat();

        var sourceImage = inputImages[0]
            //.AddImageIntoTransparentCanvas(
            .AddImageIntoBlackCanvas(
                positions[0],
                imageSize
            );

        int positionFrom = 1;
        while (sourceImage == null && positionFrom < inputImages.Length)
        {
            sourceImage = inputImages[positionFrom]
                //.AddImageIntoTransparentCanvas(
                .AddImageIntoBlackCanvas(
                    positions[positionFrom],
                    imageSize
                );
            positionFrom++;
        }

        if (sourceImage == null)
        {
            return Image<Bgr, byte>.ConvertFromImage(mainCopy);
        }

        Image<Bgra, byte> resultImage = new(mainCopy.Size);

        using var sourceImage1 = Image<Bgr, byte>.ConvertFromImage(sourceImage);
        Cv2.AddWeighted(mainCopy.InputArray, 1.0 - opacities[positionFrom - 1], sourceImage1.InputArray, opacities[positionFrom - 1], gamma, resultImage.OutputArray);

        for (int i = positionFrom; i < inputImages.Length; i++)
        {
            using var updatedImage = inputImages[i].AddImageIntoBlackCanvas(//.AddImageIntoTransparentCanvas(
                positions[i],
                imageSize);

            if (updatedImage == null)
            {
                continue;
            }

            //using var updatedHelp = updatedImage.Convert(mainType);
            Cv2.AddWeighted(mainCopy.InputArray, 1.0 - opacities[i], updatedImage.InputArray, opacities[i], gamma, updatedImage.OutputArray);
            Cv2.AddWeighted(resultImage.Mat, 0.5, updatedImage.InputArray, 0.5, gamma, resultImage.OutputArray);
        }

        sourceImage.Dispose();
        return Image<Bgr, byte>.ConvertFromImage(resultImage);
    }

    /// <summary>
    /// Get image with max intensity composed from images in list.
    /// </summary>
    /// <param name="images">List of images taken from the same position.</param>
    /// <returns>Image with max intensity.</returns>
    /// <exception cref="ArgumentException">Throws exception when list is null or empty.</exception>
    public static ImageWithMetadata GetMaxIntensityImage(IEnumerable<ImageWithMetadata> images)
    {
        using var enumerator = images.GetEnumerator();

        if (!enumerator.MoveNext())
        {
            throw new ArgumentException($"Overlays - {nameof(GetMaxIntensityImage)} Image list is empty.");
        }

        var firstImage = enumerator.Current;
        Mat result = firstImage.Image.Mat.Clone();

        while (enumerator.MoveNext())
        {
            Cv2.Max(result, enumerator.Current.Image.Mat, result);
        }

        var resultImage = Image<Bgr, byte>.FromMat(result);
        return firstImage with { ImageId = Guid.NewGuid(), Image = resultImage };
    }

    /// <summary>
    /// Get image with max intensity composed from images.
    /// </summary>
    /// <param name="resultImage">Image with previous max intensity.</param>
    /// <param name="imageToAdd">Image to add to maximum intensity.</param>
    /// <returns>Image with max intensity.</returns>
    /// <exception cref="ArgumentException">Throws exception when list is null or empty.</exception>
    public static ImageWithMetadata GetMaxIntensityImage(ImageWithMetadata resultImage, ImageWithMetadata imageToAdd)
    {
        Cv2.Max(resultImage.Image.Mat, imageToAdd.Image.Mat, resultImage.Image.Mat);
        return resultImage;
    }

    ///// <summary>
    ///// Add image borders from black or transparent black canvas and resize it to main image size.
    ///// Source image overlays from image size are cropped.
    ///// </summary>
    ///// <param name="sourceImage">Image to change.</param>
    ///// <param name="position">Image position in main image (left top corner).</param>
    ///// <param name="imageSize">Main image size.</param>
    ///// <param name="transparentBlack">Default false. True if make transparent black included borders.</param>
    ///// <returns>Image with the same size as main image and with borders from black or black transparent.</returns>
    ///// <exception cref="ArgumentException">Apply when method has invalid arguments.</exception>
    private static IImage? AddImageIntoBlackCanvas(this IImage sourceImage, Point position, Size imageSize, bool transparentBlack = false)
    {
        using var cropImage = sourceImage
            .ResizeImageBeforeAddCanvas(position, imageSize, out int topPos, out int leftPos);

        if (cropImage == null)
        {
            return null;
        }

        using var canvas = new Mat();
        //var canvasImage = new Mat();


        //using var cropImage_padded =
        //    new Image<Gray, float>(inputImage1.Width + (inputImage2.Width / 2) + (inputImage2.Width / 2), inputImage1.Height + (inputImage2.Height / 2) + (inputImage2.Height / 2));
        //Cv2.CopyMakeBorder(
        //    cropImage.Mat, inputImage1_padded.Mat, (inputImage2.Height / 2), (inputImage2.Height / 2), (inputImage2.Width / 2), (inputImage2.Width / 2), BorderTypes.Reflect);

        Cv2.CopyMakeBorder(
            cropImage.InputArray,
            canvas,
            topPos,
            Math.Max(imageSize.Height - cropImage.Height - topPos, 0),
            leftPos,
            Math.Max(imageSize.Width - cropImage.Width - leftPos, 0),
            BorderTypes.Constant,
            new Scalar(0, 0, 0));

        if (transparentBlack)
        {
            return Image<Bgr, byte>.ConvertFromMat(canvas.SetTransparentBlack());
            //    .SetTransparentBlack();
            //canvasImage = canvas.SetTransparentBlack();
        }
        else
        {
            return Image<Bgr, byte>.ConvertFromMat(canvas);
            //Cv2.CvtColor(canvas, canvasImage, ColorConversionCodes.BGRA2BGR);
        }

        //return canvasImage;
    }

    /// <summary>
    /// Add BGR image into transparent canvas and resize it to main image size.
    /// Source image overlays from image size are cropped.
    /// </summary>
    /// <param name="sourceImage">Image to change.</param>
    /// <param name="position">Image position in main image (left top corner).</param>
    /// <param name="imageSize">Main image size.</param>
    /// <returns>BGRA image with the same size as main image and with transparent borders.</returns>
    /// <exception cref="ArgumentException">Apply when method has invalid arguments.</exception>
    private static IImage? AddImageIntoTransparentCanvas(this IImage sourceImage, Point position, Size imageSize)
    {
        var cropImage = sourceImage.ResizeImageBeforeAddCanvas(position, imageSize, out int topPos, out int leftPos);

        if (cropImage == null)
        {
            return null;
        }

        if (cropImage.NumberOfChannels < 4)
        {
            var cropBgraImage = Image<Bgra, byte>.ConvertFromImage(cropImage);
            cropImage.Dispose();
            cropImage = cropBgraImage;
        }

        var canvas = new Mat();

        Cv2.CopyMakeBorder(
            cropImage.InputArray,
            canvas,
            topPos,
            Math.Max(imageSize.Height - cropImage.Height - topPos, 0),
            leftPos,
            Math.Max(imageSize.Width - cropImage.Width - leftPos, 0),
            BorderTypes.Constant,
            new Scalar(0, 0, 0, 0)
        );

        return Image<Bgra, byte>.FromMat(canvas);
    }

    /// <summary>
    /// Resize image to main image size.
    /// Source image overlays from image size are cropped.
    /// </summary>
    /// <param name="sourceImage">Image to change.</param>
    /// <param name="position">Image position in main image (left top corner).</param>
    /// <param name="imageSize">Main image size.</param>
    /// <param name="topPos">Output top position of the image in main image.</param>
    /// <param name="leftPos">Output left position of the image in main image.</param>
    /// <returns>Image with the same size as main image.</returns>
    private static IImage? ResizeImageBeforeAddCanvas(this IImage sourceImage, Point position, Size imageSize, out int topPos, out int leftPos)
    {
        Guard.IsGreaterThan(imageSize.Width, 0);
        Guard.IsGreaterThan(imageSize.Height, 0);

        topPos = position.Y;
        leftPos = position.X;

        if ((Math.Abs(position.X) >= imageSize.Width || Math.Abs(position.Y) >= imageSize.Height) &&
            position.X <= -sourceImage.Width || position.Y <= -sourceImage.Height)
        {
            return null;
        }

        int xStartPos = 0;
        int yStartPos = 0;
        int xEndPos = sourceImage.Width;
        int yEndPos = sourceImage.Height;

        if (position.X + sourceImage.Width > imageSize.Width)
        {
            xEndPos = sourceImage.Width - (position.X + sourceImage.Width - imageSize.Width);
        }

        if (position.X < 0)
        {
            xStartPos = -position.X;
            leftPos = 0;
        }

        if (position.Y + sourceImage.Height > imageSize.Height)
        {
            yEndPos = sourceImage.Height - (position.Y + sourceImage.Height - imageSize.Height);
        }

        if (position.Y < 0)
        {
            yStartPos = -position.Y;
            topPos = 0;
        }

        return sourceImage.GetSubRect(new Rect(xStartPos, yStartPos, xEndPos - xStartPos, yEndPos - yStartPos)).Clone();
    }

    //private static Mat ChangeMatType(this Mat inputImage, MatType targetType)
    //{
    //    Mat outputImage = new Mat(inputImage.Size(), targetType);
    //    var inputType = inputImage.Type();

    //    var targetTypeIs16 = targetType.Equals(MatType.CV_16U) || targetType.Equals(MatType.CV_16UC3) || targetType.Equals(MatType.CV_16UC4);

    //    if (inputType.Equals(MatType.CV_16U) || inputType.Equals(MatType.CV_16UC3) || inputType.Equals(MatType.CV_16UC4))
    //    {
    //        if (targetTypeIs16)
    //        {
    //            inputImage.CopyTo(outputImage);
    //        }
    //        else
    //        {
    //            inputImage.ConvertTo(outputImage, targetType, 1.0 / 256.0);
    //        }
    //    }
    //    else if (inputType.Equals(MatType.CV_8U) || inputType.Equals(MatType.CV_8UC3) || inputType.Equals(MatType.CV_8UC4))
    //    {
    //        if (targetTypeIs16)
    //        {
    //            inputImage.ConvertTo(outputImage, targetType, 256.0);
    //        }
    //        else
    //        {
    //            inputImage.CopyTo(outputImage);
    //        }
    //    }
    //    else
    //    {
    //        inputImage.CopyTo(outputImage);
    //    }

    //    return outputImage;
    //}
}
