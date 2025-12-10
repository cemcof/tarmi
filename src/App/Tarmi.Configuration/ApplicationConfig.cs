using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using Tarmi.Configuration.Application;
using Tarmi.Configuration.Devices;

namespace Tarmi.Configuration;

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

    [DataMember]
    public required Features Features { get; init; }
}
