using Fei.XT.Server.BrickConnector;
using Fei.XT.ViewServer.gen;

namespace Betrian.Devices.Thermofisher.Instrument.ObjectModel.Abstractions;

internal interface IXtObjectsCollection
{
    IXtObjectHandle<T> GetObject<T>(string objectPath) where T : class;
    IXtObjectHandle<ViewServer> GetViewServer();
    IXtObjectHandle<PatternDataSource> GetPatterningDataSource(VsViewType view);
    IXtObjectHandle<View> GetView(VsViewType view);
    void ConnectObjects();
    void DisconnectObjects();
}
