using System.Runtime.Serialization;
using Betrian.Models.Serialization;
using CFLMnavi.Configuration.Alignments;

namespace CFLMnavi.Configuration.Devices;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record Microscope
{
    [DataMember]
    public required InstrumentAlignment Alignment { get; init; }

    [DataMember]
    public FilterHandler FilterHandler { get; init; } = new();

    [DataMember]
    public ThermofisherInstrument ThermofisherInstrument { get; init; } = new();

    [DataMember]
    public BaslerCamera BaslerCamera { get; init; } = new();

    [DataMember]
    public Thorlabs4100 Thorlabs4100 { get; init; } = new();

    [DataMember]
    public LinearStageConfiguration LinearStage { get; init; } = new();
}
