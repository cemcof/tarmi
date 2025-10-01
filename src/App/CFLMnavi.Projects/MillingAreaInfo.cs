using System.Runtime.Serialization;

using Betrian.Models;
using Betrian.Models.Serialization;

namespace CFLMnavi.Projects;

[DataContract(Namespace = Helpers.AppNamespace)]
public record MillingAreaInfo
{
    [DataMember]
    public string Name { get; set; } = string.Empty;

    [DataMember]
    public RatioRectangle Definition { get; init; } = RatioRectangle.Zero;

}
