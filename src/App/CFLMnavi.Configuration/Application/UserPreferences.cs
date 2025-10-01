using System.Runtime.Serialization;
using Betrian.Models.Serialization;
using UnitsNet;

namespace CFLMnavi.Configuration.Application;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record UserPreferences
{
    [DataMember]
    public required ImageColoring ImageColoring { get; init; }

    [DataMember]
    public required LinearStageFocus LinearStageFocus { get; init; }

    [DataMember]
    public required Algorithms Algorithms { get; init; }

    [DataMember]
    public required string ProjectsDirectory { get; init; }

    [DataMember]
    public required LuminescenceAberrations LuminescenceAberrations { get; init; }

    [DataMember]
    public required Duration LuminescenceFilterSwitchDelay { get; init; }
}
