using Tarmi.Serializers.Ini;

namespace Tarmi.Imaging.Common.Metadata.Thermofisher.IniFormat;

public class ImageSection
{
    public int DigitalContrast { get; set; }
    public int DigitalBrightness { get; set; }
    public int DigitalGamma { get; set; }
    public int Average { get; set; }
    public int Integrate { get; set; }
    public int ResolutionX { get; set; }
    public int ResolutionY { get; set; }
    [IniBoolValueFormatter("On", "Off")]
    public bool DriftCorrected { get; set; }
    public double ZoomFactor { get; set; } = 1.0;
    public double ZoomPanX { get; set; } // might be empty
    public double ZoomPanY { get; set; } // might be empty
    public int MagCanvasRealWidth { get; set; }
    public string MagnificationMode { get; set; } = string.Empty;
    public int ScreenMagCanvasRealWidth { get; set; }
    public string ScreenMagnificationMode { get; set; } = string.Empty;
    public string PostProcessing { get; set; } = string.Empty; // ??
    public string Transformation { get; set; } = string.Empty; // ??
}
