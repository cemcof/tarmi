using Betrian.Models;

namespace CFLMnavi.VirtualDevices;

public interface ILimits
{
    AngleRangeDescriptorWithStep GetTiltRangeForView(StageCameraView view);
    AngleRangeDescriptorWithStep GetAutoTiltRangeForView(StageCameraView view);
    LengthRangeDescriptorWithStep GetFocusRangeForActiveBeam();
    LengthRangeDescriptorWithStep GetAutoFocusRangeForActiveBeam();
}
