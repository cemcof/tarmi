using Tarmi.Models;

namespace Tarmi.VirtualDevices;

public interface ILimits
{
    AngleRangeDescriptorWithStep GetTiltRangeForView(StageCameraView view);
    AngleRangeDescriptorWithStep GetAutoTiltRangeForView(StageCameraView view);
    LengthRangeDescriptorWithStep GetFocusRangeForActiveBeam();
    LengthRangeDescriptorWithStep GetAutoFocusRangeForActiveBeam();
}
