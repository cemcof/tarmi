using Tarmi.Imaging.Common;
using Tarmi.ImagePipeline.Filters;

namespace Tarmi.ImagePipeline.Sinks;

public interface IImageObservableSink : IImageSink
{
    void Initialize(IObservable<ImageWithMetadata> source, params FilterBase[] filters);
    Task Set(ImageWithMetadata image);
    Task SetSourceFile(string path);
}
