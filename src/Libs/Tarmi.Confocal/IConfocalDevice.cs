using Tarmi.Imaging.Common.Metadata.Confocal;
using Tarmi.Models;
using UnitsNet;

namespace Tarmi.Confocal;

public interface IConfocalDevice
{
    LuminescenceMode LuminescenceMode { get; set; }
    Length LaserColor { get; set; }
    Ratio Intensity { get; set; }
    //RangeDescriptor<Ratio> IntensityRange { get; }
    Duration Dwell { get; set; }
    Length PinHolePosition { get; set; }
    Length FilterPosition { get; set; }
    int Width { get; set; }
    int Height { get; set; }
    Length FieldWidth { get; set; }
    Length FieldHeight { get; set; }
    string Resolution { get; set; }
    Level Gain { get; set; }
    //RangeDescriptor<Level> GainRange { get; }
    ElectricPotential ADC { get; set; }
    //RangeDescriptor<ElectricPotential> ADCRange { get; }
    LengthPoint PixelSize { get; set; }
    //IEnumerable<Length> PixelSizes { get; }
}
