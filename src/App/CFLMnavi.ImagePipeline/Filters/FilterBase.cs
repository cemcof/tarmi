using System.Reactive.Linq;
using System.Reactive.Subjects;
using Betrian.App.Infrastructure;
using Betrian.Imaging.Common;
using Microsoft.Extensions.Logging;

namespace CFLMnavi.ImagePipeline.Filters;

public abstract class FilterBase : IDisposable
{
    protected readonly ILogger _logger;
    protected readonly Subject<ImageWithMetadata> _output = new();
    protected IDisposable? _inputSubscription;
    private bool _firstSource = false;

    protected FilterBase(ILogger logger)
    {
        _logger = logger;
        Output = _output.AsObservable();
    }

    public IObservable<ImageWithMetadata> Output { get; }


    public void SetSource(IObservable<ImageWithMetadata> source, bool firstSource = true)
    {
        _inputSubscription?.Dispose();
        _firstSource = firstSource;
        _inputSubscription = source.Subscribe(ProcessImage);
    }

    private void ProcessImage(ImageWithMetadata image)
    {
        using var activity = AppTelemetry.ImagePipelineActivitySource.StartActivity($"{GetType().Name}::{nameof(ProcessImage)}");

        try
        {
            if (_firstSource)
            {
                image = image with { Image = image.Image.Clone() };
            }

            ProcessImageImplementation(image);
            _output.OnNext(image);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image");
        }
    }

    protected abstract void ProcessImageImplementation(ImageWithMetadata image);

    public virtual void Dispose()
    {
        _inputSubscription?.Dispose();
        _output.OnCompleted();
        _output.Dispose();
        GC.SuppressFinalize(this);
    }
}
