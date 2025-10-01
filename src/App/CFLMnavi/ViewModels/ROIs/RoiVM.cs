using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Data;
using Betrian.CflmNavi.App.Infrastructure;
using Betrian.CflmNavi.App.Services.Application;
using CFLMnavi.Projects;
using CFLMnavi.Projects.Implementation;
using CFLMnavi.VirtualDevices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;

namespace Betrian.CflmNavi.App.ViewModels.ROIs;

public partial class RoiVM : ObservableProjectVMBase
{
    private RegionOfInterest _roi;
    private readonly IStageNavigation _stageNavigation;
    private readonly ISafeStageControlling _safeStageControlling;
    private readonly IWindowService _windowService;
    public List<object> Markers { get; set; } = [];

    private readonly ReadOnlyObservableCollection<RoiChildVM> _roiChildVms;
    private readonly ObservableCollectionExtended<RoiChildVM> _roiChildVmsSource = [];
    private readonly SourceList<RoiChildVM> _roiChildVmsSourceList;

    public ICollectionView RoiChildVMs
        => Application.Current.Dispatcher.Invoke(() =>
        {
            var source = new CollectionViewSource()
            {
                Source = _roiChildVms
            };
            var view = source.View;
            var groupDescription = new PropertyGroupDescription(nameof(RoiChildVM.FiducialsGroupId));
            view.GroupDescriptions.Add(groupDescription);
            return view;
        });

    internal IWindowService WindowService => _windowService;
    internal IStageNavigation StageNavigation => _stageNavigation;
    internal ObservableProject ObservableProject => _observableProject;

    internal ReadOnlyCollection<RoiChildVM> UngroupedRoiChildVMs => _roiChildVmsSource.AsReadOnly();

    [ObservableProperty]
    public partial bool CorrelationByFiducials { get; private set; }

    public RoiControlViewModel Parent { get; }

    public RoiVM(
        ObservableProject observableProject, RoiControlViewModel roiControlViewModel, RegionOfInterest regionOfInterest,
        IStageNavigation stageNavigation, ISafeStageControlling safeStageControlling, IWindowService windowService
    )
        : base(observableProject)
    {
        _roi = regionOfInterest;
        _stageNavigation = stageNavigation;
        _safeStageControlling = safeStageControlling;
        _windowService = windowService;
        Parent = roiControlViewModel;
        Name = _roi.Name;
        
        if (_roi.TileSets3D is null)
        {
            _roi = _roi with { TileSets3D = [] };
        }

        _roiChildVmsSourceList = new(_roiChildVmsSource.ToObservableChangeSet());

        _ = _roiChildVmsSourceList
            .Connect()
            .Sort(SortExpressionComparer<RoiChildVM>.Ascending(vm => vm.SortId))
            .ObserveOnDispatcher()
            .Bind(out _roiChildVms)
            .Subscribe();

        CreateChildren();
    }

    private bool CanBeDeleted => _roi.CanBeDeleted;
    // TODO: bound UI behavior
    private bool CanBeRenamed => _roi.CanBeRenamed;

    public Guid Id => _roi.Id;

    private static readonly RoiChildBehaviors _tileSetsBehaviors = new()
    {
        SupportsRemoveCommand = true,
        CanBeMarkedAsReference = true,
    };

    private static readonly RoiChildBehaviors _tileSets3DBehaviors = new()
    {
        SupportsRemoveCommand = true,
        CanBeMarkedAsReference = true,
    };

    private static readonly RoiChildBehaviors _zStacksBehaviors = new()
    {
        SupportsRemoveCommand = true,
        CanBeMarkedAsReference = true,
    };

    private static readonly RoiChildBehaviors _singleImagesBehaviors = new()
    {
        SupportsRemoveCommand = true,
        CanBeMarkedAsReference = true,
    };

    private void CreateChildren()
    {
        foreach (var descriptor in _roi.Images)
        {
            foreach (var layerContent in descriptor.Images)
            {
                _roiChildVmsSource.Add(new SingleImageChildVM(this, null, descriptor, layerContent, _singleImagesBehaviors));
            }
        }

        foreach (var descriptor in _roi.TileSets)
        {
            _roiChildVmsSource.Add(
                new TileSetVirtualChildVM(this, null, descriptor, _tileSetsBehaviors)
            );
        }

        foreach (var descriptor in _roi.ZStacks)
        {
            _roiChildVmsSource.Add(
                new ZStackVirtualChildVM(this, null, descriptor, _zStacksBehaviors)
            );
        }

        foreach (var descriptor in _roi.TileSets3D)
        {
            _roiChildVmsSource.Add(
                new TileSet3DVirtualChildVM(this, null, descriptor, _tileSets3DBehaviors)
            );
        }
    }

    [ObservableProperty]
    public partial bool CanChangeCorrelationMode { get; internal set; }

    [RelayCommand]
    private async Task ChangeCorrelationMode()
    {
        await Parent.ImagesStateManager.OnRoiCorrelationModeChanged(this);
    }

    internal void UpdateCorrelationMode(bool correlationByFiducials)
    {
        CorrelationByFiducials = correlationByFiducials;
    }

    [RelayCommand(CanExecute = nameof(CanBeDeleted))]
    private void DeleteRoi()
    {
        if (_roi.CanBeDeleted)
        {
            // TODO: move to parent view model delete objects
            _observableProject.RemoveROI(_roi, true);
        }
    }

    [RelayCommand]
    private async Task NavigateToRoi()
    {
        var position = _stageNavigation.GetStagePosition(_roi.Position, _safeStageControlling.ActiveCameraView);
        using (_windowService.ShowBusyMessage(Messages.StageMoveBusyMessage))
        {
            _ = await _safeStageControlling.MoveStageAsync(position);
        }
    }

    protected override void NameChanged(string name)
    {
        if (_roi.Name != name && _roi.CanBeRenamed)
        {
            _observableProject.UpdateROI(_roi, roi => roi.Name = name);
        }
    }

    private void UpdateTileSets()
    {
        foreach (var descriptor in _roi.TileSets.ToImmutableArray())
        {
            var tilesetVM =
                _roiChildVmsSource
                    .OfType<TileSetVirtualChildVM>()
                    .FirstOrDefault(vm => vm.Descriptor.Id == descriptor.Id);

            if (tilesetVM is not null)
            {
                tilesetVM.UpdateAttributes();
            }
            else
            {
                _roiChildVmsSource.Add(
                    new TileSetVirtualChildVM(this, null, descriptor, _tileSetsBehaviors)
                );
            }
        }
    }

    private void UpdateZStacks()
    {
        foreach (var descriptor in _roi.ZStacks.ToImmutableArray())
        {
            var stackVM =
                _roiChildVmsSource
                    .OfType<ZStackVirtualChildVM>()
                    .FirstOrDefault(vm => vm.Descriptor.Id == descriptor.Id);

            if (stackVM is not null)
            {
                stackVM.UpdateAttributes();
            }
            else
            {
                _roiChildVmsSource.Add(
                    new ZStackVirtualChildVM(this, null, descriptor, _zStacksBehaviors)
                );
            }
        }
    }

    private void UpdateTileSets3D()
    {
        if (_roi.TileSets3D is null)
        {
            // old project version
            return;
        }

        foreach (var descriptor in _roi.TileSets3D.ToImmutableArray())
        {
            var stackVM =
                _roiChildVmsSource
                    .OfType<TileSet3DVirtualChildVM>()
                    .FirstOrDefault(vm => vm.Descriptor.Id == descriptor.Id);

            if (stackVM is not null)
            {
                stackVM.UpdateAttributes();
            }
            else
            {
                _roiChildVmsSource.Add(
                    new TileSet3DVirtualChildVM(this, null, descriptor, _tileSets3DBehaviors)
                );
            }
        }
    }

    private void UpdateImages()
    {
        var roiImages = _roi.Images.ToImmutableArray();

        foreach (var descriptor in roiImages)
        {
            foreach (var layerContent in descriptor.Images)
            {
                var singleImageVm =
                    _roiChildVmsSource
                        .OfType<SingleImageChildVM>()
                        .FirstOrDefault(vm => vm.Descriptor.Id == descriptor.Id && vm.Content.Id == layerContent.Id);

                if (singleImageVm is not null)
                {
                    singleImageVm.UpdateAttributes();
                }
                else
                {
                    _roiChildVmsSource.Add(new SingleImageChildVM(this, null, descriptor, layerContent, _singleImagesBehaviors));
                }
            }
        }
    }

    internal void RemoveChild(RoiChildVM child)
    {
        Application.Current.Dispatcher.Invoke(() => _ = _roiChildVmsSource.Remove(child));
    }

    internal void Update()
    {
        UpdateImages();
        UpdateTileSets();
        UpdateZStacks();
        UpdateTileSets3D();

        Application.Current.Dispatcher.Invoke(() =>
        {
            if (Id.Equals(Parent.SelectedRoi?.Id))
            {
                Parent.ImagesStateManager.UpdateCanToggleVisibilities();
            }

            OnPropertyChanged(nameof(RoiChildVMs));
        });
    }

    internal void ModeDeInitialized()
    {
        _roiChildVms.ForEach(vm => vm.OnModeDeInitialized());
    }
}
