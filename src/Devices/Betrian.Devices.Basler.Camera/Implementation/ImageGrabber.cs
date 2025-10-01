using System.Reactive.Linq;
using System.Reactive.Subjects;
using Basler.Pylon;
using Betrian.Imaging.Common;
using Betrian.Imaging.Common.Metadata.Luminescence;
using Betrian.Models;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace Betrian.Devices.Basler.Camera.Implementation;

internal class ImageGrabber : IImageGrabber
{
    protected readonly ILogger _logger;
    protected readonly ICamera _camera;
    private IStreamGrabber? _streamGrabber;
    private readonly Subject<ImageWithMetadata> _grabbedImageSubject = new();
    private readonly BehaviorSubject<bool> _connectedSubject = new(false);
    private IDisposable? _continuousGrabbingDisposable;
    private const double _valueChangeTolerance = 1e-4;

    public ImageGrabber(ILogger logger, ICameraInfo cameraInfo)
    {
        _logger = logger;
        _camera = new global::Basler.Pylon.Camera(cameraInfo);
        _camera.CameraOpened += (sender, args) => _connectedSubject.OnNext(true);
        _camera.ConnectionLost += (sender, args) => _connectedSubject.OnNext(false);
        _camera.CameraClosed += (sender, args) => _connectedSubject.OnNext(false);
    }

    protected bool IsOpen => _camera is { IsOpen: true };

    protected void ThrowIfNotOpen()
    {
        if (!IsOpen)
        {
            throw new InvalidOperationException("Camera is not open");
        }
    }

    protected void ThrowIfGrabbingInProgress()
    {
        if (_streamGrabber is { IsGrabbing: true })
        {
            throw new InvalidOperationException("Image grabbing in progress");
        }
    }

    public IObservable<bool> Connected => _connectedSubject.AsObservable().DistinctUntilChanged();

    public IObservable<ImageWithMetadata> GrabbedImage => _grabbedImageSubject.AsObservable();

    public int RawGain
    {
        get
        {
            ThrowIfNotOpen();
            return (int)_camera.Parameters[PLCamera.GainRaw].GetValue();
        }
        set
        {
            ThrowIfNotOpen();
            // can be changed during acquisition
            var parameter = _camera.Parameters[PLCamera.GainRaw];
            var val = Math.Clamp(value, parameter.GetMinimum(), parameter.GetMaximum());
            parameter.SetValue(val);
            _logger.LogDebug("Raw gain set to {value}", val);
        }
    }

    public RangeDescriptor<Level> GainRange => new()
    {
        Min = Level.FromDecibels(_camera.Parameters[PLCamera.Gain].GetMinimum()),
        Max = Level.FromDecibels(_camera.Parameters[PLCamera.Gain].GetMaximum())
    };

    public Level Gain
    {
        get
        {
            ThrowIfNotOpen();
            return Level.FromDecibels(_camera.Parameters[PLCamera.Gain].GetValue());
        }
        set
        {
            ThrowIfNotOpen();
            // can be changed during acquisition
            var parameter = _camera.Parameters[PLCamera.Gain];
            var val = Math.Clamp(value.Decibels, GainRange.Min.Decibels, GainRange.Max.Decibels);
            if (!Comparison.EqualsAbsolute(val, Gain.Value, _valueChangeTolerance))
            {
                parameter.SetValue(val);
                _logger.LogDebug("Gain set to {value}", val);
            }
        }
    }

    public AutoGainMode AutoGain
    {
        get
        {
            ThrowIfNotOpen();
            return _camera.Parameters[PLCamera.GainAuto].GetValue().ToAutoGainMode();
        }
        set
        {
            ThrowIfNotOpen();
            // can be changed during acquisition
            var val = value.ToGainAutoString();
            _camera.Parameters[PLCamera.GainAuto].SetValue(val);
            _logger.LogDebug("Auto gain set to {value}", val);
        }
    }

    public double BlackLevel
    {
        get
        {
            ThrowIfNotOpen();
            return _camera.Parameters[PLCamera.BlackLevel].GetValue();
        }
        set
        {
            ThrowIfNotOpen();
            // can be changed during acquisition
            var parameter = _camera.Parameters[PLCamera.BlackLevel];
            var val = Math.Clamp(value, parameter.GetMinimum(), parameter.GetMaximum());
            if (!Comparison.EqualsAbsolute(val, BlackLevel, _valueChangeTolerance))
            {
                parameter.SetValue(val);
                _logger.LogDebug("Black level set to {value}", val);
            }
        }
    }

    public RangeDescriptor<double> GammaRange => new()
    {
        Min = _camera.Parameters[PLCamera.Gamma].GetMinimum(),
        Max = _camera.Parameters[PLCamera.Gamma].GetMaximum()
    };

    public double Gamma
    {
        get
        {
            ThrowIfNotOpen();
            return _camera.Parameters[PLCamera.Gamma].GetValue();
        }
        set
        {
            ThrowIfNotOpen();
            // can be changed during acquisition
            var parameter = _camera.Parameters[PLCamera.Gamma];
            var val = Math.Clamp(value, GammaRange.Min, GammaRange.Max);
            if (!Comparison.EqualsAbsolute(val, Gamma, _valueChangeTolerance))
            {
                parameter.SetValue(val);
                _logger.LogDebug("Gamma set to {value}", val);
            }
        }
    }

    public BinningMode BinningMode
    {
        get
        {
            ThrowIfNotOpen();
            // take from X
            return _camera.Parameters[PLCamera.BinningHorizontalMode].GetValue().ToBinningMode();
        }
        set
        {
            ThrowIfNotOpen();
            // cannot be changed during acquisition
            ThrowIfGrabbingInProgress();

            // set to X and Y
            var binningModeValue = value.ToBinningModeString();
            _camera.Parameters[PLCamera.BinningHorizontalMode].SetValue(binningModeValue);
            _camera.Parameters[PLCamera.BinningVerticalMode].SetValue(binningModeValue);
            _logger.LogDebug("Binning mode set to {value}", binningModeValue);
        }
    }

    public int Binning
    {
        get
        {
            ThrowIfNotOpen();
            // take from X
            return (int)_camera.Parameters[PLCamera.BinningHorizontal].GetValue();
        }
        set
        {
            ThrowIfNotOpen();
            // cannot be changed during acquisition
            ThrowIfGrabbingInProgress();

            // set to X and Y
            var horizontalBinningParam = _camera.Parameters[PLCamera.BinningHorizontal];
            var verticalBinningParam = _camera.Parameters[PLCamera.BinningVertical];
            var horizontalVal = Math.Clamp(value, horizontalBinningParam.GetMinimum(), horizontalBinningParam.GetMaximum());
            var verticalVal = Math.Clamp(value, verticalBinningParam.GetMinimum(), verticalBinningParam.GetMaximum());
            horizontalBinningParam.SetValue(horizontalVal);
            verticalBinningParam.SetValue(verticalVal);
            _logger.LogDebug("Binning set to vertical: {verticalVal}, horizontal: {horizontalVal}", verticalVal, horizontalVal);
        }
    }

    public int Width
    {
        get
        {
            ThrowIfNotOpen();
            return (int)_camera.Parameters[PLCamera.Width].GetValue();
        }
        set
        {
            ThrowIfNotOpen();
            // cannot be changed during acquisition
            ThrowIfGrabbingInProgress();

            var parameter = _camera.Parameters[PLCamera.Width];
            var val = Math.Clamp(value, parameter.GetMinimum(), parameter.GetMaximum());
            parameter.SetValue(val);
            _logger.LogDebug("Width set to {value}", val);
        }
    }

    public int Height
    {
        get
        {
            ThrowIfNotOpen();
            return (int)_camera.Parameters[PLCamera.Height].GetValue();
        }
        set
        {
            ThrowIfNotOpen();
            // cannot be changed during acquisition
            ThrowIfGrabbingInProgress();

            var parameter = _camera.Parameters[PLCamera.Height];
            var val = Math.Clamp(value, parameter.GetMinimum(), parameter.GetMaximum());
            parameter.SetValue(val);
            _logger.LogDebug("Height set to {value}", val);

        }
    }

    public ImagePixelFormat PixelFormat
    {
        get
        {
            ThrowIfNotOpen();
            return _camera.Parameters[PLCamera.PixelFormat].GetValue().ToImagePixelFormat();
        }
        set
        {
            ThrowIfNotOpen();
            // cannot be changed during acquisition
            ThrowIfGrabbingInProgress();

            Guard.IsTrue(value.IsOneOf(ImagePixelFormat.Mono8, ImagePixelFormat.Mono12));
            var val = value.ToPixelTypeString();
            _camera.Parameters[PLCamera.PixelFormat].SetValue(val);
            _logger.LogDebug("Pixel format set to {value}", val);
        }
    }

    public Frequency FrameRate
    {
        get
        {
            ThrowIfNotOpen();
            return Frequency.FromHertz(_camera.Parameters[PLCamera.AcquisitionFrameRate].GetValue());
        }
        set
        {
            ThrowIfNotOpen();
            // can be changed during acquisition
            var parameter = _camera.Parameters[PLCamera.AcquisitionFrameRate];
            var val = Math.Clamp(value.Hertz, parameter.GetMinimum(), parameter.GetMaximum());
            parameter.SetValue(val);
            _logger.LogDebug("Frame rate set to {value}", val);
        }
    }

    public RangeDescriptor<Duration> ExposureTimeRange => new()
    {
        Min = Duration.FromMicroseconds(_camera.Parameters[PLCamera.ExposureTime].GetMinimum()),
        Max = Duration.FromMicroseconds(_camera.Parameters[PLCamera.ExposureTime].GetMaximum())
    };

    public Duration ExposureTime
    {
        get
        {
            ThrowIfNotOpen();
            return Duration.FromMicroseconds(_camera.Parameters[PLCamera.ExposureTime].GetValue());
        }
        set
        {
            ThrowIfNotOpen();
            // can be changed during acquisition
            var parameter = _camera.Parameters[PLCamera.ExposureTime];
            var val = Math.Clamp(value.Microseconds, ExposureTimeRange.Min.Microseconds, ExposureTimeRange.Max.Microseconds);
            if (!Comparison.EqualsAbsolute(val, ExposureTime.Value, _valueChangeTolerance))
            {
                parameter.SetValue(val);
                _logger.LogDebug("Exposure set to {value}", val);
            }
        }
    }

    public void Close()
    {
        if (!IsOpen)
        {
            throw new InvalidOperationException("Camera is not opened");
        }

        StopContinuousGrabbing();
        _streamGrabber = null;
        _camera.Close();
    }

    public void Open(TimeSpan timeout)
    {
        if (IsOpen)
        {
            throw new InvalidOperationException("Camera already open");
        }

        _camera.Parameters[PLCameraInstance.GrabCameraEvents].SetValue(true);
        _ = _camera.Open((int)timeout.TotalMilliseconds, TimeoutHandling.ThrowException);
        _streamGrabber = _camera.StreamGrabber;
    }

    public ImageWithMetadata GrabImage(TimeSpan timeout)
    {
        ThrowIfGrabbingInProgress();
        var result = _streamGrabber!.GrabOne((int)timeout.TotalMilliseconds, TimeoutHandling.ThrowException);
        return !result.GrabSucceeded || !result.IsValid
            ? throw new InvalidOperationException($"Grabbing of image failed with result {result.ErrorCode}: {result.ErrorDescription}")
            : result.ConvertToImageWithMetadata(_camera, isLiveStreamImage: false);
    }

    public void StartContinuousGrabbing()
    {
        ThrowIfGrabbingInProgress();

        _continuousGrabbingDisposable =
            Observable.FromEventPattern<ImageGrabbedEventArgs>(
                handler => _streamGrabber!.ImageGrabbed += handler,
                handler => _streamGrabber!.ImageGrabbed -= handler
            )
            .Subscribe(args =>
            {
                _logger.Swallow(() => _grabbedImageSubject.OnNext(args.EventArgs.GrabResult.ConvertToImageWithMetadata(_camera, isLiveStreamImage: true)));
            });

        Configuration.AcquireContinuous(_camera, null);
        _streamGrabber!.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
    }

    public void StopContinuousGrabbing()
    {
        ThrowIfNotOpen();

        _continuousGrabbingDisposable?.Dispose();
        _continuousGrabbingDisposable = null;
        if (_streamGrabber is { IsGrabbing: true })
        {
            _streamGrabber.Stop();
        }
    }

    public void Dispose()
    {
        _camera.Dispose();
        GC.SuppressFinalize(this);
    }
}
