namespace Tarmi.Imaging.Common.Metadata.Thermofisher.IniFormat;

public class HotStageSection
{
    public string TargetTemperature { get; set; } = string.Empty;
    public string ActualTemperature { get; set; } = string.Empty;
    public string SampleBias { get; set; } = string.Empty;
    public string ShieldBias { get; set; } = string.Empty;
}
