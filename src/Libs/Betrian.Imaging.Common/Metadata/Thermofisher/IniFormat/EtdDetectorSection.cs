namespace Betrian.Imaging.Common.Metadata.Thermofisher.IniFormat;

public class EtdDetectorSection
{
    public double Contrast { get; set; }
    public double Brightness { get; set; }
    public int Mix { get; set; }
    public string Signal { get; set; } = "SE";
    public int Grid { get; set; }
    public double ContrastDB { get; set; }
    public double BrightnessDB { get; set; }
    public int Setting { get; set; }
    public double MinimumDwellTime { get; set; }
}
