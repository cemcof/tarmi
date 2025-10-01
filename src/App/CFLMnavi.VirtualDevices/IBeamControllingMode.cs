using UnitsNet;

namespace CFLMnavi.VirtualDevices;

public interface IBeamControllingMode
{
    ElectricCurrent[] AvailableBeamCurrents { get; }
    void SetBeamCurrentIndex(int currentIndex);
    void SetBeamRotation(Angle rotation);
}
