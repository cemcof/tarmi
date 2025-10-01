using Microsoft.Extensions.Options;

namespace Betrian.App.Infrastructure.Options;

public interface IWritableOptions<out TOptions> : IOptionsMonitor<TOptions>
    where TOptions : class, new()
{
    TOptions Value { get; }
    void Update(Action<TOptions> applyChanges);
}
