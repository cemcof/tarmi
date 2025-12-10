using Tarmi.ImagePipeline.Filters;

namespace Tarmi.ImagePipeline.Sinks;

public interface IImageFileSink : IImageSink
{
    void Initialize(params FilterBase[] filters);
    Task SetSourceFile(string path);
}
