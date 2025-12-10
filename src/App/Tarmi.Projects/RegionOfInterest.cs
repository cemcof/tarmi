using System.Runtime.Serialization;
using Tarmi.Models;
using Tarmi.Models.Serialization;

namespace Tarmi.Projects;

[DataContract(Namespace = Helpers.AppNamespace)]
[KnownType(typeof(RegionOfInterest))]
[KnownType(typeof(GridCenterRegionOfInterest))]
public record RegionOfInterest
{
    [DataMember]
    public required string Name { get; set; }

    [DataMember]
    public required Guid Id { get; init; }

    [IgnoreDataMember]
    public virtual bool CanBeRenamed => true;

    [IgnoreDataMember]
    public virtual bool CanBeDeleted => true;

    [DataMember]
    public required LengthPoint Position { get; set; }

    [DataMember]
    public List<LayeredImageDescriptor> Images { get; init; } = [];

    [DataMember]
    public List<TileSetDescriptor> TileSets { get; init; } = [];

    [DataMember]
    public List<ZStackDescriptor> ZStacks { get; init; } = [];

    [DataMember]
    public List<TileSet3DDescriptor> TileSets3D { get; init; } = [];

    [DataMember]
    public List<FiducialDescriptor> Fiducials { get; init; } = [];
}

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record GridCenterRegionOfInterest : RegionOfInterest
{
    [IgnoreDataMember]
    public override bool CanBeRenamed => false;

    [IgnoreDataMember]
    public override bool CanBeDeleted => false;

    [DataMember]
    public required string GridName { get; init; }
}
