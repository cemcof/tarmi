using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Betrian.App.Infrastructure;
using Betrian.CflmNavi.App.Services.Application;
using Betrian.Imaging.Algorithms.Utilities;
using Betrian.Imaging.Common;
using CFLMnavi.WPF.ViewModels;
using CFLMnavi.ImagePipeline.Pipelines;
using CFLMnavi.Projects;
using CFLMnavi.Projects.Implementation;
using CFLMnavi.VirtualDevices;
using CFLMnavi.WPF;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Betrian.CflmNavi.App.ViewModels.Modes.Viewer;

public partial class ViewerViewModel : ApplicationModeViewModelBase
{
    private readonly SemaphoreSlim _activationLock = new(1, 1);

    private readonly ImagingPipeline _genericImagingPipeline;

    private readonly IProjectManager _projectManager;
    private readonly IStageNavigation _stageNavigation;
    private readonly List<IDisposable> _subscriptions = [];
    private readonly IApplicationModeService _applicationModeService;

    private readonly IWindowService _windowService;

    [ObservableProperty]
    private SortedDictionary<int, double>? _histogram;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImageWithMetadataNotNull))]
    private ImageWithMetadata? _imageWithMetadata;

    private volatile bool _isActive;

    [ObservableProperty]
    private ObservableProject? _project;
    private string ModeName { get; } = "Viewer";

    public bool ImageWithMetadataNotNull => ImageWithMetadata != null;

    public ViewerViewModel(IWindowService windowService, IProjectManager projectManager, IStageNavigation stageNavigation, ILoggerFactory loggerFactory, IApplicationModeService applicationModeService)
    {
        _windowService = windowService;
        _genericImagingPipeline = new ViewerImagingPipeline(loggerFactory.CreateLogger<ViewerImagingPipeline>(), projectManager, stageNavigation);
        _projectManager = projectManager;
        _stageNavigation = stageNavigation;
        _applicationModeService = applicationModeService;
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

    private string CreateActivityName([CallerMemberName] string methodName = "") => $"{nameof(ViewerViewModel)}::{methodName}";

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
}
