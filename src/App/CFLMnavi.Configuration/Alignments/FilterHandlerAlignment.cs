using System.Runtime.Serialization;
using Betrian.Models.Serialization;

namespace CFLMnavi.Configuration.Alignments;

[DataContract(Namespace = Helpers.AppNamespace)]
public record FilterHandlerAlignment
{
    [DataMember]
    public int ReflectionFilterPosition { get; init; }

    [DataMember]
    public int FluorescenceFilterPosition { get; init; }
}
