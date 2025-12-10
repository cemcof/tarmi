namespace Tarmi.Imaging.Common.Metadata.Thermofisher.IniFormat;

public class IcdDetectorSection
{
    public double Contrast { get; set; }
    public double Brightness { get; set; }
    public string Signal { get; set; } = "SE";
    public double ContrastDB { get; set; }
    public double BrightnessDB { get; set; }
    public double MinimumDwellTime { get; set; }
}
