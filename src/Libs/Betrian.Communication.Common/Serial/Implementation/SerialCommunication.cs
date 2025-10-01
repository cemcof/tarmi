using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Betrian.Communication.Common.Serial.Implementation;
public sealed class SerialCommunication : ISerialCommunication
{
    private readonly SerialPort _serialPort;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public SerialCommunication(SerialPort serialPort, ILogger<SerialCommunication> logger)
    {
        _serialPort = serialPort;
        _logger = logger;
    }

    public void Dispose()
    {
        _serialPort.Dispose();
        _semaphore.Dispose();
    }

    public async Task SendCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        using var lockGuard = await _semaphore.UseOnceAsync(cancellationToken);
        _logger.LogInformation("Sending command {Command} to port {Port}.", command, _serialPort.PortName);
        await _serialPort.WriteLineAsync(command, cancellationToken);
    }

    public async Task<string> SendCommandWithResponseAsync(string command, CancellationToken cancellationToken)
    {
        using var lockGuard = await _semaphore.UseOnceAsync(cancellationToken);
        _logger.LogInformation("Sending command {Command} to port {Port}.", command, _serialPort.PortName);
        await _serialPort.WriteLineAsync(command, cancellationToken);
        string response = await _serialPort.ReadLineAsync(cancellationToken);
        _logger.LogInformation("Received response {Response} from port {Port}.", response, _serialPort.PortName);
        return response;
    }
}
