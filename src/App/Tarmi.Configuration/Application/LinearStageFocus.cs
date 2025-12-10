using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using UnitsNet;

namespace Tarmi.Configuration.Application;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record LinearStageFocus
{
    [DataMember]
    public List<Length> FocusSteps { get; init; } = [];
}
