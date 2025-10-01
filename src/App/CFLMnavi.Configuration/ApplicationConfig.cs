using System.Runtime.Serialization;
using Betrian.Models.Serialization;
using CFLMnavi.Configuration.Application;
using CFLMnavi.Configuration.Devices;

namespace CFLMnavi.Configuration;

[DataContract(Namespace = Helpers.AppNamespace)]
[Namespace(Prefix = "unit", Uri = "http://schemas.datacontract.org/2004/07/UnitsNet")]
[Namespace(Prefix = "array", Uri = "http://schemas.microsoft.com/2003/10/Serialization/Arrays")]
public sealed record ApplicationConfig
{
    [DataMember]
    public required Simulation Simulation { get; init; }

    [DataMember]
    public required UserPreferences UserPreferences { get; init; }

    [DataMember]
    public required Microscope Microscope { get; init; }
}
