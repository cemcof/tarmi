using Spectre.Console;
using Spectre.Console.Cli;

namespace CFlMnavi.Installer.Validators;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
internal sealed class VersionValidatorAttribute : ParameterValidationAttribute
{
    public VersionValidatorAttribute(string errorMessage)
        : base(errorMessage)
    {
    }

    public override ValidationResult Validate(CommandParameterContext context)
    {
        if (context.Value is Version)
        {
            return ValidationResult.Success();
        }
        if (context.Value is int)
        {
            return ValidationResult.Success();
        }
        else if (context.Value is string value)
        {
            if (Version.TryParse(value, out var _))
            {
                return ValidationResult.Success();
            }
            return ValidationResult.Error($".Invalid version format ({context.Parameter.PropertyName}).");
        }

        throw new InvalidOperationException($"Parameter is not a string or integer ({context.Parameter.PropertyName}).");
    }
}
