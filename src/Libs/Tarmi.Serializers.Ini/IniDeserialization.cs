using System.Collections;
using System.Globalization;
using System.Net;
using System.Reflection;
using SimpleBase;
using System.Linq;

namespace Tarmi.Serializers.Ini;

internal static class IniDeserialization
{
    private const string ArraySeparator = ", ";

    public static T Deserialize<T>(string content)
        where T : class, new()
    {
        var obj = new T();
        var properties = GetStringProperties(content);
        var type = obj.GetType();
        foreach (var propertyInfo in type.GetProperties().Where(propertyInfo => propertyInfo.IsDefined(typeof(IniSectionAttribute))))
        {
            var sectionAttribute = propertyInfo.GetCustomAttribute<IniSectionAttribute>();
            object? sectionObj = null;
            var method = propertyInfo.GetGetMethod();
            if (method != null)
            {
                sectionObj = method.Invoke(obj, null);
                if (sectionObj == null)
                {
                    method = propertyInfo.GetSetMethod(true);
                    if (method != null)
                    {
                        sectionObj = Activator.CreateInstance(propertyInfo.PropertyType);
                        _ = method.Invoke(obj, [sectionObj]);
                    }
                }
            }
            if (sectionObj is not null)
            {
                foreach (var sectionProperty in sectionObj.GetType().GetProperties())
                {
                    var setMethod = sectionProperty.GetSetMethod();
                    if (setMethod is not null)
                    {
                        var sectionPropertyName = sectionProperty.GetCustomAttribute<IniValueAttribute>()?.Name ?? sectionProperty.Name;
                        var property = properties.Find(p => p.Section == sectionAttribute!.Name && p.Name == sectionPropertyName);
                        if (property is not null)
                        {
                            var processedValue = ProcessValue(property.Value?.ToString() ?? string.Empty, sectionProperty.PropertyType, sectionProperty);
                            _ = setMethod.Invoke(sectionObj, [processedValue]);
                        }
                    }
                }
            }
        }

        return obj;
    }

    private static List<Property> GetStringProperties(string content)
    {
        var properties = new List<Property>();
        string currentSection = "";

        using var reader = new StringReader(content);
        reader.ForEachLine(line =>
        {
            line = line.Trim();
            if (line.StartsWith('['))
            {
                currentSection = line.Replace("[", "", StringComparison.Ordinal).Replace("]", "", StringComparison.Ordinal);
            }
            else if (line.Contains('=', StringComparison.Ordinal) && !line.StartsWith('#'))
            {
                line = line.Replace(" =", "=", StringComparison.Ordinal).Replace("= ", "=", StringComparison.Ordinal);
                var settingParts = line.Split('=');
                if (settingParts.Length > 1)
                {
                    var key = settingParts[0];
                    var value = "";
                    for (int s = 0; s < settingParts.Length; s++)
                    {
                        if (s != 0)
                        {
#pragma warning disable S1643 // Strings should not be concatenated using '+' in a loop
                            value += (s == settingParts.Length - 1) ? settingParts[s] : settingParts[s] + " ";
#pragma warning restore S1643 // Strings should not be concatenated using '+' in a loop
                        }
                    }
                    var property = new Property()
                    {
                        Section = currentSection,
                        Name = key,
                        Value = value
                    };
                    properties.Add(property);
                }
            }
        });

        return properties;
    }

    private static DateOnly ProcessDateOnly(string value, PropertyInfo? propertyInfo)
    {
        var customAttr = propertyInfo?.GetCustomAttribute<IniValueFormatterAttribute>();
        if (
            customAttr is not null &&
            DateOnly.TryParseExact(value, customAttr.Formatter, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d1)
        )
        {
            return d1;
        }
        return DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d2) ? d2 : DateOnly.MinValue;
    }

    private static TimeOnly ProcessTimeOnly(string value, PropertyInfo? propertyInfo)
    {
        var customAttr = propertyInfo?.GetCustomAttribute<IniValueFormatterAttribute>();
        if (
            customAttr is not null &&
            TimeOnly.TryParseExact(value, customAttr.Formatter, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d1)
        )
        {
            return d1;
        }
        return TimeOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d2) ? d2 : TimeOnly.MinValue;
    }

    private static DateTime ProcessDateTime(string value, PropertyInfo? propertyInfo)
    {
        var customAttr = propertyInfo?.GetCustomAttribute<IniValueFormatterAttribute>();
        if (
            customAttr is not null &&
            DateTime.TryParseExact(value, customAttr.Formatter, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d1)
        )
        {
            return d1;
        }
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d2) ? d2 : DateTime.MinValue;
    }

    private static object? ProcessValue(string value, Type propertyType, PropertyInfo? propertyInfo = null)
    {
        if (propertyType == typeof(string))
        {
            return value;
        }
        else if (propertyType.IsEnum)
        {
            return Enum.Parse(propertyType, value, true);
        }
        else if (propertyType == typeof(DateOnly))
        {
            return ProcessDateOnly(value, propertyInfo);
        }
        else if (propertyType == typeof(DateOnly?))
        {
            var dt = ProcessDateOnly(value, propertyInfo);
            return dt != DateOnly.MinValue ? dt : null;
        }
        else if (propertyType == typeof(TimeOnly))
        {
            return ProcessTimeOnly(value, propertyInfo);
        }
        else if (propertyType == typeof(TimeOnly?))
        {
            var dt = ProcessTimeOnly(value, propertyInfo);
            return dt != TimeOnly.MinValue ? dt : null;
        }
        else if (propertyType == typeof(DateTime))
        {
            return ProcessDateTime(value, propertyInfo);
        }
        else if (propertyType == typeof(DateTime?))
        {
            var dt = ProcessDateTime(value, propertyInfo);
            return dt != DateTime.MinValue ? dt : null;
        }
        else if (propertyType == typeof(TimeSpan))
        {
            if (TimeSpan.TryParse(value!.ToString(), CultureInfo.InvariantCulture, out var span))
            {
                return span;
            }
        }
        else if (propertyType == typeof(IPAddress))
        {
            if (IPAddress.TryParse(value, out var Address))
            {
                return Address;
            }
        }
        else if (propertyType == typeof(bool))
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            var attr = propertyInfo?.GetCustomAttribute<IniBoolValueFormatterAttribute>();
            if (attr is not null)
            {
                if (value == attr.TrueValue)
                {
                    return true;
                }
                else if (value == attr.FalseValue)
                {
                    return false;
                }
            }
            else if (bool.TryParse(value, out var result))
            {
                return result;
            }
        }
        else if (propertyType == typeof(double))
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0.0;
            }
            value = value.Replace(',', '.');
            if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }
        }
        else if (propertyType == typeof(float))
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0.0;
            }
            value = value.Replace(',', '.');
            if (float.TryParse(value, NumberStyles.Float | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
        }
        else if (propertyType == typeof(decimal))
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0.0;
            }
            value = value.Replace(',', '.');
            if (decimal.TryParse(value, NumberStyles.Float | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out decimal result))
            {
                return result;
            }
        }
        else if (propertyType.IsPrimitive)
        {
            return string.IsNullOrWhiteSpace(value)
                ? Activator.CreateInstance(propertyType)
                : Convert.ChangeType(value, propertyType, CultureInfo.InvariantCulture);
        }
        else if (propertyType == typeof(Uri))
        {
            if (Uri.TryCreate(value!.ToString(), UriKind.RelativeOrAbsolute, out var uri))
            {
                return uri;
            }
        }
        else if (propertyType == typeof(Version))
        {
            if (Version.TryParse(value!.ToString(), out var version))
            {
                return version;
            }
        }
        else if (propertyType == typeof(IPEndPoint) || propertyType == typeof(EndPoint))
        {
            if (value!.ToString().Contains(':', StringComparison.Ordinal))
            {
                var parts = value.ToString().Split(':');
                var ip = IPAddress.Parse(parts[0]);
                var port = int.Parse(parts[1]);
                return new IPEndPoint(ip, port);
            }
            return null;
        }
        else if (propertyType == typeof(byte[]))
        {
            return Base16.Decode(value.ToString()).ToArray();
        }
        else if (propertyType.IsArray)
        {
            var arrayType = propertyType.GetElementType()!;
            var elements = value.Split(new string[] { ArraySeparator }, StringSplitOptions.None);
            var newArray = Array.CreateInstance(arrayType, elements.Length);
            for (var i = 0; i < elements.Length; i++)
            {
                newArray.SetValue(ProcessValue(elements[i], arrayType), i);
            }
            return newArray;
        }
        else if (value != null && propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
        {
            var listType = propertyType.GetGenericArguments()[0];
            List<string> elements = [.. value.Split(new string[] { ArraySeparator }, StringSplitOptions.None)];
            var list = (IList)propertyType.GetConstructor([typeof(int)])!.Invoke([elements.Count]);
            for (var i = 0; i < elements.Count; i++)
            {
                _ = list.Add(ProcessValue(elements[i], listType));
            }
            return list;
        }
        else if (value != null && propertyType.IsGenericType && propertyType.GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>)))
        {
            var keyType = propertyType.GetGenericArguments()[0];
            var valueType = propertyType.GetGenericArguments()[1];

            var items = value.GetArrayElements();
            var dictionary = (IDictionary)propertyType.GetConstructor([typeof(int)])!.Invoke([items.Count / 2]);
            foreach (string item in items)
            {
                var kv = item.Split(ArraySeparator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                var key = ProcessValue(kv[0], keyType)!;
                var val = ProcessValue(kv[1], valueType);
                dictionary.Add(key, val);
            }
            return dictionary;
        }
        else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition().IsAssignableFrom(typeof(KeyValuePair<,>)))
        {
            var keyType = propertyType.GetGenericArguments()[0];
            var valueType = propertyType.GetGenericArguments()[1];

            var ctor = propertyType.GetConstructor([keyType, valueType])!;
            List<string> Items = value!.GetArrayElements();
            foreach (string item in Items)
            {
                var kv = item.Split(ArraySeparator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                var key = ProcessValue(kv[0], keyType);
                var val = ProcessValue(kv[1], valueType);
#pragma warning disable S1751 // Loops with at most one iteration should be refactored
                // TODO: check if this can be refactored
                return ctor.Invoke([key, val]);
#pragma warning restore S1751 // Loops with at most one iteration should be refactored
            }
        }

        throw new NotSupportedException($"Type {propertyType} is not supported");
    }
}
