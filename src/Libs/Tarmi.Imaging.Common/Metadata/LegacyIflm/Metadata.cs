using UnitsNet;

namespace Tarmi.Imaging.Common.Metadata.LegacyIflm;

public record ZStackInfo
{
    public int StepsCount { get; init; }
    public int Step { get; init; }
    public Length StepDistance { get; init; }
    public Length SliceBaseDistance { get; init; }
}

public record Metadata
{
    public bool IsStackImage => ZStackInfo is not null;
    public ZStackInfo? ZStackInfo { get; init; }
    public int Binning { get; init; }
    public Level Gain { get; init; }
    public double Gamma { get; init; }
    public Duration ExposureTime { get; init; }
    public Angle Rotation { get; init; }
    public bool FlipImageUD { get; init; }
    public bool FlipImageLR { get; init; }
    public Ratio LightIntensity { get; init; }
    public Frequency LightFrequency { get; init; }
}
