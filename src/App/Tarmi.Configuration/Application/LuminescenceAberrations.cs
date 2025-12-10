using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using UnitsNet;

namespace Tarmi.Configuration.Application;

[DataContract(Namespace = Helpers.AppNamespace)]
public class LuminescenceAberrations
{
    [DataMember]
    public Dictionary<string, Length> FluorescenceAberrations { get; init; } = [];

    [DataMember]
    public Dictionary<string, Length> ReflectionAberrations { get; init; } = [];
}
