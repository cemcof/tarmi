using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using UnitsNet;

namespace Tarmi.Configuration.Application;

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

    [DataMember]
    public required LuminescenceAberrations ConfocalAberrations { get; init; }

    [DataMember]
    public required Duration ConfocalFilterSwitchDelay { get; init; }

    [DataMember]
    public required ConfocalSettings ConfocalSettings { get; init; }

    [DataMember]
    public required PythonConfig PythonConfig { get; init; }
}
