using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using Betrian.CflmNavi.App.Services.Application;
using Betrian.WPF;
using CFLMnavi.Projects;
using CFLMnavi.WPF;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Betrian.CflmNavi.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IApplicationModeService _applicationModeService;
    private readonly IWindowService _windowService;
    private readonly IProjectManager _projectManager;
    private readonly IShutdownService _shutdownService;
    private readonly IStartupService _startupService;

    [ObservableProperty]
    public partial ApplicationMode Mode { get; set; }

    [ObservableProperty]
    public partial bool InputDisabled { get; private set; }

    [ObservableProperty]
    public partial bool InputDisabledWithMessage { get; private set; }

    [ObservableProperty]
    public partial string? BusyMessage { get; private set; }

    [ObservableProperty]
    public partial string ProjectName { get; private set; }

    [ObservableProperty]
    public partial string WindowTitle { get; private set; }

    public MainWindowViewModel(IApplicationModeService applicationModeService, IWindowService windowService, IProjectManager projectManager, IShutdownService shutdownService, IStartupService startupService)
    {
        _applicationModeService = applicationModeService;
        _windowService = windowService;
        if (_windowService is WindowService ws)
        {
            ws.SetMainViewModel(this);
        }
        _projectManager = projectManager;
        _shutdownService = shutdownService;
        _startupService = startupService;
        ProjectName = AppDomain.CurrentDomain.FriendlyName;
        WindowTitle = ProjectName;

        _disposables.Add(
            Observable
                .Interval(TimeSpan.FromSeconds(1))
                .Subscribe(_ =>
                {
                    try
                    {
                        var ws = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
                        var bytes = UnitsNet.Information.FromBytes(ws);
                        WindowTitle = $"{ProjectName} (Memory: {(int)bytes.Megabytes}Mb)";
                    }
                    catch
                    {
                    }
                })
        );
    }

    protected override async Task InitializeCoreAsync()
    {
        _disposables.Add(_applicationModeService.Mode.ObserveOnDispatcher().Subscribe(mode => Mode = mode));
        _disposables.Add(_projectManager.ActiveProject.ObserveOnDispatcher()
            .Where(activeProject => activeProject is null)
            .Subscribe(_ => OpenProjectOrCloseApplication()));
        _disposables.Add(_windowService.IsBusy.ObserveOnDispatcher().Subscribe(HandleIsBusy));
        _disposables.Add(_windowService.BusyMessage.ObserveOnDispatcher().Subscribe(HandleBusyMessage));
        await _startupService.PerformPreStartProcedure(default);

        App.Current.MainWindow.Closing += MainWindow_Closing;
        await base.InitializeCoreAsync();
    }

    internal void HandleBusyMessage(string message)
    {
        BusyMessage = message;
        InputDisabledWithMessage = InputDisabled && !string.IsNullOrWhiteSpace(BusyMessage);
    }

    internal void HandleIsBusy(bool isBusy)
    {
        InputDisabled = isBusy;
        InputDisabledWithMessage = InputDisabled && !string.IsNullOrWhiteSpace(BusyMessage);
        if (!isBusy)
        {
            BusyMessage = string.Empty;
        }
    }

    partial void OnInputDisabledChanged(bool value)
    {
        Mouse.OverrideCursor = value ? Cursors.Wait : Cursors.Arrow;
    }

    private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        await _shutdownService.Shutdown();
    }

    private void OpenProjectOrCloseApplication()
    {
        var projectDescriptor = _windowService.ShowProjectSelectionDialog();
        if (projectDescriptor is null)
        {
            // TODO: Is this valid MVVM approach?
            Application.Current.MainWindow.Close();
        }
        else
        {
            ProjectName = $"{AppDomain.CurrentDomain.FriendlyName} - {projectDescriptor.Name}";
            WindowTitle = ProjectName;
            _projectManager.OpenProject(projectDescriptor);
        }
    }

    partial void OnModeChanged(ApplicationMode value)
    {
        _applicationModeService.Mode.OnNext(value);
    }
}
