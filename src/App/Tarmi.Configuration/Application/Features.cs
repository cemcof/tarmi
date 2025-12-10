using System.Runtime.Serialization;
using Tarmi.Models.Serialization;

namespace Tarmi.Configuration.Application;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record Features
{
    [DataMember]
    public bool EnableConfocalMode {  get; init; } = false;
}
