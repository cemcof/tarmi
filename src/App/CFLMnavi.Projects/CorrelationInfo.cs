using System.Runtime.Serialization;
using Betrian.Models.Serialization;
using UnitsNet;

namespace CFLMnavi.Projects;

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
