using System.Runtime.Serialization;
using Betrian.Models.Serialization;
using UnitsNet;

namespace CFLMnavi.Configuration.Application;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record StageTiltingFunctions
{
    [DataMember]
    public Angle ManualTiltStep { get; init; } = Angle.FromDegrees(0.1);
}
