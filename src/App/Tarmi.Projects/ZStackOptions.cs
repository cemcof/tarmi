using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using UnitsNet;

namespace Tarmi.Projects;

[DataContract(Namespace = Helpers.AppNamespace)]
public record ZStackOptions
{
    [DataMember]
    public required Length StartPosition { get; init; }
    [DataMember]
    public required Length Step { get; init; }
    [DataMember]
    public required int NumberOfSteps { get; init; }
}
