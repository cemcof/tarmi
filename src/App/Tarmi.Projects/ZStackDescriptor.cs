using System.Runtime.Serialization;
using Tarmi.Models.Serialization;

namespace Tarmi.Projects;

[DataContract(Namespace = Helpers.AppNamespace)]
public record ZStackDescriptor : LayerDescriptor
{
    [DataMember]
    public required List<LayerContentDescriptor> Images { get; init; } = [];

    // Added
    [DataMember]
    public LayerContentDescriptor MipImage { get; set; } = new LayerContentDescriptor
    {
        Filename = string.Empty,
        SubDirectory = null,
        Id = Guid.Empty
    };

    [IgnoreDataMember]
    public override int ImagesCount => Images.Count;

    [DataMember]
    public CorrelationInfo CorrelationInfo { get; init; } = new();

    [DataMember]
    public Guid? LinkId { get; set; }
}
