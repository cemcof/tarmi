using System.Text.Json;
using System.Text.Json.Nodes;
using Betrian.App.Infrastructure.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Betrian.App.Infrastructure.Options;

internal class JsonWritableOptions<T> : IWritableOptions<T>
    where T : class, new()
{
    private readonly ReaderWriterLockSlim _lock;
    private readonly IOptions<T> _options;
    private readonly IOptionsMonitor<T> _optionsMonitor;
    private readonly IConfigurationRoot _configuration;
    private readonly string _sectionName;
    private readonly string _filePath;

    public JsonWritableOptions(
        IFsLocksDictionary fsLocks,
        IOptions<T> options,
        IOptionsMonitor<T> optionsMonitor,
        IConfigurationRoot configuration,
        string section,
        string filePath
    )
    {
        _options = options;
        _optionsMonitor = optionsMonitor;
        _configuration = configuration;
        _sectionName = section;
        _filePath = filePath;
        _lock = fsLocks.GetLockForPath(filePath);
    }

    public T Value => _options.Value;

    public T CurrentValue => _optionsMonitor.CurrentValue;

    public T Get(string? name) => _optionsMonitor.Get(name);

    private JsonNode ReadFileContent()
    {
        using var fs = File.Open(_filePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
        return JsonNode.Parse(fs) ?? new JsonObject();
    }

    private void WriteFileContent(JsonNode jObject)
    {
        using var fs = File.Open(_filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var utf8jw = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true });
        jObject.WriteTo(utf8jw);
    }

    public void Update(Action<T> applyChanges)
    {
        using var guard = _lock.TakeWriterLock();
        var jObject = ReadFileContent();
        var jSectionObject = jObject[_sectionName];
        var sectionObject = jSectionObject is not null ? jSectionObject.Deserialize<T>() : new T();

        applyChanges(sectionObject!);

        jObject[_sectionName] = JsonNode.Parse(JsonSerializer.Serialize(sectionObject));
        WriteFileContent(jObject);

        _configuration.Reload();
    }

    public IDisposable? OnChange(Action<T, string?> listener)
    {
        return _optionsMonitor.OnChange(listener);
    }
}
