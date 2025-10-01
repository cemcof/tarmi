//using Betrian.Imaging.Algorithms.Utilities;
//using Betrian.Imaging.Common;
//using Microsoft.Extensions.Logging;

//namespace CFLMnavi.ImagePipeline.Filters;

//public sealed class GammaFilter : FilterBase
//{
//    public GammaFilter(ILogger logger) : base(logger) { }

//    public double Gamma { get; set; } = 1.0;

//    protected override void ProcessImageImplementation(ImageWithMetadata image)
//    {
//        using var originalImage = image;
//        image = image with { Image = image.Image.ApplyGamma(Gamma) };
//    }
//}

