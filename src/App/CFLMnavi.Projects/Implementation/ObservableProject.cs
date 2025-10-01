using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Betrian.App.Infrastructure.Threading;
using Betrian.Models;
using CFLMnavi.Configuration;
using CFLMnavi.Configuration.Holders;
using Microsoft.Extensions.Logging;

namespace CFLMnavi.Projects.Implementation;

public class ObservableProject
{
    private readonly ProjectManager _projectManager;
    private readonly Project _project;
    private readonly ILogger _logger;
    private readonly Mutex _manipulationLock = new();


    private readonly BehaviorSubject<ActiveView> _activeView = new(Betrian.Models.ActiveView.Zero);
    public IObservable<ActiveView> ActiveView => _activeView.AsObservable().DistinctUntilChanged();

    private readonly BehaviorSubject<Guid> _activeRegionOfInterestId = new(Guid.Empty);
    public IObservable<Guid> ActiveRegionOfInterestIdChanges => _activeRegionOfInterestId.AsObservable();
    public Guid ActiveRegionOfInterestId => _activeRegionOfInterestId.Value;

    public RegionOfInterest? GetActiveRegionOfInterest() => GetRegionOfInterest(ActiveRegionOfInterestId);

    private readonly Subject<Guid> _regionsOfInterestSubject = new();
    public IObservable<Guid> RegionsOfInterestObservable => _regionsOfInterestSubject.AsObservable();

    public IObservable<Unit> FiducialsUpdate => _fiducialsManager.FiducialsUpdate;

    public IReadOnlyList<RegionOfInterest> RegionsOfInterest => _project.RegionsOfInterest;

    public bool HasReferenceImageInActiveRoi => _fiducialsManager.HasReferenceImage;

    private readonly FiducialsManager _fiducialsManager;

    public string Name => _project.Name;
    public Holder Holder => _project.Holder;
    public string ProjectDirectory => _project.Directory;

    public ApplicationConfig Config { get; }

    public ObservableProject(ProjectManager projectManager, Project project, ILogger<ObservableProject> logger, ApplicationConfig applicationConfig)
    {
        _projectManager = projectManager;
        _project = project;
        _logger = logger;
        Config = applicationConfig;

        _fiducialsManager = new(projectManager, project, logger);
    }

    private RegionOfInterest? GetRegionOfInterest(Guid roiId) => _project.RegionsOfInterest.Find(roi => roi.Id.Equals(roiId));

    public void AddROI(RegionOfInterest regionOfInterest)
    {
        using var manipulationGuard = _manipulationLock.UseOnce();
        _logger.LogInformation("Adding {RegionOfInterestId}:{RegionOfInterestName}", regionOfInterest.Id, regionOfInterest.Name);

        _project.RegionsOfInterest.Add(regionOfInterest);
        Save();
        NotifyRegionsOfInterestChange(regionOfInterest.Id);
    }

    public void UpdateROI(RegionOfInterest regionOfInterest, Action<RegionOfInterest> update)
    {
        using var manipulationGuard = _manipulationLock.UseOnce();
        _logger.LogInformation("Updating {RegionOfInterestId}:{RegionOfInterestName}", regionOfInterest.Id, regionOfInterest.Name);

        if (!_project.RegionsOfInterest.Contains(regionOfInterest))
        {
            _logger.LogError("Could not find {@RegionOfInterest} in the {@Project}", regionOfInterest, _project);
            return;
        }

        update(regionOfInterest);
        Save();
    }

    public void RemoveROI(RegionOfInterest regionOfInterest, bool notify = true)
    {
        using var manipulationGuard = _manipulationLock.UseOnce();
        _logger.LogInformation("Removing {RegionOfInterestId}:{RegionOfInterestName}", regionOfInterest.Id, regionOfInterest.Name);

        if (!_project.RegionsOfInterest.Remove(regionOfInterest))
        {
            _logger.LogWarning("Could not find {@RegionOfInterest} in the {@Project}", regionOfInterest, _project);
        }

        Save();
        if (notify)
        {
            NotifyRegionsOfInterestChange(regionOfInterest.Id);
        }
    }

    public void AddFocusPoint(TileSetDescriptor tileSetDescriptor, FocusPoint focusPoint)
    {
        using var manipulationGuard = _manipulationLock.UseOnce();
        RegionOfInterest? regionOfInterest = GetRegionOfInterest(tileSetDescriptor.RegionOfInterestId);
        if (regionOfInterest is null)
        {
            _logger.LogError("Could not find {@RegionOfInterest} in the {@Project}", tileSetDescriptor.RegionOfInterestId, _project);
            return;
        }
        var tileSet = regionOfInterest.TileSets.FirstOrDefault(tileset => tileset.Id == tileSetDescriptor.Id);
        tileSet?.GrabbingOptions.FocusPoints.Add(focusPoint);
        Save();
    }

    public void RemoveFocusPoint(TileSetDescriptor tileSetDescriptor, FocusPoint focusPoint)
    {
        using var manipulationGuard = _manipulationLock.UseOnce();
        RegionOfInterest? regionOfInterest = GetRegionOfInterest(tileSetDescriptor.RegionOfInterestId);
        if (regionOfInterest is null)
        {
            _logger.LogError("Could not find {@RegionOfInterest} in the {@Project}", tileSetDescriptor.RegionOfInterestId, _project);
            return;
        }
        var tileSet = regionOfInterest.TileSets.FirstOrDefault(tileset => tileset.Id == tileSetDescriptor.Id);
        _ = tileSet?.GrabbingOptions.FocusPoints.Remove(focusPoint);
        Save();
    }

    public void UpdateFocusPoint(TileSetDescriptor tileSetDescriptor, FocusPoint oldFocusPoint, FocusPoint newFocusPoint)
    {
        using var manipulationGuard = _manipulationLock.UseOnce();
        RegionOfInterest? regionOfInterest = GetRegionOfInterest(tileSetDescriptor.RegionOfInterestId);
        if (regionOfInterest is null)
        {
            _logger.LogError("Could not find {@RegionOfInterest} in the {@Project}", tileSetDescriptor.RegionOfInterestId, _project);
            return;
        }
        var tileSet = regionOfInterest.TileSets.FirstOrDefault(tileset => tileset.Id == tileSetDescriptor.Id);
        _ = tileSet?.GrabbingOptions.FocusPoints.Remove(oldFocusPoint);
        tileSet?.GrabbingOptions.FocusPoints?.Add(newFocusPoint);
        Save();
    }

    public void RemoveDescriptor<T>(T descriptor, bool save = true, bool notify = true)
        where T : LayerDescriptor
    {
        static bool RemoveById<TDescriptor>(List<TDescriptor> list, LayerDescriptor descriptor)
            where TDescriptor : LayerDescriptor
        {
            var item = list.FirstOrDefault(d => d.Id == descriptor.Id);
            return item is not null && list.Remove(item);
        }

        using var manipulationGuard = _manipulationLock.UseOnce();
        var roi = GetRegionOfInterest(descriptor.RegionOfInterestId);
        if (roi is not null)
        {
            _logger.LogInformation("Removing descriptor {DescriptorId}({DescriptorType}) form {RegionOfInterestId}:{RegionOfInterestName}", descriptor.Id, descriptor.GetType().Name, roi.Id, roi.Name);

            bool wasRemoved = descriptor switch
            {
                LayeredImageDescriptor => RemoveById(roi.Images, descriptor),
                TileSetDescriptor => RemoveById(roi.TileSets, descriptor),
                TileSet3DDescriptor => RemoveById(roi.TileSets3D, descriptor),
                ZStackDescriptor => RemoveById(roi.ZStacks, descriptor),
                _ => throw new InvalidOperationException()
            };

            if (!wasRemoved)
            {
                _logger.LogWarning("Could not find {DescriptorId} of the {RegionOfInterestId} in the {Project}", descriptor.Id, roi?.Id, _project.Name);
            }
            else
            {
                if (save)
                {
                    Save();
                }
                if (notify)
                {
                    NotifyRegionsOfInterestChange(roi.Id);
                }
            }
        }
        else
        {
            _logger.LogWarning("Could not find parent ROI of {DescriptorId} in {Project}", descriptor.Id, _project.Name);
        }
    }

    public void AddOrUpdateDescriptor<T>(T descriptor, bool save = true, bool notify = true)
        where T : LayerDescriptor
    {
        static bool RemoveById<TDescriptor>(List<TDescriptor> list, LayerDescriptor descriptor)
            where TDescriptor : LayerDescriptor
        {
            var item = list.FirstOrDefault(d => d.Id == descriptor.Id);
            return item is not null && list.Remove(item);
        }

        using var manipulationGuard = _manipulationLock.UseOnce();
        var roi = GetRegionOfInterest(descriptor.RegionOfInterestId);
        if (roi is not null)
        {
            _logger.LogInformation("AddOrUpdate descriptor {DescriptorId}({DescriptorType}) form {RegionOfInterestId}:{RegionOfInterestName}", descriptor.Id, descriptor.GetType().Name, roi.Id, roi.Name);

            _ = descriptor switch
            {
                LayeredImageDescriptor => RemoveById(roi.Images, descriptor),
                TileSetDescriptor => RemoveById(roi.TileSets, descriptor),
                TileSet3DDescriptor => RemoveById(roi.TileSets3D, descriptor),
                ZStackDescriptor => RemoveById(roi.ZStacks, descriptor),
                _ => throw new InvalidOperationException()
            };

            switch (descriptor)
            {
                case LayeredImageDescriptor lid: roi.Images.Add(lid); break;
                case TileSetDescriptor tsd: roi.TileSets.Add(tsd); break;
                case TileSet3DDescriptor ts3d: roi.TileSets3D.Add(ts3d); break;
                case ZStackDescriptor zsd: roi.ZStacks.Add(zsd); break;
                default: throw new InvalidOperationException();
            }
            if (save)
            {
                Save();
            }
            if (notify)
            {
                NotifyRegionsOfInterestChange(roi.Id);
            }
        }
        else
        {
            _logger.LogWarning("Could not find parent ROI of {DescriptorId} in {Project}", descriptor.Id, _project.Name);
        }
    }

    public TileSetDescriptor CreateTileSetDescriptor(StageCameraView cameraView, Guid regionOfInterestId, TileSetOptions tileSetOptions)
        => _project.CreateTileSetDescriptor(cameraView, regionOfInterestId, tileSetOptions);

    public ZStackDescriptor CreateZStackDescriptor(StageCameraView cameraView, Guid regionOfInterestId, Guid? linkId)
        => _project.CreateZStackDescriptor(cameraView, regionOfInterestId, linkId);

    public TileSet3DDescriptor CreateTileSet3DDescriptor(StageCameraView cameraView, Guid regionOfInterestId, TileSetOptions tileSetOptions, Guid? linkId)
        => _project.CreateTileSet3DDescriptor(cameraView, regionOfInterestId, tileSetOptions, linkId);

    public LayeredImageDescriptor CreateLayeredImageDescriptor(StageCameraView cameraView, Guid regionOfInterestId)
        => _project.CreateLayeredImageDescriptor(cameraView, regionOfInterestId);

    public LayeredImageDescriptor GetOrCreateLayeredImageDescriptor(StageCameraView cameraView, Guid regionOfInterestId)
    {
        var roi = GetRegionOfInterest(regionOfInterestId);
        if (roi is not null)
        {
            var layer = roi.Images.FirstOrDefault(layer => layer.Source == cameraView);
            if (layer is not null)
            {
                return layer;
            }
        }
        return _project.CreateLayeredImageDescriptor(cameraView, regionOfInterestId);
    }

    public string GetContentFilePath(LayerDescriptor layerDescriptor, LayerDescriptor subLayerDescriptor, LayerContentDescriptor contentDescriptor)
        => _project.GetContentFilePath(layerDescriptor, subLayerDescriptor, contentDescriptor);

    public string GetContentFilePath(LayerDescriptor layerDescriptor, LayerContentDescriptor contentDescriptor)
        => _project.GetContentFilePath(layerDescriptor, contentDescriptor);

    public string GetLayerDirectoryPath(LayerDescriptor layerDescriptor)
        => _project.GetLayerDirectoryPath(layerDescriptor);

    public string GetLayerDirectoryPath(LayerDescriptor layerDescriptor, LayerDescriptor subLayerDescriptor)
        => _project.GetLayerDirectoryPath(layerDescriptor, subLayerDescriptor);

    public void Save()
    {
        using var manipulationGuard = _manipulationLock.UseOnce();
        _logger.LogInformation("Saving project");
        _projectManager.SaveProject(_project);
    }

    private void NotifyRegionsOfInterestChange(Guid regionOfInterestId) => _logger.Swallow(() => _regionsOfInterestSubject.OnNext(regionOfInterestId));

    public void PublishActiveView(ActiveView activeView) => _logger.Swallow(() => _activeView.OnNext(activeView));

    public void PublishSelectedRoiChange(Guid regionOfInterestId)
    {
        using var manipulationGuard = _manipulationLock.UseOnce();
        _logger.Swallow(() => _activeRegionOfInterestId.OnNext(regionOfInterestId));
        var activeRoi = GetActiveRegionOfInterest();
        if (activeRoi is null)
        {
            _logger.LogError("No ROI selected.");
            return;
        }
        _fiducialsManager.UpdateActiveRoi(activeRoi);
    }

    public void AddFiducial(LengthPoint fiducialPoint)
    {
        using var manipulationGuard = _manipulationLock.UseOnce();
        RegionOfInterest? activeRoi = GetActiveRegionOfInterest();
        if (activeRoi is null)
        {
            _logger.LogError("No ROI selected.");
            return;
        }
        _fiducialsManager.AddFiducial(activeRoi, fiducialPoint);
    }

    public void UpdateFiducial(CorrelationInfo correlationInfo, FiducialPoint updatedPoint)
    {
        using var manipulationGuard = _manipulationLock.UseOnce();
        RegionOfInterest? activeRoi = GetActiveRegionOfInterest();
        if (activeRoi is null)
        {
            _logger.LogError("No ROI selected.");
            return;
        }
        _fiducialsManager.UpdateFiducial(activeRoi, correlationInfo, updatedPoint);
    }

    public void RenameFiducial(Guid fiducialId, string newName)
    {
        using var manipulationGuard = _manipulationLock.UseOnce();
        var activeRoi = GetActiveRegionOfInterest();
        if (activeRoi is null)
        {
            _logger.LogError("No ROI selected.");
            return;
        }
        _fiducialsManager.RenameFiducial(activeRoi, fiducialId, newName);
    }

    public void SetReference(CorrelationInfo correlationInfo)
    {
        using var manipulationGuard = _manipulationLock.UseOnce();
        var activeRoi = GetActiveRegionOfInterest();
        if (activeRoi is null)
        {
            _logger.LogError("No active ROI.");
            return;
        }
        _logger.LogInformation("Changing REF image for {RegionOfInterestId}:{RegionOfInterestName}", activeRoi.Id, activeRoi.Name);
        _fiducialsManager.SetReference(activeRoi, correlationInfo);
    }

    public void PrepareFiducials(CorrelationInfo correlationInfo)
    {
        using var manipulationGuard = _manipulationLock.UseOnce();
        var activeRoi = GetActiveRegionOfInterest();
        if (activeRoi is null)
        {
            _logger.LogError("No active ROI.");
            return;
        }
        _fiducialsManager.PrepareFiducials(activeRoi, correlationInfo);
    }

    public void UnbindCorrelation(CorrelationInfo correlationInfo)
    {
        using var manipulationGuard = _manipulationLock.UseOnce();
        var activeRoi = GetActiveRegionOfInterest();
        if (activeRoi is null)
        {
            _logger.LogError("No ROI selected.");
            return;
        }
        _fiducialsManager.Unbind(activeRoi, correlationInfo);
    }

    public void BindCorrelation(CorrelationInfo parentInfo, CorrelationInfo childInfo)
    {
        using var manipulationGuard = _manipulationLock.UseOnce();
        _fiducialsManager.Bind(parentInfo, childInfo);
    }

    public void UnsetReference()
    {
        using var manipulationGuard = _manipulationLock.UseOnce();
        var activeRoi = GetActiveRegionOfInterest();
        if (activeRoi is null)
        {
            _logger.LogError("No ROI selected.");
            return;
        }
        _fiducialsManager.SetReference(activeRoi, null);
    }

    public void SetAddingCorrelationInfo(CorrelationInfo correlationInfo) => _fiducialsManager.SetAddingCorrelationInfo(correlationInfo);
    public void UnsetAddingCorrelationInfo() => _fiducialsManager.SetAddingCorrelationInfo(null);
}
