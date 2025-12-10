using Fei.XT.Instrument.gen;

namespace Tarmi.Devices.Thermofisher.Instrument.ObjectModel;

internal static class Extensions
{
    public static TResult? Get<TResult>(this IMemento memento, string key)
    {
        try
        {
            var mementoItem = memento.Find(key);
            return mementoItem is { Valid: true } ? (TResult)Convert.ChangeType(mementoItem.Value, typeof(TResult)) : default;
        }
        catch (Exception ex)
        {
#if DEBUG
            Console.WriteLine($"KEY: '{key}', {ex.Message}");
#endif
            _ = ex;
            return default;
        }
    }
}
