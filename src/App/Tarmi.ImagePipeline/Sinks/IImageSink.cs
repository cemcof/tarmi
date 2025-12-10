using Tarmi.Imaging.Common;
using Tarmi.ImagePipeline.Filters;

namespace Tarmi.ImagePipeline.Sinks;

public interface IImageSink : IDisposable
{
    IObservable<ImageWithMetadata> Input { get; }
    IObservable<ImageWithMetadata> Output { get; }
    IEnumerable<FilterBase> Filters { get; }
    Task Clear();
    Task Invalidate();
    Task<ImageWithMetadata> GetInputCopyAsync();
    Task<ImageWithMetadata> GetOutputCopyAsync();
}
