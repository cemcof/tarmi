using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows.Data;
using UnitsNet;
using UnitsNet.Units;

namespace Tarmi.App.Converters;

[ValueConversion(typeof(IQuantity), typeof(string))]
public class UnitConverter : IValueConverter
{
    private const int SignificantDigits = 2;

    private record UnitConversionDescriptor
    {
        public required Enum BaseUnit { get; init; }
        public required Func<int, Enum> DisplayUnitForExponent { get; init; }
    }

    private static readonly Dictionary<Type, UnitConversionDescriptor> DefaultUnits = new()
    {
        [typeof(LengthUnit)] = new()
        {
            BaseUnit = LengthUnit.Meter,
            DisplayUnitForExponent = exponent => exponent switch
            {
                >= 3 and < 6 => LengthUnit.Kilometer,
                >= -3 and < 0 => LengthUnit.Millimeter,
                >= -6 and < -3 => LengthUnit.Micrometer,
                >= -9 and < -6 => LengthUnit.Nanometer,
                >= -12 and < -9 => LengthUnit.Picometer,
                _ => LengthUnit.Meter,
            }
        },
        [typeof(AngleUnit)] = new()
        {
            BaseUnit = AngleUnit.Degree,
            DisplayUnitForExponent = exponent => AngleUnit.Degree
        },
        [typeof(ElectricCurrentUnit)] = new()
        {
            BaseUnit = ElectricCurrentUnit.Ampere,
            DisplayUnitForExponent = exponent => exponent switch
            {
                >= 3 and < 6 => ElectricCurrentUnit.Kiloampere,
                >= -3 and < 0 => ElectricCurrentUnit.Milliampere,
                >= -6 and < -3 => ElectricCurrentUnit.Microampere,
                >= -9 and < -6 => ElectricCurrentUnit.Nanoampere,
                < -9 => ElectricCurrentUnit.Picoampere,
                _ => ElectricCurrentUnit.Ampere,
            }
        },
        [typeof(DurationUnit)] = new()
        {
            BaseUnit = DurationUnit.Second,
            DisplayUnitForExponent = exponent => exponent switch
            {
                >= -3 and < 0 => DurationUnit.Millisecond,
                >= -6 and < -3 => DurationUnit.Microsecond,
                < -6 => DurationUnit.Nanosecond,
                _ => DurationUnit.Second,
            }
        },
        [typeof(RatioUnit)] = new()
        {
            BaseUnit = RatioUnit.Percent,
            DisplayUnitForExponent = exponent => RatioUnit.Percent
        },
        [typeof(ElectricPotentialUnit)] = new()
        {
            BaseUnit = ElectricPotentialUnit.Volt,
            DisplayUnitForExponent = exponent => exponent switch
            {
                >= 3 and < 6 => ElectricPotentialUnit.Kilovolt,
                >= -3 and < 0 => ElectricPotentialUnit.Millivolt,
                >= -6 and < -3 => ElectricPotentialUnit.Microvolt,
                >= -9 and < -6 => ElectricPotentialUnit.Nanovolt,
                _ => ElectricPotentialUnit.Volt,
            }
        },
        [typeof(LevelUnit)] = new()
        {
            BaseUnit = LevelUnit.Decibel,
            DisplayUnitForExponent = exponent => exponent switch
            {
                _ => LevelUnit.Decibel,
            }
        },

        // Add more unit types here if needed
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not IQuantity quantity)
        {
            return "???";
            //throw new ArgumentException("Expected value of type UnitsNet.IQuantity.", nameof(value));
        }

        if (!DefaultUnits.TryGetValue(quantity.QuantityInfo.UnitType, out var defaultUnits))
        {
            // no definition for this unit type, return the default representation
            return quantity.ToString($"s{SignificantDigits}", culture);
        }

        var baseUnit = (double)quantity.ToUnit(defaultUnits.BaseUnit).Value;
        var exponent = baseUnit == 0 ? 0 : (int)Math.Floor(Math.Log10(Math.Abs(baseUnit)));
        var targetQuantityType = defaultUnits.DisplayUnitForExponent(exponent);
        var targetUnit = quantity.ToUnit(targetQuantityType);
        return targetUnit.ToString($"s{SignificantDigits}", culture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (!typeof(IQuantity).IsAssignableFrom(targetType))
        {
            throw new ArgumentException("Expected targetType of type UnitsNet.IQuantity.", nameof(value));
        }

        if (value is not string valueString || string.IsNullOrWhiteSpace(valueString))
        {
            return new ValidationResult("""Input is not valid. Expected a number or a string like "15 mA".""");
        }

        try
        {
            // If input is just a number, use the configured default unit
            if (double.TryParse(valueString, culture, out double number))
            {
                Type unitEnumType = Quantity.Infos.First(qi => qi.ValueType == targetType).UnitType;
                Enum defaultUnit;
                if (DefaultUnits.TryGetValue(unitEnumType, out var defaultUnits))
                {
                    defaultUnit = defaultUnits.BaseUnit;
                }
                else
                { 
                    defaultUnit = Enum.GetValues(unitEnumType).Cast<Enum>().First();
                }
                return Quantity.From(number, defaultUnit);
            }

            // Parse the quantity with the target type
            return Quantity.Parse(targetType, valueString);
        }
        catch (Exception ex)
        {
            return new ValidationResult(ex.InnerException?.Message ?? ex.Message);
        }
    }
}

