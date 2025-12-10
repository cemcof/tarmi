using System.Runtime.Serialization;
using UnitsNet;
using Tarmi.Models.Serialization;

namespace Tarmi.Configuration.Devices;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record BaslerCamera
{
    // "Basler acA3088-57um (23713667)"
    [DataMember]
    public string CameraName { get; init; } = "Basler Emulation (0815-0000)";

    [DataMember]
    public int Width { get; init; } = 4096;

    [DataMember]
    public int Height { get; init; } = 4096;

    [DataMember]
    public Length FieldWidth { get; init; } = Length.FromMicrometers(2.4 * 4096);

    [DataMember]
    public Length FieldHeight { get; init; } = Length.FromMicrometers(2.4 * 4096);

    [DataMember]
    public bool FlipImageOnX { get; init; } = true;

    [DataMember]
    public bool FlipImageOnY { get; init; } = false;
}
