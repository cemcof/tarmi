using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using Tarmi.Configuration.Holders;

namespace Tarmi.Projects;

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
