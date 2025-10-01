namespace Betrian.Imaging.Common.Metadata.Thermofisher.IniFormat;

public class CbsDetectorSection
{
    public double Contrast { get; set; }
    public double Brightness { get; set; }
    public string Signal { get; set; } = "BSE";
    public int Mix { get; set; }
    public double ContrastDB { get; set; }
    public double BrightnessDB { get; set; }
    public string Setting { get; set; } = "A+B+C+D";
    public double MinimumDwellTime { get; set; }
}
