namespace Tarmi.Imaging.Common.Metadata.Thermofisher.IniFormat;

public class HotStageMemsSection
{
    public string HeatingCurrent { get; set; } = string.Empty;
    public string HeatingVoltage { get; set; } = string.Empty;
    public string TargetTemperature { get; set; } = string.Empty;
    public string ActualTemperature { get; set; } = string.Empty;
    public string HeatingPower { get; set; } = string.Empty;
    public string SampleBias { get; set; } = string.Empty;
    public string SampleResistance  { get; set; } = string.Empty;
}
