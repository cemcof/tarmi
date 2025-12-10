using Tarmi.Models.Serialization;
using System.Runtime.Serialization;
using UnitsNet;

namespace Tarmi.Models;

[DataContract(Namespace = Helpers.AppNamespace)]
public record RangeDescriptor<T>
    where T : notnull
{
    [DataMember]
    public required T Min { get; init; }

    [DataMember]
    public required T Max { get; init; }

    public override string ToString()
        => $"[{Min}..{Max}]";
}

[DataContract(Namespace = Helpers.AppNamespace)]
public record RangeDescriptorWithStep<T> : RangeDescriptor<T>
    where T : notnull
{
    [DataMember]
    public required T Step { get; init; }

    public override string ToString()
        => $"[{Min}..{Max}] with {Step} step";
}

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record LengthRangeDescriptor : RangeDescriptor<Length>
{
    public bool IsValueInRange(Length value) => value >= Min && value <= Max;

    public override string ToString()
        => $"[{Min.Meters:0.000000}m..{Max.Meters:0.000000}m]";
}

public sealed record LengthRangeDescriptorWithStep : RangeDescriptorWithStep<Length>
{
    public bool IsValueInRange(Length value) => value >= Min && value <= Max;

    public override string ToString()
        => $"[{Min.Meters:0.000000}m..{Max.Meters:0.000000}m] with {Step.Meters:0.000000}m step";
}


[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record AngleRangeDescriptor : RangeDescriptor<Angle>
{
    public bool IsValueInRange(Angle value) => value >= Min && value <= Max;
}

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record AngleRangeDescriptorWithStep : RangeDescriptorWithStep<Angle>
{
    public bool IsValueInRange(Angle value) => value >= Min && value <= Max;

    public override string ToString()
        => $"[{Min.Radians:0.000000}rad..{Max.Radians:0.000000}rad] with {Step.Radians:0.000000}rad step";
}
