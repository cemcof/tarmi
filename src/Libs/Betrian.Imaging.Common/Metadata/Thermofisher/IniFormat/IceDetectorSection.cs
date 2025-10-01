namespace Betrian.Imaging.Common.Metadata.Thermofisher.IniFormat;

public class IceDetectorSection
{
    public double Contrast { get; set; }
    public double Brightness { get; set; }
    public string Signal { get; set; } = "SE";
    public double ContrastDB { get; set; }
    public double GridVoltage { get; set; }
    public int ConverterVoltage { get; set; }
    public double MinimumDwellTime { get; set; }
}
