using System.Runtime.Serialization;
using Betrian.Models.Serialization;

namespace CFLMnavi.Configuration.Devices;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record FilterHandler
{
    [DataMember]
    public SerialPort Port { get; init; } = new();
}
