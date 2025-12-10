using System.Runtime.Serialization;
using Tarmi.Models.Serialization;

namespace Tarmi.Projects;

[DataContract(Namespace = Helpers.AppNamespace)]
public record TileSet3DDescriptor : LayerDescriptor
{
    [DataMember]
    public required LayerContentDescriptorWithCorrelationInfo StitchedImage { get; init; }

    [DataMember]
    public required LayerContentDescriptor StitchedImageThumbnail { get; init; }

    [DataMember]
    public required TileSetOptions GrabbingOptions { get; init; }

    [DataMember]
    public required ZStackOptions ZStackOptions { get; init; }

    [DataMember]
    public required List<ZStackDescriptor> Images { get; init; } = [];

    [IgnoreDataMember]
    public override int ImagesCount => Images.Sum(i => i.ImagesCount);

    [DataMember]
    public CorrelationInfo CorrelationInfo { get; init; } = new();

    [DataMember]
    public Guid? LinkId { get; set; }
}
