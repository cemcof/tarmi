using System.Runtime.Serialization;
using Betrian.Models.Serialization;
using UnitsNet;

namespace Betrian.Imaging.Common.Metadata.Luminescence;

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
    public CameraParameters Camera { get; init; } = new CameraParameters();

    [DataMember(EmitDefaultValue = true)]
    public StackInfo? StackInfo { get; init; } = null;
}
