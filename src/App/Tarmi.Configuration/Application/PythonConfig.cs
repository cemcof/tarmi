using System.Runtime.Serialization;
using Tarmi.Models.Serialization;

namespace Tarmi.Configuration.Application;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record PythonConfig
{
    [DataMember]
    public required string ExecutablePath = string.Empty;

    [DataMember]
    public required string ScriptPath = string.Empty;

    [DataMember]
    public required string ScriptTuningPath = string.Empty;
}
