using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using UnitsNet;

namespace Tarmi.Configuration.Application;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record StageTiltingFunctions
{
    [DataMember]
    public Angle ManualTiltStep { get; init; } = Angle.FromDegrees(0.1);
}
