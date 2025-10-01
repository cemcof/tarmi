using Spectre.Console;
using Spectre.Console.Cli;

namespace CFlMnavi.Installer.Validators;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
internal sealed class RuntimeVersionValidatorAttribute : ParameterValidationAttribute
{
    public RuntimeVersionValidatorAttribute(string errorMessage)
        : base(errorMessage)
    {
    }

    public override ValidationResult Validate(CommandParameterContext context)
    {
        ValidationResult GetResult(int value) => value switch
        {
            >= 8 and <= 10 => ValidationResult.Success(),
            _ => ValidationResult.Error($".Net Runtime version is not supported ({context.Parameter.PropertyName})."),
        };


        if (context.Value is FlagValue<int?> flaggedInt)
        {
            if (flaggedInt.Value.HasValue)
            {
                return GetResult(flaggedInt.Value.Value);
            }
        }
        else if (context.Value is int value)
        {
            return GetResult(value);
        }
        else if (context.Value is string stringValue)
        {
            if (int.TryParse(stringValue, out var intValue))
            {
                return GetResult(intValue);
            }
        }

        throw new InvalidOperationException($"Parameter is not a number ({context.Parameter.PropertyName}).");
    }
}
