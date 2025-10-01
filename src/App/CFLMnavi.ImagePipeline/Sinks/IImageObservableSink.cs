using Betrian.Imaging.Common;
using CFLMnavi.ImagePipeline.Filters;

namespace CFLMnavi.ImagePipeline.Sinks;

public interface IImageObservableSink : IImageSink
{
    void Initialize(IObservable<ImageWithMetadata> source, params FilterBase[] filters);
    Task Set(ImageWithMetadata image);
    Task SetSourceFile(string path);
}
