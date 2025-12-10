using OpenCvSharp;

namespace Tarmi.Devices.Basler.Camera;

public enum AutoGainMode
{
    Off,
    Once,
    Continuous
}

public enum ImagePixelFormat
{
    Unknown,
    Mono8,
    Mono12,
    // Not supported by acA3088-57um
    //Mono16,
    //Rgb8,
    //Bgr8,
    //Rgb16
}

public enum SimulationImageMode
{
    Off,
    StaticPattern,
    DynamicPattern,
    File
}

public enum ImageOrientation
{
    TopDown,
    BottomUp
}

public class ImageMetadata
{
    public ImageOrientation Orientation { get; init; }

    public double Gain { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public ImagePixelFormat PixelFormat { get; set; }

    public TimeSpan ExposureTime { get; set; }

    public DateTimeOffset Timestamp { get; init; }
}
