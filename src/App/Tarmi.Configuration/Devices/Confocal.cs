using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using UnitsNet;

namespace Tarmi.Configuration.Devices;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record ConfocalStaff
{
    [DataMember]
    public string Name { get; init; } = string.Empty;

    [DataMember] 
    public ConfocalCamera ConfocalCamera { get; init; } = new ConfocalCamera();

    [DataMember]
    public ConfocalLights ConfocalLights { get; init; } = new ConfocalLights();
}

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record ConfocalCamera
{
    [DataMember]
    public string CameraName { get; init; } = "Confocal camera";

    [DataMember]
    public int Width { get; init; } = 4096;

    [DataMember]
    public int Height { get; init; } = 4096;

    [DataMember]
    public Length FieldWidth { get; init; } = Length.FromMicrometers(2.4 * 4096);

    [DataMember]
    public Length FieldHeight { get; init; } = Length.FromMicrometers(2.4 * 4096);
}

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record ConfocalLights
{
    [DataMember]
    public ConfocalLight ConfocalLight1 { get; init; } = new ConfocalLight { Wavelength = Length.FromNanometers(405), LightColor = ConfocalLightColor.COLOR1 };

    [DataMember]
    public ConfocalLight ConfocalLight2 { get; init; } = new ConfocalLight { Wavelength = Length.FromNanometers(488), LightColor = ConfocalLightColor.COLOR2 };

    [DataMember]
    public ConfocalLight ConfocalLight3 { get; init; } = new ConfocalLight { Wavelength = Length.FromNanometers(561), LightColor = ConfocalLightColor.COLOR3 };

    [DataMember]
    public ConfocalLight ConfocalLight4 { get; init; } = new ConfocalLight { Wavelength = Length.FromNanometers(640), LightColor = ConfocalLightColor.COLOR4 };
}

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record ConfocalLight
{
    [DataMember]
    public Length Wavelength { get; init; }

    [DataMember]
    public ConfocalLightColor LightColor { get; init; }

    public override string ToString() => $"{LightColor} {Wavelength.Nanometers} nm";
}

public enum ConfocalLightColor
{
    COLOR1,
    COLOR2,
    COLOR3,
    COLOR4
}
