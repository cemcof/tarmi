using Tarmi.Devices.Thermofisher.Instrument.Types;
using Tarmi.Imaging.Common;
using Tarmi.Models;
using UnitsNet;

namespace Tarmi.Devices.Thermofisher.Instrument;

public interface IInstrument : IDisposable
{
    IObservable<bool> IsConnected { get; }
    IObservable<InstrumentMode> Mode { get; }
    IObservable<ImageWithMetadata> ImageStream { get; }
    ChamberState CurrentChamberState { get; }
    IObservable<ChamberState> Chamber { get; }
    StageState CurrentStageState { get; }
    IObservable<StageState> Stage { get; }
    BeamState CurrentBeamState { get; }
    IObservable<BeamState> Beam { get; }
    IObservable<DetectorState> Detector { get; }
    DetectorState CurrentDetectorState { get; }
    IObservable<ImageFilterState> ImageFilter { get; }
    ImageFilterState CurrentImageFilterState { get; }

    InstrumentMode ActiveMode { get; }
    Task SwitchMode(InstrumentMode mode);

    ImageWithMetadata GrabImage();
    void StartImageStream(CancellationToken cancellationToken);

    StageLimits GetStageLimits();
    Task StageMove(StagePosition axesPositions);
    Task StageMoveBy(StagePosition axesOffsets);
    Task StageStopMoving();

    void SetBeamCurrentIndex(int currentIndex);
    Length GetBeamFreeWorkingDistance();
    void SetBeamFreeWorkingDistance(Length value);

    [Obsolete("Use ILimits")]
    LengthRangeDescriptorWithStep GetBeamFreeWorkingDistanceRange();

    Length GetHorizontalFieldWidth();
    void SetHorizontalFieldWidth(Length hfw);
    LengthRangeDescriptor GetHorizontalFieldWidthRange();

    Task AutoFocus(CancellationToken cancellationToken);
    Task AutoStigmation(CancellationToken cancellationToken);
    Task AutoContrastBrightness(CancellationToken cancellationToken);

    void SetDwellTime(Duration dwellTime);
    void SetBeamOn(bool beamOn);
    void SetBeamBlank(bool beamBlank);
    void SetResolution(Resolution resolution);

    void ClearMillingDefinitions();
    void AddMillingDefinition(RatioRectangle rectangle);

    void SetReducedArea(BeamCoordinatesRectangle rectangle, Duration dwellTime, ImageFilterType imageFilterType, int frames, int lineIntegration);
    void SetFullFrameMode(Duration dwellTime, ImageFilterType imageFilterType, int frames, int lineIntegration);
    void SetBeamRotation(Angle rotation);
}
