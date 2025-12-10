namespace Tarmi.Imaging.Common.Metadata.Thermofisher.IniFormat;

public class BeamSection
{
    public double HV { get; set; }
    public int Spot { get; set; }
    public double StigmatorX { get; set; }
    public double StigmatorY { get; set; }
    public double BeamShiftX { get; set; }
    public double BeamShiftY { get; set; }
    public double ScanRotation { get; set; }
    public string ImageMode { get; set; } = "Normal";
    public string FineStageBias { get; set; } = string.Empty;
    public string Beam { get; set; } = "EBeam";
    public string Scan { get; set; } = "EScan";
}
