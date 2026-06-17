using Tarmi.Imaging.Common;
using Tarmi.Models;
using UnitsNet;

namespace Tarmi.Confocal;

public interface IImageGrabber : IDisposable
{
    //IObservable<bool> Connected { get; }
    //void Open(TimeSpan timeout);
    //void Close();
    //Length Light { get; set; }
    //Level Gain { get; set; }
    //Ratio Intensity { get; set; }
    //int Width { get; set; }
    //int Height { get; set; }
    //Duration Dwell { get; set; }
    //ElectricPotential ADC { get; set; }
    //RangeDescriptor<ElectricPotential> ADCVoltRange { get; }
    string DefaultImagePath { get; }
    bool IsGrabbing { get; }
    Task<ImageWithMetadata> GrabImage(IConfocalDevice confocalDevice);
    Task StartContinuousGrabbing(IConfocalDevice confocalDevice);
    void StopContinuousGrabbing();
    IObservable<ImageWithMetadata> GrabbedImage { get; }
    string ImagePath { get; set; }
}

