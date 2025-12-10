using System.Runtime.Serialization;
using Tarmi.Models.Serialization;

namespace Tarmi.Configuration.Application;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record Algorithms
{
    [DataMember]
    public StageTiltingFunctions TiltingFunctions { get; init; } = new();

    [DataMember]
    public FocusFunctions FocusFunctions { get; init; } = new();

    [DataMember]
    public required TileSetPreferences TileSetPreferences { get; init; }

    [DataMember]
    public AutoEqualize AutoEqualize { get; init; } = new();
}
