using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using UnitsNet;

namespace Tarmi.Configuration.Devices;

[DataContract(Namespace = Helpers.AppNamespace)]
public record LinearStageConfiguration
{
    [DataMember]
    public string IPAddress { get; init; } = System.Net.IPAddress.Loopback.ToString();

    [DataMember]
    public ushort Port { get; init; } = 12345;

    [DataMember]
    public Duration Timeout { get; init; } = Duration.FromSeconds(5);

    [DataMember]
    public int Channel { get; init; } = 2;
}
