using System.Runtime.Serialization;
using Betrian.Models.Serialization;

namespace CFLMnavi.Projects;

[DataContract(Namespace = Helpers.AppNamespace)]
public class FiducialDescriptor
{
    [DataMember]
    public Guid Id { get; init; } = UUIDNext.Uuid.NewSequential();

    [DataMember]
    public required string Name { get; set; }
}
