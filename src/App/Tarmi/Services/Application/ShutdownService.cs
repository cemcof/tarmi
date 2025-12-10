﻿using Tarmi.App.WPF;
using Microsoft.Extensions.Logging;
using Tarmi.App.ViewModels.Modes.FIB;
using Tarmi.App.ViewModels.Modes.SEM;
using Tarmi.App.ViewModels.Modes.LM;
using Tarmi.App.ViewModels.Modes.Confocal;

namespace Tarmi.App.Services.Application;

public interface IShutdownService
{
    Task Shutdown();
}

public class ShutdownService : IShutdownService
{
    private readonly ILogger _logger;
    private readonly IWindowService _windowService;
    private readonly IApplicationModeService _applicationModeService;
    private readonly ElectronBeamModeViewModel _electronBeamModeViewModel;
    private readonly LuminescenceModeViewModel _luminescenceModeViewModel;
    private readonly IonBeamModeViewModel _ionBeamModeViewModel;
    private readonly ConfocalModeViewModel _confocalModeViewModel;

    public ShutdownService(
        ILogger<ShutdownService> logger,
        IWindowService windowService,
        IApplicationModeService applicationModeService,
        ElectronBeamModeViewModel electronBeamModeViewModel,
        LuminescenceModeViewModel luminescenceModeViewModel,
        IonBeamModeViewModel ionBeamModeViewModel,
        ConfocalModeViewModel confocalModeViewModel)
    {
        _logger = logger;
        _windowService = windowService;
        _applicationModeService = applicationModeService;
        _electronBeamModeViewModel = electronBeamModeViewModel;
        _luminescenceModeViewModel = luminescenceModeViewModel;
        _ionBeamModeViewModel = ionBeamModeViewModel;
        _confocalModeViewModel = confocalModeViewModel;
    }

    public async Task Shutdown()
    {
        ApplicationMode activeMode = _applicationModeService.GetCurrentMode();
        try
        {
            await _windowService.ShowIndeterminateWaitingDialogAsync($"Disabling {activeMode} mode", async progress =>
            {
                await DisableActiveMode(activeMode);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while disabling the active mode: {ActiveMode}", activeMode);
        }
        finally
        {
            System.Windows.Application.Current.Shutdown(0);
        }
    }

    private Task DisableActiveMode(ApplicationMode applicationMode) => applicationMode switch
    {
        ApplicationMode.FIB => _ionBeamModeViewModel.DeInitialize(ApplicationMode.Viewer),
        ApplicationMode.SEM => _electronBeamModeViewModel.DeInitialize(ApplicationMode.Viewer),
        ApplicationMode.LM => _luminescenceModeViewModel.DeInitialize(ApplicationMode.Viewer),
        ApplicationMode.Confocal => _confocalModeViewModel.DeInitialize(ApplicationMode.Viewer),
        _ => Task.CompletedTask
    };
}
