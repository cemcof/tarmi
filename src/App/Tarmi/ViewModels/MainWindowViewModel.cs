using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Tarmi.App.Services.Application;
using Tarmi.App.WPF;
using Tarmi.Configuration;
using Tarmi.Projects;
using Tarmi.WPF;

namespace Tarmi.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IApplicationModeService _applicationModeService;
    private readonly IWindowService _windowService;
    private readonly IProjectManager _projectManager;
    private readonly IShutdownService _shutdownService;
    private readonly IStartupService _startupService;
    private readonly UnitsNet.Information _physicalMemory;

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

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

    public IList<ApplicationMode> ApplicationModes { get; }

    public MainWindowViewModel(
        IApplicationModeService applicationModeService,
        IWindowService windowService,
        IProjectManager projectManager,
        IShutdownService shutdownService,
        IStartupService startupService,
        ApplicationConfig applicationConfig
    )
    {
        _applicationModeService = applicationModeService;
        _windowService = windowService;
        _projectManager = projectManager;
        _shutdownService = shutdownService;
        _startupService = startupService;
        ProjectName = AppDomain.CurrentDomain.FriendlyName;
        WindowTitle = ProjectName;
        _ = GetPhysicallyInstalledSystemMemory(out var pm);
        _physicalMemory = UnitsNet.Information.FromKilobytes(pm);

        ProjectName = "CFLMnavi";
        ApplicationModes = [ApplicationMode.Viewer, ApplicationMode.SEM, ApplicationMode.FIB, ApplicationMode.LM];

        if (applicationConfig.Features.EnableConfocalMode)
        {
            ApplicationModes.Add(ApplicationMode.Confocal);
        }

        _disposables.Add(
            Observable
                .Interval(TimeSpan.FromSeconds(1))
                .Subscribe(_ =>
                {
                    try
                    {
                        var ws = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
                        var workingSet = UnitsNet.Information.FromBytes(ws);
                        var usedPercent = UnitsNet.Ratio.FromDecimalFractions(workingSet.Bytes / _physicalMemory.Bytes);
                        WindowTitle = $"{ProjectName} (MEM {(int)workingSet.Megabytes}Mb/{usedPercent.Percent:0.00}%)";
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

        Application.Current.MainWindow.Closing += MainWindow_Closing;
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
