using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using UnitsNet;

namespace Tarmi.Imaging.Common.Metadata.Luminescence;

[DataContract(Namespace = Helpers.AppNamespace)]
public record CameraParameters
{
    [DataMember]
    public Level Gain { get; init; }

    [DataMember]
    public double BlackLevel { get; init; }

    [DataMember]
    public double Gamma { get; init; }

    [DataMember]
    public BinningMode BinningMode { get; init; }

    [DataMember]
    public int Binning { get; init; }

    [DataMember]
    public Frequency FrameRate { get; init; }

    [DataMember]
    public Duration ExposureTime { get; init; }
}
