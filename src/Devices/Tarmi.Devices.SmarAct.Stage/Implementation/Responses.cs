using System.Diagnostics.CodeAnalysis;

namespace Tarmi.Devices.SmarAct.Stage.Implementation;

internal static class Responses
{
    internal readonly struct ErrorResponse : IParsable<ErrorResponse>
    {
        public readonly ResponseType Type;

        public ErrorResponse(ResponseType type) => Type = type;

        public static ErrorResponse Parse(string s, IFormatProvider? provider)
        {
            var splitIndex = s.IndexOf(',');
            if (splitIndex < 0)
            {
                throw new FormatException($"The input string '{s}' was not in a correct format.");
            }
            var responseType = Enum.Parse<ResponseType>(s.AsSpan()[..splitIndex]);
            return new ErrorResponse(responseType);
        }
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out ErrorResponse result) => throw new NotImplementedException();

        public static implicit operator ResponseType(ErrorResponse response) => response.Type;
    }

    /// <summary>
    /// A general response type for <see langword="enum"/> with distinct values.
    /// </summary>
    /// <typeparam name="T">The <see langword="enum"/> type of the inner value.</typeparam>
    internal readonly struct EnumResponse<T> : IParsable<EnumResponse<T>> where T : struct, Enum
    {
        public readonly T Value { get; }

        public EnumResponse(T value) => Value = value;

        public static EnumResponse<T> Parse(string s, IFormatProvider? provider)
        {
            var value = Enum.Parse<T>(s);
            if (!typeof(T).IsDefined(typeof(FlagsAttribute), false) && !Enum.IsDefined(value))
            {
                throw new FormatException("Enum value does not exists.");
            }
            return new EnumResponse<T>(value);
        }

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out EnumResponse<T> result)
        {
            if (!Enum.TryParse(s, out T enumValue) ||
                (!typeof(T).IsDefined(typeof(FlagsAttribute), false) && !Enum.IsDefined(enumValue)))
            {
                result = default;
                return false;
            }
            result = new(enumValue);
            return true;
        }

        public static implicit operator T(EnumResponse<T> value) => value.Value;
    }

    public readonly struct StringResponse : IParsable<StringResponse>
    {
        public string Value { get; }

        public StringResponse(string value) => Value = value;

        public static StringResponse Parse(string s, IFormatProvider? provider) => new(s);

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out StringResponse result)
        {
            result = new StringResponse(s ?? string.Empty);
            return s is not null;
        }

        public static implicit operator string(StringResponse value) => value.Value;
    }
}
