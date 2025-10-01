namespace Betrian.Imaging.Common.Metadata.Thermofisher.IniFormat;

public class StageSection
{
    public double StageX { get; set; }
    public double StageY { get; set; }
    public double StageZ { get; set; }
    public double StageR { get; set; }
    public double StageT { get; set; }
    public double StageTb { get; set; }
    public double SpecTilt { get; set; }
    public double WorkingDistance { get; set; }
    public string ActiveStage { get; set; } = "Bulk";
    public double StageRawX { get; set; }
    public double StageRawY { get; set; }
    public double StageRawZ { get; set; }
    public double StageRawR { get; set; }
    public double StageRawT { get; set; }
    public double StageRawTb { get; set; }
}
