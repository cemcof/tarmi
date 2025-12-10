using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using UnitsNet;

namespace Tarmi.Configuration.Application;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record AutoEqualize
{
    [DataMember]
    public Ratio Min { get; init; } = Ratio.Zero;

    [DataMember] 
    public Ratio Max { get; init; } = Ratio.Zero;
}
