using Tarmi.Serializers.Ini;

namespace Tarmi.Imaging.Common.Metadata.Thermofisher.IniFormat;

public record class Metadata
{
    [IniSection("User")]
    public UserSection User { get; init; } = new();

    [IniSection("System")]
    public SystemSection System { get; init; } = new();
    
    [IniSection("Beam")]
    public BeamSection Beam { get; init; } = new();

    [IniSection("EBeam")]
    public NamedBeamSection? EBeam { get; init; }

    [IniSection("IBeam")]
    public NamedBeamSection? IBeam { get; init; }

    [IniSection("GIS")]
    public GisSection Gis { get; init; } = new();

    [IniSection("Scan")]
    public ScanSection Scan { get; init; } = new();

    [IniSection("EScan")]
    public NamedScanSection? EScan { get; init; }

    [IniSection("IScan")]
    public NamedScanSection? IScan { get; init; }

    [IniSection("Stage")]
    public StageSection Stage { get; init; } = new();

    [IniSection("Image")]
    public ImageSection Image { get; init; } = new();

    [IniSection("Vacuum")]
    public VacuumSection Vacuum { get; init; } = new();

    [IniSection("Specimen")]
    public SpecimenSection Specimen { get; init; } = new();

    [IniSection("Detectors")]
    public DetectorsSection Detectors { get; init; } = new();

    [IniSection("TLD")] // conditional section
    public TldDetectorSection TldDetector { get; init; } = new();

    [IniSection("ETD")] // conditional section
    public EtdDetectorSection EtdDetector { get; init; } = new();

    [IniSection("ICE")] // conditional section
    public IceDetectorSection IceDetector { get; init; } = new();

    [IniSection("ICD")] // conditional section
    public IcdDetectorSection IcdDetector { get; init; } = new();

    [IniSection("CBS")] // conditional section
    public CbsDetectorSection CbsDetector { get; init; } = new();

    [IniSection("Mix")] // conditional section
    public MixDetectorSection MixDetector { get; init; } = new();

    [IniSection("Accessories")]
    public AccessoriesSection Accessories { get; init; } = new();

    [IniSection("PrivateFei")]
    public PrivateFeiSection PrivateFei { get; init; } = new();

    [IniSection("HiResIllumination")]
    public HiResIlluminationSection HiResIllumination { get; init; } = new();

    [IniSection("HotStageMEMS")]
    public HotStageMemsSection HotStageMems { get; init; } = new();

    [IniSection("HotStage")]
    public HotStageSection HotStage { get; init; } = new();

    [IniSection("HotStageHVHS")]
    public HotStageHvhsSection HotStageHvhs { get; init; } = new();

    [IniSection("ColdStage")]
    public ColdStageSection ColdStage { get; init; } = new();
}
