using System.Runtime.Serialization;
using Betrian.Models.Serialization;
using CFLMnavi.Configuration.Holders;

namespace CFLMnavi.Configuration;

[DataContract(Namespace = Helpers.AppNamespace)]
[Namespace(Prefix = "unit", Uri = "http://schemas.datacontract.org/2004/07/UnitsNet")]
[Namespace(Prefix = "array", Uri = "http://schemas.microsoft.com/2003/10/Serialization/Arrays")]
public class HoldersConfig
{
    [DataMember]
    public Holder[] Holders { get; init; } = [];
}
