using System.Runtime.Serialization;
using Betrian.Models;
using UnitsNet;

namespace CFLMnavi.Projects;

[DataContract]
public record FiducialPoint
{
    [DataMember]
    public required Guid Id { get; init; }
    
    [DataMember]
    public required LengthPoint Position { get; init; }

    public static FiducialPoint FromPoint(LengthPoint point, Guid id)
    {
        return new()
        {
            Id = id,
            Position = point
        };
    }
}
