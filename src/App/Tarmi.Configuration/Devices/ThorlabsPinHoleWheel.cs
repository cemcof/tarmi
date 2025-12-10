using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using Tarmi.Configuration.Alignments;

namespace Tarmi.Configuration.Devices;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record ThorlabsPinHoleWheel
{
    [DataMember]
    public SerialPort Port { get; init; } = new();

    [DataMember]
    public PinHoleWheelAlignments PinHoleWheelAlignments { get; init; } = new();
}
