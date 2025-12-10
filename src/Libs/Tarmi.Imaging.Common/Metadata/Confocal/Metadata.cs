using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using UnitsNet;

namespace Tarmi.Imaging.Common.Metadata.Confocal;

[DataContract(Namespace = Helpers.AppNamespace)]
[Namespace(Prefix = "unit", Uri = "http://schemas.datacontract.org/2004/07/UnitsNet")]
public record Metadata
{
    [DataMember]
    public Length PixelSizeX { get; init; } = Length.Zero;

    [DataMember]
    public Length PixelSizeY { get; init; } = Length.Zero;

    [DataMember]
    public Length WorkingDistance { get; init; } = Length.Zero;

    [DataMember]
    public Length LightWavelength { get; init; } = Length.Zero;

    [DataMember]
    public Ratio LightIntensity { get; init; } = Ratio.Zero;

    [DataMember]
    public LuminescenceMode Mode { get; init; } = LuminescenceMode.Fluorescence;

    [DataMember]
    public Length PinholePosition { get; init; } = Length.Zero;

    [DataMember]
    public Length FilterWheelColor { get; init; } = Length.Zero;

    [DataMember]
    public Level Gain { get; init; }

    [DataMember]
    public ElectricPotential ADC { get; init; }

    [DataMember]
    public Duration Dwell { get; init; }

    [DataMember(EmitDefaultValue = true)]
    public StackInfo? StackInfo { get; init; } = null;

    [DataMember]
    public string ImagePath { get; init; } = string.Empty;
}
