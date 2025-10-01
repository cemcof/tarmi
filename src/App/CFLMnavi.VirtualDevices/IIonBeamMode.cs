using Betrian.Devices.Thermofisher.Instrument.Types;
using Betrian.Models;
using UnitsNet;

namespace CFLMnavi.VirtualDevices;

public enum IonBeamViewMode
{
    RightAngle,
    Milling
}

public interface IIonBeamMode : IBeamMode
{
    IonBeamViewMode ViewMode { get; }
    Task SwitchViewModeAsync(IonBeamViewMode viewMode);
    void SetResolution(Resolution resolution);
    void SetWorkingDistance(Length value);
    void SetHorizontalFieldWidth(Length value);
    LengthRangeDescriptor GetHorizontalFieldWidthRange();
    void AddMillingDefinition(RatioRectangle rectangle);
    void ClearMillingDefinitions();
}
