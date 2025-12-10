using Tarmi.Devices.Thermofisher.Instrument.ObjectModel.Abstractions;

namespace Tarmi.Devices.Thermofisher.Instrument.ObjectModel.Implementation;

internal class XtObjectModelHandle<T> : IXtObjectHandle<T>
    where T : class
{
    private readonly IBrickConnector _brickConnector;
    private readonly string _objectName;
    private readonly object _lockObj = new();
    private T? _comImpl = null;

    public event EventHandler? Connected;
    public event EventHandler? Disconnecting;

    internal XtObjectModelHandle(IBrickConnector brickConnector, string objectName)
    {
        _brickConnector = brickConnector;
        _objectName = objectName;
    }

    public bool IsConnected => _comImpl != null;

    public T Object
    {
        get
        {
            if (!IsConnected)
            {
                var result = Connect();
                if (!result.IsSuccess)
                {
#pragma warning disable CA1065
                    throw result.Exception!;
#pragma warning restore CA1065
                }
            }
            return _comImpl!;
        }
    }

    public Result Connect()
    {
        lock (_lockObj)
        {
            if (!IsConnected)
            {
                var result = _brickConnector.GetObject<T>(_objectName);
                if (result.IsSuccess)
                {
                    _comImpl = result.Value;
                    Connected?.Invoke(this, EventArgs.Empty);
                }
                return result;
            }
            return Result.Success;
        }
    }

    public void Disconnect()
    {
        lock (_lockObj)
        {
            if (IsConnected)
            {
                try
                {
                    // RPC server might be already down for events unregistering
                    Disconnecting?.Invoke(this, EventArgs.Empty);
                }
                catch { }
                finally
                {
                    _comImpl = null;
                }
            }
        }
    }
}
