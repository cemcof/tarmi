using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tarmi.Devices.Thermofisher.Instrument.ObjectModel.Abstractions;
using Fei.XT.Instrument.gen;

namespace Tarmi.Devices.Thermofisher.Instrument.ObjectModel.Implementation;

internal class XtConnectionService : BackgroundService
{
    private readonly ILogger<XtConnectionService> _logger;
    private readonly IXtObjectsCollection _xtObjects;
    private readonly IBrickConnector _brickConnector;
    private IInstrumentInfo? _instrumentInfo = null;

    public XtConnectionService(ILogger<XtConnectionService> logger, IXtObjectsCollection xtObjects, IBrickConnector brickConnector)
    {
        _logger = logger;
        _xtObjects = xtObjects;
        _brickConnector = brickConnector;
    }

    public override Task StartAsync(CancellationToken cancellationToken) => base.StartAsync(cancellationToken);

    public override Task StopAsync(CancellationToken cancellationToken) => base.StopAsync(cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            try
            {
                CheckXtConnection();
            }
            catch
            {
            }
        }
    }

    private void CheckXtConnection()
    {
        if (_instrumentInfo is null)
        {
            var result = _brickConnector.GetObject<IInstrumentInfo>(PathLiterals.Instrument.Service.InstrumentInfo.AsString);
            if (result.IsSuccess)
            {
                _instrumentInfo = result.Value;
            }
            else
            {
                return;
            }
        }

        if (_instrumentInfo is not null)
        {
            try
            {
                _ = _instrumentInfo!.InstrumentServerVersion;
                _xtObjects.ConnectObjects(); // not all objects must be connected already
            }
            catch
            {
                _instrumentInfo = null;
                _xtObjects.DisconnectObjects();
            }
        }
    }
}
