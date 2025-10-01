namespace Betrian.Devices.Thermofisher.Instrument.ObjectModel.Abstractions;

internal interface IXtObjectHandle
{
    event EventHandler? Connected;
    event EventHandler? Disconnecting;
    bool IsConnected { get; }
    Result Connect();
    void Disconnect();
}

internal interface IXtObjectHandle<T> : IXtObjectHandle
    where T : class
{
    public T Object { get; }
}
