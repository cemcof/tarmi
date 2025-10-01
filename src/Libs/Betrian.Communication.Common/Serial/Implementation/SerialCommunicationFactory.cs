using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Betrian.Communication.Common.Serial.Implementation;
public class SerialCommunicationFactory : ISerialCommunicationFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;

    public SerialCommunicationFactory(ILoggerFactory loggerFactory, ILogger<SerialCommunicationFactory> logger)
    {
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    // TODO: Find which settings should come from the configuration.
    public ISerialCommunication CreateSerialCommunication(SerialPortConfiguration configuration)
    {
        var serialPort = CreateSerialPort(configuration);
        var logger = _loggerFactory.CreateLogger<SerialCommunication>();
        _logger.LogInformation("Creating SerialCommunication with {@SerialPort}", serialPort);
        return new SerialCommunication(serialPort, logger);
    }

    private SerialPort CreateSerialPort(SerialPortConfiguration configuration)
    {
        var serialPort = new SerialPort()
        {
            PortName = configuration.PortName,
            BaudRate = configuration.BaudRate,
            Handshake = Handshake.None,
            NewLine = configuration.NewLine,
            Encoding = Encoding.ASCII,
            WriteTimeout = 1000,
        };
        serialPort.Open();
        return serialPort;
    }
}
