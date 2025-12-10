using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using UnitsNet;

namespace Tarmi.Configuration.Alignments;

[DataContract(Namespace = Helpers.AppNamespace)]
public record LinearStageAlignment
{
    [DataMember]
    public Acceleration Acceleration { get; init; }

    [DataMember]
    public Speed HighVelocity { get; init; }

    [DataMember]
    public Speed LowVelocity { get; init; }

    /// <summary>
    /// Grace offset telling we are at the demanded position.
    /// are we at the demanded position = pos =< demandedPos + PositionTolerance && pos >= demandedPos = PositionTolerance
    /// </summary>
    [DataMember]
    public Length PositionTolerance { get; init; }

    /// <summary>
    /// Position where to move on retract command
    /// </summary>
    [DataMember]
    public Length RetractPosition { get; init; }

    /// <summary>
    /// Position where to move on protract command
    /// </summary>
    [DataMember]
    public Length ProtractPosition { get; init; }

    /// <summary>
    /// Minimum position to allow move to when retracted.
    /// </summary>
    [DataMember]
    public Length FocusMinimum { get; init; }

    /// <summary>
    /// Maximum position to allow move to when retracted.
    /// </summary>
    [DataMember]
    public Length FocusMaximum { get; init; }

    /// <summary>
    /// used on manual focus? obsoleted by provided combo, most likely will be removed
    /// </summary>
    [DataMember]
    public Length FocusStep { get; init; }
}
