namespace Tarmi.Imaging.Common.Metadata.Thermofisher.IniFormat;

public class VacuumSection
{
    public double ChPressure { get; set; }
    public string Gas { get; set; } = string.Empty;
    public string UserMode { get; set; } = "High vacuum";
    public string Humidity { get; set; } = string.Empty; // ??
}
