using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Tarmi.Imaging.Common;
using Tarmi.ImagePipeline.Filters;

namespace Tarmi.ImagePipeline.Sinks;

public class FilteredFileSink : IImageFileSink
{
    private readonly CompositeDisposable _disposables = [];
    private readonly BehaviorSubject<ImageWithMetadata> _inputSubject = new(ImageWithMetadata.Empty);
    private readonly BehaviorSubject<ImageWithMetadata> _outputSubject = new(ImageWithMetadata.Empty);
    private readonly List<FilterBase> _filters = [];
    // guard for correct memory management
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public IObservable<ImageWithMetadata> Input => _inputSubject.AsObservable();
    public IObservable<ImageWithMetadata> Output => _outputSubject.AsObservable();
    public IEnumerable<FilterBase> Filters => _filters;

    public void Initialize(params FilterBase[] filters)
    {
        _filters.AddRange(filters);

        if (filters.Length == 0)
        {
            _disposables.Add(_inputSubject.Subscribe(_outputSubject));
            return;
        }

        IObservable<ImageWithMetadata> currentInput = _inputSubject;

        foreach (var filter in filters)
        {
            filter.SetSource(currentInput);
            currentInput = filter.Output;
        }

        _disposables.Add(currentInput.Subscribe(_outputSubject));
    }

    public async Task Clear()
    {
        using var guard = await _semaphore.UseOnceAsync();
        using var oldImage = _inputSubject.Value;
        _inputSubject.OnNext(ImageWithMetadata.Empty);
    }

    public async Task Invalidate()
    {
        using var guard = await _semaphore.UseOnceAsync();
        _inputSubject.OnNext(_inputSubject.Value);
    }

    public async Task SetSourceFile(string path)
    {
        using var guard = await _semaphore.UseOnceAsync();
        var image = TiffImage.Load(path);
        using var oldImage = _inputSubject.Value;
        _inputSubject.OnNext(image);
    }

    public async Task<ImageWithMetadata> GetInputCopyAsync()
    {
        using var guard = await _semaphore.UseOnceAsync();
        var image = _inputSubject.Value;
        return image with { Image = image.Image.Clone() };
    }

    public async Task<ImageWithMetadata> GetOutputCopyAsync()
    {
        using var guard = await _semaphore.UseOnceAsync();

        var imageTask = _outputSubject.Take(1).ToTask();
        await Task.Run(() => _inputSubject.OnNext(_inputSubject.Value));

        var image = await imageTask;
        return image with { Image = image.Image.Clone() };
    }

    public void Dispose()
    {
        _disposables.Dispose();
        _inputSubject.Value.Dispose();
        _inputSubject.Dispose();
        _outputSubject.Value.Dispose();
        _outputSubject.Dispose();
        foreach (var filter in _filters)
        {
            filter.Dispose();
        }
        _semaphore.Dispose();
        GC.SuppressFinalize(this);
    }
}
