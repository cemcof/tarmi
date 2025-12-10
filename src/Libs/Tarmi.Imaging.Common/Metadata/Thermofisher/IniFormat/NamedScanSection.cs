using Tarmi.Serializers.Ini;

namespace Tarmi.Imaging.Common.Metadata.Thermofisher.IniFormat;

public class NamedScanSection
{
    public string Scan { get; set; } = "PIA 4";
    public bool InternalScan { get; set; }
    public double Dwell { get; set; }
    public double PixelWidth { get; set; }
    public double PixelHeight { get; set; }
    public double HorFieldsize { get; set; }
    public double VerFieldsize { get; set; }
    public double FrameTime { get; set; }
    public double LineTime { get; set; }

    [IniBoolValueFormatter("On", "Off")]
    public bool Mainslock { get; set; } = true;
    public int LineIntegration { get; set; }
    public int ScanInterlacing { get; set; }
}
