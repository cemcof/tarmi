using System.Runtime.Serialization;
using Betrian.Models.Serialization;
using CFLMnavi.Configuration.Holders;

namespace CFLMnavi.Projects;

[DataContract(Namespace = Helpers.AppNamespace)]
[Namespace(Prefix = "unit", Uri = "http://schemas.datacontract.org/2004/07/UnitsNet")]
[Namespace(Prefix = "array", Uri = "http://schemas.microsoft.com/2003/10/Serialization/Arrays")]
[Namespace(Prefix = "system", Uri = "http://schemas.datacontract.org/2004/07/System")]
public record Project : ProjectDescriptor
{
    [DataMember]
    public required Holder Holder { get; init; }

    [DataMember]
    public List<RegionOfInterest> RegionsOfInterest { get; init; } = [];
}
