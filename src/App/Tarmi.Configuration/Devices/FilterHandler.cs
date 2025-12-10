using System.Runtime.Serialization;
using Tarmi.Models.Serialization;

namespace Tarmi.Configuration.Devices;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record FilterHandler
{
    [DataMember]
    public SerialPort Port { get; init; } = new();
}
