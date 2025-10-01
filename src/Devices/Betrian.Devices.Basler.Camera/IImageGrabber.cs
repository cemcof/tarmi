using Betrian.Imaging.Common;
using Betrian.Imaging.Common.Metadata.Luminescence;
using Betrian.Models;
using UnitsNet;

namespace Betrian.Devices.Basler.Camera;

public interface IImageGrabber : IDisposable
{
    IObservable<bool> Connected { get; }
    void Open(TimeSpan timeout);
    void Close();
    int RawGain { get; set; }
    Level Gain { get; set; }
    RangeDescriptor<Level> GainRange { get; }
    AutoGainMode AutoGain { get; set; }
    double BlackLevel { get; set; }
    double Gamma { get; set; }
    RangeDescriptor<double> GammaRange { get; }
    BinningMode BinningMode { get; set; }
    int Binning { get; set; }
    int Width { get; set; }
    int Height { get; set; }
    ImagePixelFormat PixelFormat { get; set; }
    Frequency FrameRate { get; set; }
    Duration ExposureTime { get; set; }
    RangeDescriptor<Duration> ExposureTimeRange { get; }
    ImageWithMetadata GrabImage(TimeSpan timeout);
    void StartContinuousGrabbing();
    void StopContinuousGrabbing();
    IObservable<ImageWithMetadata> GrabbedImage { get; }
}
