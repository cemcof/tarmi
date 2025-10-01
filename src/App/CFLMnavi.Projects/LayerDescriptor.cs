using System.Runtime.Serialization;
using Betrian.Models;
using Betrian.Models.Serialization;

namespace CFLMnavi.Projects;

[DataContract(Namespace = Helpers.AppNamespace)]
[KnownType(typeof(TileSetDescriptor))]
[KnownType(typeof(ZStackDescriptor))]
[KnownType(typeof(TileSet3DDescriptor))]
[KnownType(typeof(LayeredImageDescriptor))]
public abstract record LayerDescriptor
{
    [DataMember]
    public required Guid Id { get; init; }

    [DataMember]
    public required Guid RegionOfInterestId { get; init; }

    [DataMember]
    public required string Name { get; init; }

    [DataMember]
    public required StageCameraView Source { get; init; }

    [IgnoreDataMember]
    public abstract int ImagesCount { get; }
}
