using System.Runtime.Serialization;
using Betrian.Models.Serialization;

namespace CFLMnavi.Projects;

// NOTE: Adding known types attribute leads to serialization
// of the inherited class, while we only want to serialize
// members of the base class.
[DataContract(Name = nameof(Project), Namespace = Helpers.AppNamespace)]
[Namespace(Prefix = "unit", Uri = "http://schemas.datacontract.org/2004/07/UnitsNet")]
[Namespace(Prefix = "array", Uri = "http://schemas.microsoft.com/2003/10/Serialization/Arrays")]
[Namespace(Prefix = "system", Uri = "http://schemas.datacontract.org/2004/07/System")]
public record ProjectDescriptor
{
    [DataMember]
    public required string Name { get; init; }

    [DataMember]
    public required string Description { get; set; }

    [DataMember]
    public required DateTimeOffset TimeOfCreation { get; init; }

    [DataMember]
    public required DateTimeOffset TimeOfAccess { get; set; }

    // Needs to be set manually after deserialization
    [IgnoreDataMember]
    public required string Directory { get; set; }
}
