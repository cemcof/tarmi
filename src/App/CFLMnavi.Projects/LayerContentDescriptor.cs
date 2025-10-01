using System.Runtime.Serialization;
using Betrian.Models.Serialization;

namespace CFLMnavi.Projects;

[DataContract(Namespace = Helpers.AppNamespace)]
[KnownType(typeof(LayerContentDescriptor))]
[KnownType(typeof(LayerContentDescriptorWithCorrelationInfo))]
public record LayerContentDescriptor
{
    [DataMember]
    public required string? SubDirectory { get; init; }

    [DataMember]
    public required string Filename { get; init; }

    [DataMember]
    public required Guid Id { get; init; }

    [IgnoreDataMember]
    public string FilePath => SubDirectory is not null ? Path.Combine(SubDirectory, Filename) : Filename;
}

[DataContract(Namespace = Helpers.AppNamespace)]
public record LayerContentDescriptorWithCorrelationInfo : LayerContentDescriptor
{
    [DataMember]
    public CorrelationInfo CorrelationInfo { get; init; } = new();

    [DataMember]
    public List<MillingAreaInfo> MillingAreas { get; init; } = [];
}
