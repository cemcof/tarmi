using System.Runtime.Serialization;
using Betrian.Models.Serialization;

namespace CFLMnavi.Projects;

[DataContract(Namespace = Helpers.AppNamespace)]
public record ZStackDescriptor : LayerDescriptor
{
    [DataMember]
    public required List<LayerContentDescriptor> Images { get; init; } = [];

    // Added
    [DataMember]
    public LayerContentDescriptor MipImage { get; set; } = new LayerContentDescriptor { Filename = "", SubDirectory = null, Id = Guid.Empty };

    [IgnoreDataMember]
    public override int ImagesCount => Images.Count;

    [DataMember]
    public CorrelationInfo CorrelationInfo { get; init; } = new();

    [DataMember]
    public Guid? LinkId { get; set; }
}
