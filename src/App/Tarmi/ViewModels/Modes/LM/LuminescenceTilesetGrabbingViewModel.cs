using Tarmi.Imaging.Common;
using Tarmi.Configuration;
using Tarmi.Projects;
using Tarmi.VirtualDevices;
using Tarmi.VirtualDevices.Implementation;
using Microsoft.Extensions.Logging;
using UnitsNet;
using Tarmi.App.Services.Application;

namespace Tarmi.App.ViewModels.Modes.LM;

internal partial class LuminescenceTilesetGrabbingViewModel : TileSetGrabbingViewModel
{
    public IDisposable disposable;
    public LuminescenceTilesetGrabbingViewModel(ILogger logger, IWindowService windowService, IStageNavigation stageNavigation, ILuminescenceMode luminescenceMode, IProjectManager projectManager, IImagingPipelineGrabber pipelineGrabber, ISafeStageControlling safeStageControlling, LuminescenceImagingViewModel luminescenceImaging, TileSetGrabbingService tileSetGrabbingService, TileSet3DGrabbingService tileSet3DGrabbingService, ZStackGrabbingViewModel zStackGrabbingVM, ApplicationConfig applicationConfig, VirtualDeviceViewModel parent)
        : base(logger, windowService, stageNavigation, projectManager, pipelineGrabber, safeStageControlling, tileSetGrabbingService, tileSet3DGrabbingService, zStackGrabbingVM, applicationConfig, parent)
    {
        _luminescenceMode = luminescenceMode;
        _luminescenceImaging = luminescenceImaging;
        TilesetGrabbingOptions = [.. Enum.GetValues<TilesetGrabbingOptions>()];

        disposable = _luminescenceImaging.CanAcquireData.Subscribe(UpdateCanAcquireTileset);
    }

    private void UpdateCanAcquireTileset(bool canAcquire)
    {
        CanAcquireTileset = canAcquire;
    }

    private readonly ILuminescenceMode _luminescenceMode;
    private readonly LuminescenceImagingViewModel _luminescenceImaging;

    protected override async Task TileSetReAcquisitionImplementation(TileSetDescriptor descriptor, IProgress<(string, Ratio)> progress, CancellationToken cancellationToken)
    {
        await _luminescenceMode.TurnLightOnAsync(cancellationToken);
        await base.TileSetReAcquisitionImplementation(descriptor, progress, cancellationToken);
        await _luminescenceMode.TurnLightOffAsync(default);
    }

    protected override async Task TileSetAcquisitionImplementation(AcquisitionStrategy acquisitionStrategy, IProgress<(string, Ratio)> progress, CancellationToken cancellationToken)
    {
        var selectedLights = _luminescenceImaging.GetSelectedLights().ToArray();
        // Acquisition with current light setting.
        if (selectedLights is [])
        {
            await _luminescenceMode.TurnLightOnAsync(cancellationToken);
            await base.TileSetAcquisitionImplementation(acquisitionStrategy, progress, cancellationToken);
            await _luminescenceMode.TurnLightOffAsync(default);
            return;
        }

        await _luminescenceMode.TurnLightOnAsync(cancellationToken);
        var initialColor = _luminescenceMode.SelectedLightColor;
        var part = Ratio.FromDecimalFractions(1.0 / selectedLights.Length);
        for (var i = 0; i < selectedLights.Length; i++)
        {
            var innerProgress = new Progress<(string Message, Ratio Percentage)>(inner => progress.Report((inner.Message, i * part + part.DecimalFractions * inner.Percentage)));
            var lightColor = selectedLights[i];
            await _luminescenceImaging.SelectLightColorAsync(lightColor);

            await base.TileSetAcquisitionImplementation(acquisitionStrategy, innerProgress, cancellationToken);
        }
        await _luminescenceMode.TurnLightOffAsync(default);
        if (initialColor.HasValue)
        {
            await _luminescenceImaging.SelectLightColorAsync(initialColor.Value);
        }
    }
}
