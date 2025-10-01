using Fei.XT.ViewServer.gen;

namespace Betrian.Devices.Thermofisher.Instrument.ObjectModel.Abstractions;

internal interface IBrickConnector
{
    Result<T> GetObject<T>(string objectPath) where T : class;
    Result<ViewServer> GetViewServer();
    Result<PatternDataSource> GetPatterningDataSource(string viewName);
    Result<View> GetView(string viewName);
    void DisconnectionDetected();
    IObservable<bool> IsConnected { get; }
}
