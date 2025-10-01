using System.Runtime.Serialization;
using Betrian.Models.Serialization;
using UnitsNet;

namespace CFLMnavi.Configuration.Application;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record LinearStageFocus
{
    [DataMember]
    public List<Length> FocusSteps { get; init; } = [];
}
