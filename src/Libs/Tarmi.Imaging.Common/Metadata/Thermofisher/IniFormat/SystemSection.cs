namespace Tarmi.Imaging.Common.Metadata.Thermofisher.IniFormat;

public class SystemSection
{
    public string Type { get; set; } = "DualBeam";
    public string Dnumber { get; set; } = string.Empty;
    public Version Software { get; set; } = new();
    public ushort BuildNr { get; set; }
    public string Source { get; set; } = "FEG";
    public string Column { get; set; } = "Elstar";
    public string FinalLens { get; set; } = "Elstar";
    public string Chamber { get; set; } = "xT-SDB";
    public string Stage { get; set; } = "110 x 110";
    public string Pump { get; set; } = "TMP";
    public string ESEM { get; set; } = "no";
    public string Aperture { get; set; } = "AVA";
    public string Scan { get; set; } = "PIA 4";
    public string Acq { get; set; } = "PIA 4";
    public double EucWD { get; set; }
    public string SystemType { get; set; } = string.Empty;
    public double DisplayWidth { get; set; }
    public double DisplayHeight { get; set; }
}
