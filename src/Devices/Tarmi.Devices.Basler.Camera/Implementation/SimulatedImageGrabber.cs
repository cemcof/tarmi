using Basler.Pylon;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Tarmi.Devices.Basler.Camera.Implementation;
internal sealed class SimulatedImageGrabber : ImageGrabber, ISimulatedImageGrabber
{
    public SimulatedImageGrabber(ILogger logger, ICameraInfo cameraInfo)
        : base(logger, cameraInfo)
    {
    }

    public SimulationImageMode SimulationMode
    {
        get
        {
            ThrowIfNotOpen();

            var testImgSelectorValue = _camera.Parameters[PLCamEmuCamera.TestImageSelector].GetValueOrDefault("Off");
            if (testImgSelectorValue.Equals("Testimage1", StringComparison.OrdinalIgnoreCase))
            {
                return SimulationImageMode.StaticPattern;
            }
            else if (testImgSelectorValue.Equals("Testimage2", StringComparison.OrdinalIgnoreCase))
            {
                return SimulationImageMode.DynamicPattern;
            }
            else
            {
                var fileModeValue = _camera.Parameters[PLCamEmuCamera.ImageFileMode].GetValue();
                if (fileModeValue.Equals("On", StringComparison.OrdinalIgnoreCase))
                {
                    return SimulationImageMode.File;
                }
            }
            return SimulationImageMode.Off;
        }
        set
        {
            ThrowIfNotOpen();
            ThrowIfGrabbingInProgress();

            if (value == SimulationImageMode.Off)
            {
                _camera.Parameters[PLCamEmuCamera.ImageFileMode].SetValue("Off");
                _camera.Parameters[PLCamEmuCamera.TestImageSelector].SetValue("Off");
            }
            else if (value == SimulationImageMode.StaticPattern)
            {
                _camera.Parameters[PLCamEmuCamera.ImageFileMode].SetValue("Off");
                _camera.Parameters[PLCamEmuCamera.TestImageSelector].SetValue("Testimage1");
            }
            else if (value == SimulationImageMode.DynamicPattern)
            {
                _camera.Parameters[PLCamEmuCamera.ImageFileMode].SetValue("Off");
                _camera.Parameters[PLCamEmuCamera.TestImageSelector].SetValue("Testimage2");
            }
            else
            {
                Guard.IsTrue(ImageFile.Exists, "Existing file path must be provided");
                _camera.Parameters[PLCamEmuCamera.TestImageSelector].SetValue("Off");
                _camera.Parameters[PLCamEmuCamera.ImageFilename].SetValue(ImageFile.FullName);
                _camera.Parameters[PLCamEmuCamera.ImageFileMode].SetValue("On");
            }
        }
    }

    public FileInfo ImageFile
    {
        get
        {
            ThrowIfNotOpen();
            var path = _camera.Parameters[PLCamEmuCamera.ImageFilename].GetValue();
            return new FileInfo(path);
        }
        set
        {
            Guard.IsTrue(value.Exists, "Existing file path must be provided");
            ThrowIfNotOpen();
            ThrowIfGrabbingInProgress();
            _camera.Parameters[PLCamEmuCamera.ImageFilename].SetValue(value.FullName);
        }
    }
}
