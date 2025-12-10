using System.Runtime.Serialization;

using Tarmi.Models;
using Tarmi.Models.Serialization;

namespace Tarmi.Projects;

[DataContract(Namespace = Helpers.AppNamespace)]
public record MillingAreaInfo
{
    [DataMember]
    public string Name { get; set; } = string.Empty;

    [DataMember]
    public RatioRectangle Definition { get; init; } = RatioRectangle.Zero;

}
