using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Tarmi.Configuration.Devices;
using Tarmi.Configuration;
using System.Net;

namespace Tarmi.Devices.SmarAct.Stage.Implementation;

public class Mcs2CommunicationFactory : IMcs2CommunicationFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<Mcs2CommunicationFactory> _logger;

    public Mcs2CommunicationFactory(ILoggerFactory loggerFactory, ILogger<Mcs2CommunicationFactory> logger)
    {
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    public IMcs2Communication CreateCommunication(ApplicationConfig configuration)
    {
        var socket = InitializeSocket(configuration.Microscope.LinearStage);
        _logger.LogInformation("Creating MCS2 communication with socket {Socket}", socket);
        return new Mcs2Communication(socket, _loggerFactory.CreateLogger<Mcs2Communication>());
    }

    private static Socket InitializeSocket(LinearStageConfiguration configuration)
    {
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        socket.SendTimeout = socket.ReceiveTimeout = (int)configuration.Timeout.Milliseconds;
        var endpoint = new IPEndPoint(IPAddress.Parse(configuration.IPAddress), configuration.Port);
        socket.Connect(endpoint);
        return socket;
    }
}
