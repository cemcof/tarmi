using System.Runtime.Serialization;
using Betrian.Models.Serialization;

namespace CFLMnavi.Projects;

[DataContract(Namespace = Helpers.AppNamespace)]
public record TileSetDescriptor : LayerDescriptor
{
    [DataMember]
    public required List<LayerContentDescriptor> Images { get; init; } = [];

    [DataMember]
    public required LayerContentDescriptorWithCorrelationInfo StitchedImage { get; init; }

    [DataMember]
    public required LayerContentDescriptor StitchedImageThumbnail { get; init; }

    [DataMember]
    public required TileSetOptions GrabbingOptions { get; init; }

    [DataMember]
    public CorrelationInfo CorrelationInfo { get; init; } = new();

    [IgnoreDataMember]
    public override int ImagesCount => Images.Count;
}
