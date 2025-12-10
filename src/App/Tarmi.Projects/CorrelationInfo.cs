using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using UnitsNet;

namespace Tarmi.Projects;

[DataContract(Namespace = Helpers.AppNamespace)]
public record CorrelationInfo
{
    [DataMember]
    public bool IsReferenceImage { get; set; } = false;

    [DataMember]
    public Ratio Opacity { get; set; } = Ratio.FromDecimalFractions(0.5);

    [DataMember]
    public List<FiducialPoint> FiducialPoints { get; init; } = [];

    [DataMember]
    public Guid FiducialsGroupId { get; set; } = Guid.Empty;
}
