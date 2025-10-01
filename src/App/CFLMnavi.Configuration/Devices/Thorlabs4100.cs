using System.Runtime.Serialization;
using Betrian.Models.Serialization;
using UnitsNet;

namespace CFLMnavi.Configuration.Devices;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record Thorlabs4100Lights
{
    [DataMember]
    public Length RedWavelength { get; init; } = Length.FromNanometers(625);

    [DataMember]
    public Length GreenWavelength { get; init; } = Length.FromNanometers(565);

    [DataMember]
    public Length BlueWavelength { get; init; } = Length.FromNanometers(470);

    [DataMember]
    public Length UltraVioletWavelength { get; init; } = Length.FromNanometers(385);
}

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record Thorlabs4100
{
    [DataMember]
    public SerialPort Port { get; init; } = new() { DeviceName = "COM1" };

    [DataMember]
    public Thorlabs4100Lights Lights { get; init; } = new();
}
