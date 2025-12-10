using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using Tarmi.Configuration.Holders;

namespace Tarmi.Configuration;

[DataContract(Namespace = Helpers.AppNamespace)]
[Namespace(Prefix = "unit", Uri = "http://schemas.datacontract.org/2004/07/UnitsNet")]
[Namespace(Prefix = "array", Uri = "http://schemas.microsoft.com/2003/10/Serialization/Arrays")]
public class HoldersConfig
{
    [DataMember]
    public Holder[] Holders { get; init; } = [];
}
