using System.Runtime.Serialization;
using Betrian.Models.Serialization;
using UnitsNet;

namespace CFLMnavi.Configuration.Application;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record LightColor
{
    [DataMember]
    public byte Red { get; init; }

    [DataMember]
    public byte Green { get; init; }

    [DataMember]
    public byte Blue { get; init; }
}

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record LightMapping
{
    [DataMember]
    public Length WaveLength { get; init; } = Length.Zero;

    [DataMember]
    public LightColor Color { get; init; } = new();
}

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record LightSettings
{
    [DataMember]
    public List<LightMapping> LightMappings { get; init; } = [];
}

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record ImageColoring
{
    [DataMember]
    public LightSettings Fluorescence { get; init; } = new();

    [DataMember]
    public LightSettings Reflection { get; init; } = new();
}
