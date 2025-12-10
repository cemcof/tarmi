using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using UnitsNet;

namespace Tarmi.Configuration.Alignments;

[DataContract(Namespace = Helpers.AppNamespace)]
public record PinHoleWheelAlignments
{
    [DataMember]
    public IEnumerable<PinHoleWheelAlignment> PinHoleAlignments { get; init; } = [];
}

[DataContract(Namespace = Helpers.AppNamespace)]
public record PinHoleWheelAlignment
{
    [DataMember]
    public Length PinHoleSize { get; init; }

    [DataMember]
    public Length Alignment { get; init; }
}
