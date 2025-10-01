using System.Runtime.Serialization;
using UnitsNet;

namespace Betrian.Imaging.Common.Metadata.Luminescence;

[DataContract]
public record StackInfo
{
    [DataMember]
    public Length Step { get; init; }

    [DataMember]
    public int CurrentStep { get; init; }

    [DataMember]
    public int StepCount { get; init; }
}
