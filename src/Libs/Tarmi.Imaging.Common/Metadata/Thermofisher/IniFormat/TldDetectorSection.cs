namespace Tarmi.Imaging.Common.Metadata.Thermofisher.IniFormat;

public class TldDetectorSection
{
    public double Contrast { get; set; }
    public double Brightness { get; set; }
    public string Signal { get; set; } = "BSE";
    public double ContrastDB { get; set; }
    public double BrightnessDB { get; set; }
    public double SuctionTube { get; set; }
    public int Mirror { get; set; }
    public double MinimumDwellTime { get; set; }
}
