namespace Tarmi.Imaging.Common.Metadata.Thermofisher.IniFormat;

public class ScanSection
{
    public bool InternalScan { get; set; } = true;
    public double Dwelltime { get; set; }
    public double PixelWidth { get; set; }
    public double PixelHeight { get; set; }
    public double HorFieldsize { get; set; }
    public double VerFieldsize { get; set; }
    public int Average { get; set; }
    public int Integrate { get; set; }
    public double FrameTime { get; set; }
}
