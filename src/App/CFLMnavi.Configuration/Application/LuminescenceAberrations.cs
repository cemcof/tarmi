using System.Runtime.Serialization;
using Betrian.Models.Serialization;
using UnitsNet;

namespace CFLMnavi.Configuration.Application;

[DataContract(Namespace = Helpers.AppNamespace)]
public class LuminescenceAberrations
{
    [DataMember]
    public Dictionary<string, Length> FluorescenceAberrations { get; init; } = [];

    [DataMember]
    public Dictionary<string, Length> ReflectionAberrations { get; init; } = [];
}
