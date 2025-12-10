using System.Runtime.Serialization;
using Tarmi.Models.Serialization;

namespace Tarmi.Projects;

[DataContract(Namespace = Helpers.AppNamespace)]
public record LayeredImageDescriptor : LayerDescriptor
{
    [DataMember]
    public List<LayerContentDescriptorWithCorrelationInfo> Images { get; init; } = [];

    [IgnoreDataMember]
    public override int ImagesCount => Images.Count;
}
