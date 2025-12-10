using System.Runtime.Serialization;
using UnitsNet;

namespace Tarmi.Imaging.Common.Metadata.Confocal;

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
