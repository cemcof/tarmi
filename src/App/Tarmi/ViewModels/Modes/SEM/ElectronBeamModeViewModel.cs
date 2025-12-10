using System.Runtime.CompilerServices;
using Tarmi.App.Infrastructure;
using Tarmi.Imaging.Common;
using Tarmi.Models;
using Tarmi.Configuration;
using Tarmi.ImagePipeline.Pipelines;
using Tarmi.Projects;
using Tarmi.VirtualDevices;
using UnitsNet;
using Microsoft.Extensions.Logging;
using Tarmi.App.WPF;
using Tarmi.App.Services.Application;
using Tarmi.App.ViewModels.ROIs;
using Tarmi.App.ViewModels.FocusPoints;

namespace Tarmi.App.ViewModels.Modes.SEM;

public partial class ElectronBeamModeViewModel : BeamModeViewModelBase
{
    protected override string ModeName => "SEM";
    protected override StageCameraView CameraView => StageCameraView.SEM;
    private readonly IElectronBeamMode _electronBeamMode;
    // TODO: move to configuration
    private readonly Angle DefaultBeamRotation = Angle.FromDegrees(180);

    public ElectronBeamModeViewModel(
        IElectronBeamMode electronBeamMode,
        IWindowService windowService,
        IProjectManager projectManager,
        ILoggerFactory loggerFactory,
        IStageNavigation stageNavigation,
        ISafeStageControlling safeStageControlling,
        ApplicationConfig applicationConfig,
        ILimits limits,
        OverviewImageViewModel overviewImageViewModel,
        RoiControlViewModel roiControlViewModel,
        FocusPointControlViewModel focusPointControlViewModel
    )
        : base(
            loggerFactory.CreateLogger<ElectronBeamModeViewModel>(),
            electronBeamMode,
            windowService,
            projectManager,
            new SemImagingPipeline(
                loggerFactory.CreateLogger<SemImagingPipeline>(),
                electronBeamMode,
                projectManager,
                stageNavigation
            ),
            stageNavigation,
            safeStageControlling,
            applicationConfig,
            limits,
            overviewImageViewModel,
            roiControlViewModel,
            focusPointControlViewModel
        )
    {
        _electronBeamMode = electronBeamMode;
    }

    protected override string CreateActivityName([CallerMemberName] string methodName = "")
        => $"{nameof(ElectronBeamModeViewModel)}::{methodName}";

    protected override async Task InitializeInternalAsync(ApplicationMode prevMode, CancellationToken cancellationToken)
    {
        await base.InitializeInternalAsync(prevMode, cancellationToken);
        _electronBeamMode.SetBeamRotation(DefaultBeamRotation);
    }


    public async Task RestoreImageState(ImageMetadata imageMetadata)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        if (
            imageMetadata.GetSource() != StageCameraView.SEM ||
            imageMetadata.FeiXmlMetadata == null ||
            imageMetadata.FeiXmlMetadata.Optics == null
        )
        {
            return;
        }

        await _electronBeamMode.RestoreImageState(imageMetadata, default);
    }
}

