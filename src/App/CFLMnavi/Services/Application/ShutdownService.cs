using Betrian.CflmNavi.App.ViewModels.Modes.FIB;
using Betrian.CflmNavi.App.ViewModels.Modes.LM;
using Betrian.CflmNavi.App.ViewModels.Modes.SEM;
using CFLMnavi.WPF;
using Microsoft.Extensions.Logging;

namespace Betrian.CflmNavi.App.Services.Application;

public interface IShutdownService
{
    Task Shutdown();
}

public class ShutdownService : IShutdownService
{
    private readonly ILogger _logger;
    private readonly IWindowService _windowService;
    private readonly IApplicationModeService _applicationModeService;
    private readonly ElectronBeamModeViewModel _electronBeamViewModel;
    private readonly LuminescenceModeViewModel _luminescenceModeViewModel;
    private readonly IonBeamModeViewModel _ionBeamModeViewModel;

    public ShutdownService(
        ILogger<ShutdownService> logger,
        IWindowService windowService,
        IApplicationModeService applicationModeService,
        ElectronBeamModeViewModel electronBeamModeViewModel,
        LuminescenceModeViewModel luminescenceModeViewModel,
        IonBeamModeViewModel ionBeamModeViewModel)
    {
        _logger = logger;
        _windowService = windowService;
        _applicationModeService = applicationModeService;
        _electronBeamViewModel = electronBeamModeViewModel;
        _luminescenceModeViewModel = luminescenceModeViewModel;
        _ionBeamModeViewModel = ionBeamModeViewModel;
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
            App.Current.Shutdown(0);
        }
    }

    private Task DisableActiveMode(ApplicationMode applicationMode) => applicationMode switch
    {
        ApplicationMode.FIB => _ionBeamModeViewModel.DeInitialize(ApplicationMode.Viewer),
        ApplicationMode.SEM => _electronBeamViewModel.DeInitialize(ApplicationMode.Viewer),
        ApplicationMode.LM => _luminescenceModeViewModel.DeInitialize(ApplicationMode.Viewer),
        _ => Task.CompletedTask
    };
}
