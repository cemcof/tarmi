using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Data;
using Tarmi.Models;
using Tarmi.Projects;
using Tarmi.Projects.Implementation;
using Tarmi.VirtualDevices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Tarmi.App.Services.Application;
using Tarmi.App.Infrastructure;

namespace Tarmi.App.ViewModels.ROIs;

public partial class RoiVM : ObservableProjectVMBase
{
    private readonly RegionOfInterest _roi;
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

    public Guid Id => _roi.Id;

    private static readonly RoiChildBehaviors TileSetBehaviors = new()
    {
        HasContextMenu = true,
        SupportsRemoveCommand = true,
        HasMarkAsReferenceMenu = true,
        CanHaveReferenceAttribute = true,
    };

    private static readonly RoiChildBehaviors TileSet3DBehaviors = new()
    {
        HasContextMenu = true,
        SupportsRemoveCommand = true,
        HasMarkAsReferenceMenu = true,
        CanHaveReferenceAttribute = true,
    };

    private static readonly RoiChildBehaviors ZStackBehaviors = new()
    {
        HasContextMenu = true,
        SupportsRemoveCommand = true,
        HasMarkAsReferenceMenu = true,
        CanRegenerateMipImage = true,
        CanHaveReferenceAttribute = true,
    };

    private static readonly RoiChildBehaviors SingleImageBehaviors = new()
    {
        HasContextMenu = true,
        SupportsRemoveCommand = true,
        HasMarkAsReferenceMenu = true,
        CanHaveReferenceAttribute = true,
        CanBindCorrelation = true,
        CanEditCorrelationOptions = true,
        CanEditFiducials = true,
        CanExportToMaps = true,
        CanEditMilling = true,
    };

    private void CreateChildren()
    {
        foreach (var descriptor in _roi.Images)
        {
            foreach (var layerContent in descriptor.Images)
            {
                _roiChildVmsSource.Add(
                    new SingleImageChildVM(this, null, descriptor, layerContent, SingleImageBehaviors)
                );
            }
        }

        foreach (var descriptor in _roi.TileSets)
        {
            _roiChildVmsSource.Add(
                new TileSetVirtualChildVM(this, null, descriptor, TileSetBehaviors)
            );
        }

        foreach (var descriptor in _roi.ZStacks)
        {
            _roiChildVmsSource.Add(
                new ZStackVirtualChildVM(this, null, descriptor, ZStackBehaviors)
            );
        }

        foreach (var descriptor in _roi.TileSets3D)
        {
            _roiChildVmsSource.Add(
                new TileSet3DVirtualChildVM(this, null, descriptor, TileSet3DBehaviors)
            );
        }
    }

    [ObservableProperty]
    public partial bool CanChangeCorrelationMode { get; internal set; }

    [RelayCommand]
    private async Task ChangeCorrelationMode()
        => await Parent.ImagesStateManager.OnRoiCorrelationModeChanged(this);

    internal void UpdateCorrelationMode(bool correlationByFiducials)
        => CorrelationByFiducials = correlationByFiducials;

    [RelayCommand(CanExecute = nameof(CanBeDeleted))]
    private void DeleteRoi()
    {
        if (_roi.CanBeDeleted)
        {
            foreach (var child in _roiChildVms)
            {
                child.RemoveFiles();
            }
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

    protected override void NameChanged(string value)
    {
        if (_roi.Name != value && _roi.CanBeRenamed)
        {
            _observableProject.UpdateROI(_roi, roi => roi.Name = value);
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
                    new TileSetVirtualChildVM(this, null, descriptor, TileSetBehaviors)
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
                    new ZStackVirtualChildVM(this, null, descriptor, ZStackBehaviors)
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
                    new TileSet3DVirtualChildVM(this, null, descriptor, TileSet3DBehaviors)
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
                var singleImageVm = _roiChildVmsSource
                    .OfType<SingleImageChildVM>()
                    .FirstOrDefault(vm => vm.Descriptor.Id == descriptor.Id && vm.Content.Id == layerContent.Id);

                if (singleImageVm is not null)
                {
                    singleImageVm.UpdateAttributes();
                }
                else
                {
                    _roiChildVmsSource.Add(new SingleImageChildVM(this, null, descriptor, layerContent, SingleImageBehaviors));
                }
            }
        }
    }

    internal void RemoveChild(RoiChildVM child)
        => Application.Current.Dispatcher.Invoke(() => _roiChildVmsSource.Remove(child));

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
        => _roiChildVms.ForEach(vm => vm.OnModeDeInitialized());

    internal static bool IsBindable(RoiChildVM child)
    {
        var source = child switch
        {
            SingleImageChildVM singleImageChild => singleImageChild.Descriptor.Source,
            TileSetVirtualChildVM tileSetVirtualChild => tileSetVirtualChild.Descriptor.Source,
            TileSet3DVirtualChildVM tileSet3DVirtualChild => tileSet3DVirtualChild.Descriptor.Source,
            ZStackVirtualChildVM zStackVirtualChildVM => zStackVirtualChildVM.Descriptor.Source,
            _ => StageCameraView.Unknown,
        };
        return source == StageCameraView.LM;
    }

    internal void BindCorrelation(RoiChildVM child)
    {
        var root = child.GetRoot();
        var luminescenceImages = _roiChildVmsSource
            .OfType<RoiChildVM>()
            .Where(IsBindable)
            .Except([root]);

        var selectedChild = WindowService.ShowImageSelectionDialog(child, luminescenceImages);
        if (selectedChild is null)
        {
            return;
        }

        _observableProject.BindCorrelation(selectedChild.CorrelationInfo, child.CorrelationInfo);
        Update();
    }

    internal void UnbindCorrelation(RoiChildVM child)
    {
        _observableProject.UnbindCorrelation(child.CorrelationInfo);
        Update();
    }
}
