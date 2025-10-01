namespace Betrian.Imaging.Common.Metadata.Thermofisher.IniFormat;

public class ColdStageSection
{
    public string TargetTemperature { get; set; } = string.Empty;
    public string ActualTemperature { get; set; } = string.Empty;
    public string Humidity { get; set; } = string.Empty;
    public string SampleBias { get; set; } = string.Empty;
}
