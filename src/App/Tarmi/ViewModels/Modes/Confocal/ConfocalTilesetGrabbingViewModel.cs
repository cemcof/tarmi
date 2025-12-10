using Microsoft.Extensions.Logging;
using Tarmi.App.Services.Application;
using Tarmi.Configuration;
using Tarmi.Imaging.Common;
using Tarmi.Projects;
using Tarmi.VirtualDevices;
using UnitsNet;

namespace Tarmi.App.ViewModels.Modes.Confocal;

internal partial class ConfocalTilesetGrabbingViewModel : TileSetGrabbingViewModel
{
    public ConfocalTilesetGrabbingViewModel(
        ILogger logger,
        IWindowService windowService,
        IStageNavigation stageNavigation,
        IConfocalMode confocalMode,
        IProjectManager projectManager,
        IImagingPipelineGrabber pipelineGrabber,
        ISafeStageControlling safeStageControlling,
        TileSetGrabbingService tileSetGrabbingService,
        TileSet3DGrabbingService tileSet3DGrabbingService,
        ZStackGrabbingViewModel zStackGrabbingViewModel,
        ApplicationConfig applicationConfig,
        VirtualDeviceViewModel parent)
        : base(logger, windowService, stageNavigation, projectManager, pipelineGrabber, safeStageControlling, tileSetGrabbingService, tileSet3DGrabbingService, zStackGrabbingViewModel, applicationConfig, parent)
    {
        _confocalMode = confocalMode;
    }

    private readonly IConfocalMode _confocalMode;
    //private readonly ConfocalImagingViewModel _confocalImaging;

    protected override async Task TileSetAcquisitionImplementation(AcquisitionStrategy acquisitionStrategy, IProgress<(string, Ratio)> progress, CancellationToken cancellationToken)
    {
        var part = Ratio.FromDecimalFractions(1.0);
        var innerProgress = new Progress<(string Message, Ratio Percentage)>(inner => progress.Report((inner.Message, part.DecimalFractions * inner.Percentage)));

        // Set pinhole and filter wheel
        await _confocalMode.SetComponentsBeforeGrabbing();
        await base.TileSetAcquisitionImplementation(acquisitionStrategy, innerProgress, cancellationToken);

        // TODO: set back init state
    }
}
