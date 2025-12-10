using System.Runtime.Serialization;
using Tarmi.Models.Serialization;

namespace Tarmi.Configuration.Alignments;

[DataContract(Namespace = Helpers.AppNamespace)]
public record FilterHandlerAlignment
{
    [DataMember]
    public int ReflectionFilterPosition { get; init; }

    [DataMember]
    public int FluorescenceFilterPosition { get; init; }
}
