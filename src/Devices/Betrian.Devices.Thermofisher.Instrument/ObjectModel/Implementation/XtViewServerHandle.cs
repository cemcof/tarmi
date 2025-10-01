using Betrian.Devices.Thermofisher.Instrument.ObjectModel.Abstractions;
using Fei.XT.ViewServer.gen;

namespace Betrian.Devices.Thermofisher.Instrument.ObjectModel.Implementation;

internal class XtViewServerHandle<T> : IXtObjectHandle<T>
    where T : class
{
    private readonly IBrickConnector _brickConnector;
    private readonly string _viewName;
    private readonly object _lockObj = new();
    private T? _comImpl = null;

    public event EventHandler? Connected;
    public event EventHandler? Disconnecting;

    internal XtViewServerHandle(IBrickConnector brickConnector, string viewName)
    {
        _brickConnector = brickConnector;
        _viewName = viewName;
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
                if (typeof(T) == typeof(PatternDataSource))
                {
                    var result = _brickConnector.GetPatterningDataSource(_viewName);
                    if (result.IsSuccess)
                    {
                        _comImpl = result.Value as T;
                        Connected?.Invoke(this, EventArgs.Empty);
                    }
                    return result;
                }
                else if (typeof(T) == typeof(View))
                {
                    var result = _brickConnector.GetView(_viewName);
                    if (result.IsSuccess)
                    {
                        _comImpl = result.Value as T;
                        Connected?.Invoke(this, EventArgs.Empty);
                    }
                    return result;
                }
                else if (typeof(T) == typeof(ViewServer))
                {
                    var result = _brickConnector.GetViewServer();
                    if (result.IsSuccess)
                    {
                        _comImpl = result.Value as T;
                        Connected?.Invoke(this, EventArgs.Empty);
                    }
                    return result;
                }
                else
                {
                    return new Result(new InvalidOperationException("Unsupported object type"));
                }
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
