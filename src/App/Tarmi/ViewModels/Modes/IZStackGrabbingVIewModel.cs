using Tarmi.Models;
using Tarmi.Projects;
using CommunityToolkit.Mvvm.Input;
using UnitsNet;

namespace Tarmi.App.ViewModels.Modes;

public interface IZStackGrabbingViewModel : IDisposable
{
    bool CanAcquireZStack { get; }
    double StepInMicrometers { get; }
    double StartPositionInMicrometers { get; set; }
    RangeDescriptor<Length> ZLimits { get; }

    double EndPositionInMicrometers { get; set; }

    double RangeInMicrometers { get; set; }


    Length LinearStagePosition { get; set; }

    int NumberOfSteps { get; set; }

    double StepSizeInMicrometers { get; set; }

    ZStackStepSetting ZStackStepSetting { get; set; }
    IRelayCommand<ZStackStepSetting> SelectStepSettingCommand { get; }
    IAsyncRelayCommand AcquireZStackCommand { get; }
    IRelayCommand CopyCurrentPositionToClipboardCommand { get; }

    IRelayCommand UseCurrentPositionAsStartPositionCommand { get; }
    IRelayCommand UseCurrentPositionAsEndPositionCommand { get; }
    ZStackOptions GetZStackOptions();
}
