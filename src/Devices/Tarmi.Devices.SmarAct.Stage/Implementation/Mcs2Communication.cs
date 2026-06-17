using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Tarmi.Devices.SmarAct.Stage.Implementation;

public sealed class Mcs2Communication : IMcs2Communication
{
    private readonly byte[] _buffer = new byte[128];
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Socket _socket;
    private readonly ILogger<Mcs2Communication> _logger;

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);

    public Mcs2Communication(Socket socket, ILogger<Mcs2Communication> logger)
    {
        _socket = socket;
        _logger = logger;
    }

    public void Dispose()
    {
        _socket.Dispose();
        _semaphore.Dispose();
    }

    public async Task SendCommandAsync<TRequest>(CancellationToken cancellationToken = default) where TRequest : Commands.ICommitCommandBuilder<TRequest>
    {
        var command = Commands.Commit<TRequest>();
        using var lockGuard = await _semaphore.UseOnceAsync(cancellationToken);
        await SendCommandAsync(command, cancellationToken);
    }

    public async Task SendCommandAsync<TRequest>(int index, CancellationToken cancellationToken = default)
        where TRequest : Commands.IIndexedCommitCommandBuilder<TRequest>
    {
        var command = Commands.Commit<TRequest>(index);
        using var lockGuard = await _semaphore.UseOnceAsync(cancellationToken);
        await SendCommandAsync(command, cancellationToken);
    }

    public async Task<TResponse> SendCommandAsync<TRequest, TResponse>(CancellationToken cancellationToken = default)
        where TRequest : Commands.IReadCommandBuilder<TRequest>
        where TResponse : IParsable<TResponse>
    {
        var command = Commands.Read<TRequest>();
        using var lockGuard = await _semaphore.UseOnceAsync(cancellationToken);
        await SendCommandAsync(command, cancellationToken);
        return await ReceiveResponseAsync<TResponse>(cancellationToken);
    }

    public async Task<TResponse> SendCommandAsync<TRequest, TResponse>(int index, CancellationToken cancellationToken = default)
        where TRequest : Commands.IIndexedReadCommandBuilder<TRequest>
        where TResponse : IParsable<TResponse>
    {
        var command = Commands.Read<TRequest>(index);
        using var lockGuard = await _semaphore.UseOnceAsync(cancellationToken);
        await SendCommandAsync(command, cancellationToken);
        return await ReceiveResponseAsync<TResponse>(cancellationToken);
    }

    public async Task SendCommandAsync<TRequest>(object parameter, CancellationToken cancellationToken = default)
        where TRequest : Commands.IWriteCommandBuilder<TRequest>
    {
        var command = Commands.Write<TRequest>(parameter);
        using var lockGuard = await _semaphore.UseOnceAsync(cancellationToken);
        await SendCommandAsync(command, cancellationToken);
    }

    public async Task SendCommandAsync<TRequest>(int index, object parameter, CancellationToken cancellationToken = default)
        where TRequest : Commands.IIndexedWriteCommandBuilder<TRequest>
    {
        var command = Commands.Write<TRequest>(index, parameter);
        using var lockGuard = await _semaphore.UseOnceAsync(cancellationToken);
        await SendCommandAsync(command, cancellationToken);
    }

    private async Task SendCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellationTokenSource.CancelAfter(DefaultTimeout);

        var bytes = Encoding.ASCII.GetBytes(command);
        _logger.LogDebug("MCS2 sending: {Command}", command);
        var bytesSent = await _socket.SendAsync(bytes, cancellationTokenSource.Token);
        _logger.LogDebug("MCS2 sent: {Command}", command);
        if (bytesSent != bytes.Length)
        {
            _logger.LogError("Sent message was shorter than the original message.");
        }
    }

    private async Task<TResponse> ReceiveResponseAsync<TResponse>(CancellationToken cancellationToken = default)
        where TResponse : IParsable<TResponse>
    {
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellationTokenSource.CancelAfter(DefaultTimeout);

        _logger.LogDebug("MCS2 receiving");
        var bytesReceived = await _socket.ReceiveAsync(_buffer, cancellationTokenSource.Token);
        if (bytesReceived >= _buffer.Length)
        {
            _logger.LogError("Response might be longer than the buffer.");
        }

        var response = Encoding.ASCII
            .GetString(_buffer.AsSpan()[..bytesReceived])
            .TrimEnd();

        _logger.LogDebug("MCS2 received: {Response}", response);
        return TResponse.Parse(response, null);
    }
}
