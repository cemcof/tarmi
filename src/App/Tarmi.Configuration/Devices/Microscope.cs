using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using Tarmi.Configuration.Alignments;

namespace Tarmi.Configuration.Devices;

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

    [DataMember]
    public ThorlabsPinHoleWheel ThorlabsPinHoleWheel { get; init; } = new();

    [DataMember]
    public ThorlabsFilterWheel ThorlabsFilterWheel { get; init; } = new();

    [DataMember]
    public ConfocalStaff ConfocalConfig { get; init; } = new();
}
