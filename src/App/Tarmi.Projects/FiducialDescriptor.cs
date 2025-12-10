using System.Runtime.Serialization;
using Tarmi.Models.Serialization;

namespace Tarmi.Projects;

[DataContract(Namespace = Helpers.AppNamespace)]
public class FiducialDescriptor
{
    [DataMember]
    public Guid Id { get; init; } = UUIDNext.Uuid.NewSequential();

    [DataMember]
    public required string Name { get; set; }
}
