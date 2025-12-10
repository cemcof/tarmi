using System.Collections.ObjectModel;
using Tarmi.Imaging.Common;
using Tarmi.Models;
using Tarmi.Projects;
using Tarmi.Projects.Implementation;
using Tarmi.VirtualDevices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData.Binding;
using DynamicData;
using Microsoft.Extensions.Logging;
using UnitsNet;
using System.Reactive.Linq;
using System.Windows;
using Tarmi.App.ViewModels.ROIs;

namespace Tarmi.App.ViewModels.Fiducials;

public partial class FiducialPointControlViewModel : ObservableObject, IDisposable
{
    private readonly ILogger _logger;
    private readonly IProjectManager _projectManager;

    private ObservableProject? _activeProject;

    private CorrelationInfo? _referenceCorrelationInfo;

    [ObservableProperty]
    public partial ImageMetadata? ImageMetadata { get; set; }

    private readonly ReadOnlyObservableCollection<Fiducial> _fiducials;
    private readonly ObservableCollectionExtended<Fiducial> _fiducialsSource = [];
    private readonly SourceList<Fiducial> _fiducialsSourceList;
    public ReadOnlyObservableCollection<Fiducial> Fiducials => _fiducials;

    private readonly IDisposable _activeProjectSubscription;
    private IDisposable? _fiducialsUpdateDisposable;

    private readonly IStageNavigation _stageNavigation;
    public FiducialPointControlViewModel(ILogger logger, IProjectManager projectManager, IStageNavigation stageNavigation)
    {
        _logger = logger;
        _projectManager = projectManager;
        _stageNavigation = stageNavigation;

        _fiducialsSourceList = new(_fiducialsSource.ToObservableChangeSet());

        _ = _fiducialsSourceList
            .Connect()
            .ObserveOnDispatcher()
            .Bind(out _fiducials)
            .Subscribe();

        _activeProjectSubscription = _projectManager.ActiveProject.Subscribe(OnActiveProjectChanged);
    }

    public void Dispose()
    {
        _activeProjectSubscription.Dispose();
        _fiducialsUpdateDisposable?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void OnActiveProjectChanged(ObservableProject? project)
    {
        _fiducialsUpdateDisposable?.Dispose();

        _activeProject = project;
        if (_activeProject is not null)
        {
            _fiducialsUpdateDisposable = _activeProject.FiducialsUpdate.Subscribe(_ => UpdateFiducials());
        }
    }

    private readonly RatioPoint RelativeCenterPoint = new()
    {
        X = Ratio.FromDecimalFractions(0.5),
        Y = Ratio.FromDecimalFractions(0.5)
    };

    public bool CanAddFiducial => _referenceCorrelationInfo?.IsReferenceImage ?? false;

    public void SetFiducialsSource(ImageChildVM? imageChildVM)
    {
        ImageMetadata = imageChildVM?.ImageMetadata;
        _referenceCorrelationInfo = imageChildVM?.CorrelationInfo;

        if (imageChildVM is not null)
        {
            _activeProject?.SetAddingCorrelationInfo(imageChildVM.CorrelationInfo);
        }
        else
        {
            _activeProject?.UnsetAddingCorrelationInfo();
        }

        UpdateFiducials();
        Application.Current.Dispatcher.Invoke(AddFiducialCommand.NotifyCanExecuteChanged);
    }

    [RelayCommand(CanExecute = nameof(CanAddFiducial))]
    private void AddFiducial()
    {
        if (_referenceCorrelationInfo is null || ImageMetadata is null)
        {
            return;
        }
        var position = _stageNavigation.GetPlanePositionFromImageLocation(RelativeCenterPoint, ImageMetadata);
        _activeProject?.AddFiducial(position);
    }

    [RelayCommand]
    private void RemoveFiducial(Fiducial fiducialPoint)
    {
        if (fiducialPoint.Reference is not null)
        {
            _activeProject?.RemoveFiducial(fiducialPoint.Reference);
        }
    }

    private void UpdateFiducials()
    {
        _fiducialsSource.Clear();
        if (_referenceCorrelationInfo is null || ImageMetadata is null)
        {
            return;
        }
        var idsToNames = _activeProject?.GetActiveRegionOfInterest()?.Fiducials?.ToDictionary(fiducial => fiducial.Id, fiducial => fiducial.Name) ?? [];
        var index = 1;
        foreach (var fiducial in _referenceCorrelationInfo.FiducialPoints)
        {
            if (_stageNavigation.IsPlanePositionInImage(fiducial.Position, ImageMetadata))
            {
                var relativePosition = _stageNavigation.GetImageLocationFromPlanePosition(fiducial.Position, ImageMetadata);
                if (!idsToNames.TryGetValue(fiducial.Id, out var name))
                {
                    _logger.LogError("Fiducial name with {ID} not found.", fiducial.Id);
                    name = "Unknown";
                }
                _fiducialsSource.Add(new Fiducial()
                {
                    Label = name,
                    X = relativePosition.X,
                    Y = relativePosition.Y,
                    Reference = fiducial,
                    OnMoveFinished = UpdateFiducialPosition
                });
            }
            index++;
        }
    }

    private void UpdateFiducialPosition(XYPoint point)
    {
        if (_activeProject is null ||
            _referenceCorrelationInfo is null ||
            point is not Fiducial fiducial ||
            fiducial.Reference is null ||
            ImageMetadata is null)
        {
            return;
        }

        var imageSize = ImageMetadata.Coordinates.ImageSize;
        var targetPosition = _stageNavigation.GetPlanePositionFromImageLocation(new RatioPoint()
        {
            X = Ratio.FromDecimalFractions(point.X / imageSize.Width),
            Y = Ratio.FromDecimalFractions(point.Y / imageSize.Height),
        }, ImageMetadata);
        var updatedPoint = fiducial.Reference with
        {
            Position = targetPosition
        };

        _logger.LogDebug("Updating fiducial point of {ImageId} [{ImageSize}] -> {TargetPosition}", ImageMetadata.ImageId, imageSize, updatedPoint);

        _activeProject.UpdateFiducial(_referenceCorrelationInfo, updatedPoint);
    }
}
