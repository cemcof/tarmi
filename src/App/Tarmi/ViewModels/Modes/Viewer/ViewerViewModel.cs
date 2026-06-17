using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Tarmi.App.Infrastructure;
using Tarmi.Imaging.Algorithms.Utilities;
using Tarmi.Imaging.Common;
using Tarmi.App.WPF.ViewModels;
using Tarmi.ImagePipeline.Pipelines;
using Tarmi.Projects;
using Tarmi.Projects.Implementation;
using Tarmi.VirtualDevices;
using Tarmi.App.WPF;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Tarmi.App.Services.Application;
using Tarmi.Configuration;

namespace Tarmi.App.ViewModels.Modes.Viewer;

public partial class ViewerViewModel : ApplicationModeViewModelBase
{
    private readonly SemaphoreSlim _activationLock = new(1, 1);

    private readonly ImagingPipeline _genericImagingPipeline;

    private readonly IProjectManager _projectManager;
    private readonly List<IDisposable> _subscriptions = [];
    private readonly IApplicationModeService _applicationModeService;

    private readonly IWindowService _windowService;

    [ObservableProperty]
    public partial SortedDictionary<int, double>? Histogram { get; set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImageWithMetadataNotNull))]
    public partial ImageWithMetadata? ImageWithMetadata { get; set; }

    private volatile bool _isActive;

    [ObservableProperty]
    public partial ObservableProject? Project { get; set; }
    private string ModeName { get; } = "Viewer";

    public bool ImageWithMetadataNotNull => ImageWithMetadata != null;

    public bool ConfocalModeEnabled { get; }

    public ViewerViewModel(
        IWindowService windowService,
        IProjectManager projectManager,
        IStageNavigation stageNavigation,
        ILoggerFactory loggerFactory,
        IApplicationModeService applicationModeService,
        ApplicationConfig applicationConfig
    )
    {
        _windowService = windowService;
        _genericImagingPipeline = new ViewerImagingPipeline(loggerFactory.CreateLogger<ViewerImagingPipeline>(), projectManager, stageNavigation);
        _projectManager = projectManager;
        _applicationModeService = applicationModeService;
        ConfocalModeEnabled = applicationConfig.Features.EnableConfocalMode;
    }

    protected override async Task DeInitializeCoreAsync(ApplicationMode nextMode)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());
        using var guard = await _activationLock.UseOnceAsync(default);

        if (_isActive)
        {
            await _windowService.ShowIndeterminateWaitingDialogAsync($"Switching from {ModeName} mode.", async progress =>
            {
                CancelAllSubscriptions();
                await _genericImagingPipeline.Clear();
                await base.DeInitializeCoreAsync(nextMode);
                _isActive = false;
            });
        }
    }

    protected override async Task InitializeCoreAsync(ApplicationMode prevMode)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());
        using var guard = await _activationLock.UseOnceAsync(default);

        if (!_isActive)
        {
            await _windowService.ShowIndeterminateWaitingDialogAsync($"Switching to {ModeName} mode.", async progress =>
            {
                _subscriptions.Add(_genericImagingPipeline.Output.Subscribe(HandleImageUpdate));
                _subscriptions.Add(_projectManager.ActiveProject.ObserveOnDispatcher().Subscribe(HandleProjectChange));
                await base.InitializeCoreAsync(prevMode);
                _isActive = true;
            });
        }
    }

    private void CancelAllSubscriptions()
    {
        foreach (IDisposable subscription in _subscriptions)
        {
            subscription.Dispose();
        }
        _subscriptions.Clear();
    }

    private static string CreateActivityName([CallerMemberName] string methodName = "") => $"{nameof(ViewerViewModel)}::{methodName}";

    private void HandleImageUpdate(ImageWithMetadata imageWithMetadata)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());
        ImageWithMetadata = imageWithMetadata;
        Histogram = ImageWithMetadata.Image.GetNormalizedHistogram();
    }

    private void HandleProjectChange(ObservableProject? project)
    {
        Project = project;
    }

    [RelayCommand]
    private void NavigateToSemView()
    {
        _applicationModeService.Mode.OnNext(ApplicationMode.SEM);
    }

    [RelayCommand]
    private void NavigateToFibView()
    {
        _applicationModeService.Mode.OnNext(ApplicationMode.FIB);
    }

    [RelayCommand]
    private void NavigateToLmView()
    {
        _applicationModeService.Mode.OnNext(ApplicationMode.LM);
    }

    [RelayCommand]
    private void NavigateToConfocalView()
    {
        _applicationModeService.Mode.OnNext(ApplicationMode.Confocal);
    }
}
