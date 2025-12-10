using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Tarmi.Devices.Thermofisher.Instrument.ObjectModel.Abstractions;

internal record Result
{
    public static readonly Result Success = new(true);
    public Exception? Exception { get; init; }
    public bool IsSuccess { get; init; }

    public Result(Exception? exception = null) => Exception = exception;

    public Result(bool isSuccess) => IsSuccess = isSuccess;

    public static implicit operator bool(Result result) => result.IsSuccess;

    public Result<TOther> Cast<TOther>() => new() { Exception = Exception };
}

internal record Result<T> : Result
{
    public Result(Exception? exception = null)
        : base(exception) { }

    public Result(T value)
        : base(true) => Value = value;

    public T? Value { get; init; }
}

internal static class ResultExtensions
{
    private const int E_INVALIDARG = unchecked((int)0x80070057);
    private const string XtServerNotRunningMessage = "xT server is not running";
    // when xT server is going down
    private const int RPC_E_DISCONNECTED = unchecked((int)0x80010108);
    // when xT server is down
    public const int RPC_E_SERVER_UNAVAILABLE = unchecked((int)0x800706BE);

    public static Result<TOther> Cast<T, TOther>(this Result<T> result)
        where T : TOther
        => new() { Exception = result.Exception, IsSuccess = result.IsSuccess, Value = result.Value };

    public static Result<T> MapToResult<T>(this COMException exception)
    {
        if (exception.HResult == E_INVALIDARG)
        {
            var argException = new ArgumentException(exception.Message, exception);
            return new Result<T>(argException);
        }
        else if (exception.HResult == RPC_E_DISCONNECTED || exception.HResult == RPC_E_SERVER_UNAVAILABLE)
        {
            var iopException = new InvalidOperationException(XtServerNotRunningMessage, exception);
            return new Result<T>(iopException);
        }
        return new Result<T>(exception);
    }

    public static Result MapToResult(this COMException exception)
    {
        if (exception.HResult == E_INVALIDARG)
        {
            var argException = new ArgumentException(exception.Message, exception);
            return new Result(argException);
        }
        else if (exception.HResult == RPC_E_DISCONNECTED || exception.HResult == RPC_E_SERVER_UNAVAILABLE)
        {
            var iopException = new InvalidOperationException(XtServerNotRunningMessage, exception);
            return new Result(iopException);
        }
        return new Result(exception);
    }

    public static Result<T> MapToResult<T>(this Exception exception)
        => exception is COMException cex ? cex.MapToResult<T>() : new Result<T>(exception);

    public static Result MapToResult(this Exception exception)
        => exception is COMException cex ? cex.MapToResult() : new Result(exception);
}
