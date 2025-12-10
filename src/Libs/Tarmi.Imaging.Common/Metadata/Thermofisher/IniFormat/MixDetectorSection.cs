namespace Tarmi.Imaging.Common.Metadata.Thermofisher.IniFormat;

public class MixDetectorSection
{
    public int Number { get; set; }
    public string Detector1 { get; set; } = string.Empty;
    public string Detector1Mode { get; set; } = string.Empty;
    public double Factor1 { get; set; }
    public string Detector2 { get; set; } = string.Empty;
    public string Detector2Mode { get; set; } = string.Empty;
    public double Factor2 { get; set; }
    public string Detector3 { get; set; } = string.Empty;
    public string Detector3Mode { get; set; } = string.Empty;
    public double Factor3 { get; set; }
}
