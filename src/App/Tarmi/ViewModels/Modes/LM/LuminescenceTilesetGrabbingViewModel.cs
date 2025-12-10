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

    protected override async Task TileSetAcquisitionImplementation(AcquisitionStrategy acquisitionStrategy, IProgress<(string, Ratio)> progress, CancellationToken cancellationToken)
    {
        var lightSettings = _luminescenceImaging.LightSettings
            .Where(settings => settings.IsSelected)
            .ToArray();

        // Acquisition with current light setting.
        if (lightSettings is [])
        {
            await base.TileSetAcquisitionImplementation(acquisitionStrategy, progress, cancellationToken);
            return;
        }

        var initialColor = _luminescenceMode.ActiveLightColor;
        var initialExposure = _luminescenceMode.ExposureTime;
        var initialIntensity = _luminescenceMode.Intensity;

        var part = Ratio.FromDecimalFractions(1.0 / lightSettings.Length);
        for (var i = 0; i < lightSettings.Length; i++)
        {
            var innerProgress = new Progress<(string Message, Ratio Percentage)>(inner => progress.Report((inner.Message, i * part + part.DecimalFractions * inner.Percentage)));
            var lightSetting = lightSettings[i];

            await _luminescenceMode.TurnLightOn(lightSetting.Color, cancellationToken);
            var percent = Ratio.FromPercent(lightSetting.ImagingSettings.Intensity);
            await _luminescenceMode.SetIntensityAsync(percent, cancellationToken);
            _luminescenceMode.ExposureTime = Duration.FromMicroseconds(lightSetting.ImagingSettings.Exposure);

            await base.TileSetAcquisitionImplementation(acquisitionStrategy, innerProgress, cancellationToken);
        }

        await _luminescenceMode.TurnLightOff(cancellationToken);
        await _luminescenceMode.SetIntensityAsync(initialIntensity, cancellationToken);
        if (initialColor.HasValue)
        {
            await _luminescenceMode.TurnLightOn(initialColor.Value, cancellationToken);
        }
        _luminescenceMode.ExposureTime = initialExposure;
    }
}
