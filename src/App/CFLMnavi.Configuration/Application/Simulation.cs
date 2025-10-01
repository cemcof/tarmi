using System.Runtime.Serialization;
using Betrian.Models.Serialization;

namespace CFLMnavi.Configuration.Application;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record Simulation
{
    [DataMember]
    public bool Enabled { get; init; }
}
