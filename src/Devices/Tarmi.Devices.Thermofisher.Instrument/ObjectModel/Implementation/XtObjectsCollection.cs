using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Tarmi.Devices.Thermofisher.Instrument.ObjectModel.Abstractions;
using Fei.XT.Server.BrickConnector;
using Fei.XT.ViewServer.gen;

namespace Tarmi.Devices.Thermofisher.Instrument.ObjectModel.Implementation;

internal class XtObjectsCollection : IXtObjectsCollection
{
    private readonly struct HandleDescriptor : IEqualityComparer<HandleDescriptor>
    {
        public required string ObjectKey { get; init; }

        public required Type ObjectType { get; init; }

        public bool Equals(HandleDescriptor x, HandleDescriptor y)
        {
            return
                string.Compare(x.ObjectKey, y.ObjectKey, StringComparison.Ordinal) == 0 &&
                x.ObjectType == y.ObjectType;
        }

        public int GetHashCode([DisallowNull] HandleDescriptor obj) => obj.GetHashCode();

        public override int GetHashCode() => ObjectKey.GetHashCode(StringComparison.Ordinal) ^ ObjectType.GetHashCode();
    }

    private readonly IBrickConnector _brickConnector;
    private readonly ConcurrentDictionary<HandleDescriptor, IXtObjectHandle> _handles = new();
    private readonly object _lockObj = new();

    public XtObjectsCollection(IBrickConnector brickConnector)
    {
        _brickConnector = brickConnector;
    }

    public IXtObjectHandle<T> GetObject<T>(string objectPath)
        where T : class
    {
        lock (_lockObj)
        {
            var descriptor = new HandleDescriptor { ObjectKey = objectPath, ObjectType = typeof(T) };
            var handle = _handles.GetOrAdd(descriptor, desc => new XtObjectModelHandle<T>(_brickConnector, desc.ObjectKey));
            return (IXtObjectHandle<T>)handle;
        }
    }

    private static string GetViewName(VsViewType view)
    {
        return view switch
        {
            VsViewType.View1 => "View 1",
            VsViewType.View2 => "View 2",
            VsViewType.View3 => "View 3",
            VsViewType.View4 => "View 4",
            _ => throw new NotSupportedException()
        };
    }

    public IXtObjectHandle<PatternDataSource> GetPatterningDataSource(VsViewType view)
    {
        lock (_lockObj)
        {
            var name = GetViewName(view);
            var descriptor = new HandleDescriptor { ObjectKey = $"{name}.Patterning", ObjectType = typeof(PatternDataSource) };
            var handle = _handles.GetOrAdd(descriptor, desc => new XtViewServerHandle<PatternDataSource>(_brickConnector, name));
            return (IXtObjectHandle<PatternDataSource>)handle;
        }
    }

    public IXtObjectHandle<View> GetView(VsViewType view)
    {
        lock (_lockObj)
        {
            var name = GetViewName(view);
            var descriptor = new HandleDescriptor { ObjectKey = name, ObjectType = typeof(View) };
            var handle = _handles.GetOrAdd(descriptor, desc => new XtViewServerHandle<View>(_brickConnector, name));
            return (IXtObjectHandle<View>)handle;
        }
    }

    public IXtObjectHandle<ViewServer> GetViewServer()
    {
        lock (_lockObj)
        {
            var descriptor = new HandleDescriptor { ObjectKey = "ViewServer", ObjectType = typeof(ViewServer) };
            var handle = _handles.GetOrAdd(descriptor, desc => new XtViewServerHandle<ViewServer>(_brickConnector, "ViewServer"));
            return (IXtObjectHandle<ViewServer>)handle;
        }
    }

    public void ConnectObjects()
    {
        lock (_lockObj)
        {
            foreach (var handle in _handles.Values)
            {
                if (!handle.IsConnected)
                {
                    _ = handle.Connect();
                }
            }
        }
    }

    public void DisconnectObjects()
    {
        lock (_lockObj)
        {
            foreach (var handle in _handles.Values)
            {
                handle.Disconnect();
            }
            _brickConnector.DisconnectionDetected();
        }
    }
}
