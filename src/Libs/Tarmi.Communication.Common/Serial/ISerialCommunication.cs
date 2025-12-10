namespace Tarmi.Communication.Common.Serial;

public interface ISerialCommunication : IDisposable
{
    Task SendCommandAsync(string command, CancellationToken cancellationToken = default);
    Task<string> SendCommandWithResponseAsync(string command, CancellationToken cancellationToken = default);
}
