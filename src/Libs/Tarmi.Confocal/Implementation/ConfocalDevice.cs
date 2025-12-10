using Tarmi.Imaging.Common.Metadata.Confocal;
using Tarmi.Models;
using UnitsNet;

namespace Tarmi.Confocal.Implementation;

public sealed record ConfocalDevice : IConfocalDevice
{
    public LuminescenceMode LuminescenceMode { get; set; }
    public Length LaserColor { get; set; }
    public Ratio Intensity { get; set; }
    public Duration Dwell { get; set; }
    public Length PinHolePosition { get; set; }
    public Length FilterPosition { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public Length FieldWidth { get; set; }
    public Length FieldHeight { get; set; }
    public string Resolution { get; set; } = string.Empty;
    public Level Gain { get; set; }
    public ElectricPotential ADC { get; set; } = ElectricPotential.Zero;
    public LengthPoint PixelSize { get; set; } = LengthPoint.Zero;
}
