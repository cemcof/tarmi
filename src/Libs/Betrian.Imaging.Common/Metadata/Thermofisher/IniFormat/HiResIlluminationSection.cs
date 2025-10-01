namespace Betrian.Imaging.Common.Metadata.Thermofisher.IniFormat;

public class HiResIlluminationSection
{
    public bool BrightFieldIsOn { get; set; }
    public string BrightFieldValue { get; set; } = string.Empty;
    public bool DarkFieldIsOn { get; set; }
    public string DarkFieldValue { get; set; } = string.Empty;
}
