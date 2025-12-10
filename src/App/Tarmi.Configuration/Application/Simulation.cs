using System.Runtime.Serialization;
using Tarmi.Models.Serialization;

namespace Tarmi.Configuration.Application;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record Simulation
{
    [DataMember]
    public bool Enabled { get; init; }
}
