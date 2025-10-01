namespace Betrian.Imaging.Common.Metadata.Thermofisher.IniFormat;

public class SpecimenSection
{
    public double Temperature { get; set; } // can be empty
    public double SpecimenCurrent { get; set; }
    public double CryoShieldTemperature { get; set; } // can be empty
    public double CryoStageTemperature { get; set; } // can be empty
}
