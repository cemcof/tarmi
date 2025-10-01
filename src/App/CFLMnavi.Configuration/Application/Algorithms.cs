using System.Runtime.Serialization;
using Betrian.Models.Serialization;

namespace CFLMnavi.Configuration.Application;

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
