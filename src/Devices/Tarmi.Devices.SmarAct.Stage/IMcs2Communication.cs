using Tarmi.Devices.SmarAct.Stage.Implementation;

namespace Tarmi.Devices.SmarAct.Stage;

public interface IMcs2Communication : IDisposable
{
    public Task SendCommandAsync<TRequest>(CancellationToken cancellationToken = default)
        where TRequest : Commands.ICommitCommandBuilder<TRequest>;

    public Task SendCommandAsync<TRequest>(int index, CancellationToken cancellationToken = default)
        where TRequest : Commands.IIndexedCommitCommandBuilder<TRequest>;

    public Task<TResponse> SendCommandAsync<TRequest, TResponse>(CancellationToken cancellationToken = default)
        where TRequest : Commands.IReadCommandBuilder<TRequest>
        where TResponse : IParsable<TResponse>;

    public Task<TResponse> SendCommandAsync<TRequest, TResponse>(int index, CancellationToken cancellationToken = default)
        where TRequest : Commands.IIndexedReadCommandBuilder<TRequest>
        where TResponse : IParsable<TResponse>;

    public Task SendCommandAsync<TRequest>(object parameter, CancellationToken cancellationToken = default)
        where TRequest : Commands.IWriteCommandBuilder<TRequest>;

    public Task SendCommandAsync<TRequest>(int index, object parameter, CancellationToken cancellationToken = default)
        where TRequest : Commands.IIndexedWriteCommandBuilder<TRequest>;
}
