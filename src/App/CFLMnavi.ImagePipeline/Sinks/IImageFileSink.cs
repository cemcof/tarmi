using CFLMnavi.ImagePipeline.Filters;

namespace CFLMnavi.ImagePipeline.Sinks;

public interface IImageFileSink : IImageSink
{
    void Initialize(params FilterBase[] filters);
    Task SetSourceFile(string path);
}
