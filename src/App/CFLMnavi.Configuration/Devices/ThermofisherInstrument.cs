using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Betrian.Models.Serialization;

namespace CFLMnavi.Configuration.Devices;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record ThermofisherInstrument
{
    [Range(1, 4, ErrorMessage = "The quad number must be in range [1..4].")]
    [DataMember]
    public int SemQuad { get; init; } = 1;

    [Range(1, 4, ErrorMessage = "The quad number must be in range [1..4].")]
    [DataMember]
    public int IonQuad { get; init; } = 2;
}
