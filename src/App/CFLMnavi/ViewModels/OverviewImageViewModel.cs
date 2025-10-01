using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using Betrian.App.Infrastructure;
using Betrian.CflmNavi.App.Services.Application;
using Betrian.CflmNavi.App.ViewModels.Modes;
using Betrian.Devices.Thermofisher.Instrument.Types;
using Betrian.Imaging.Common;
using Betrian.Models;
using Betrian.WPF;
using CFLMnavi.Configuration.Holders;
using CFLMnavi.Projects;
using CFLMnavi.Projects.Implementation;
using CFLMnavi.VirtualDevices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using OpenCvSharp.WpfExtensions;
using UnitsNet;

namespace Betrian.CflmNavi.App.ViewModels;

public partial class OverviewImageViewModel : ViewModelBase
{
    private const double PixelRadius = 140;

    private readonly IStageNavigation _stageNavigation;
    private readonly IProjectManager _projectManager;
    private readonly IWindowService _windowService;
    private readonly ISafeStageControlling _safeStageControlling;
    private readonly List<IDisposable> _subscriptions = [];
    private IDisposable? _activeViewSubscription = null;
    private IDisposable? _activeProjectTileSetsSubscription = null;
    private IDisposable? _regionsOfInterestSubscription = null;
    private readonly Dictionary<AreaOfInterest, ImageWithMetadata?> _selectedThumbnailPerGrid = [];

    public List<AreaOfInterest> AvailableGrids => ActiveProject?.Holder.Grids ?? [];

    [ObservableProperty]
    private StageState _stageState = StageState.Zero;

    [ObservableProperty]
    private ActiveView? _activeView;

    [ObservableProperty]
    private ActiveViewVM? _activeViewVM;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AvailableTileSetThumbnails))]
    [NotifyPropertyChangedFor(nameof(SelectedTileSetThumbnail))]
    [NotifyPropertyChangedFor(nameof(ShowAvailableTileSetThumbnails))]
    private AreaOfInterest? _selectedGrid;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AvailableGrids))]
    private ObservableProject? _activeProject;

    [ObservableProperty]
    private bool _overviewEdit;

    [ObservableProperty]
    private ImageWithMetadata? _selectedTileSetThumbnail;

    public ObservableCollection<ImageWithMetadata> TileSetThumbnails { get; } = [];

    public ObservableCollection<ImageWithMetadata> AvailableTileSetThumbnails => GetThumbnailsFromCurrentGrid();

    public bool ShowAvailableTileSetThumbnails => AvailableTileSetThumbnails.Any();

    [ObservableProperty]
    private NavigationImageVM? _navigationImage;

    private readonly ReadOnlyObservableCollection<NavigationRoiVM> _navigationRois;
    private readonly ObservableCollectionExtended<NavigationRoiVM> _navigationRoisSource = [];
    private readonly SourceList<NavigationRoiVM> _navigationRoisSourceList;
    public ReadOnlyObservableCollection<NavigationRoiVM> NavigationRois => _navigationRois;

    public OverviewImageViewModel(IStageNavigation stageNavigation, ISafeStageControlling safeStageControlling, IProjectManager projectManager, IWindowService windowService)
    {
        _stageNavigation = stageNavigation;
        _safeStageControlling = safeStageControlling;
        _projectManager = projectManager;
        _windowService = windowService;

        _navigationRoisSourceList = new(_navigationRoisSource.ToObservableChangeSet());
        _ = _navigationRoisSourceList
            .Connect()
            .ObserveOnDispatcher()
            .Bind(out _navigationRois)
            .Subscribe();
    }

    [RelayCommand]
    public void DeselectTilesetThumbnail()
    {
        if (SelectedGrid != null)
        {
            SelectedTileSetThumbnail = null;
            ImageWithMetadata? image = null;
            _ = _selectedThumbnailPerGrid.AddOrUpdate(SelectedGrid, image, (_, _) => null);
            NavigationImage = null;
        }
    }

    protected override Task InitializeCoreAsync()
    {
        _subscriptions.Add(_projectManager.ActiveProject.Subscribe(HandleActiveProjectChange));
        ActiveProject = _projectManager.GetActiveProject();
        HandleActiveProjectChange(ActiveProject);
        InitializeSelectedThumbnails();
        TileSetThumbnails.CollectionChanged += TileSetThumbnails_CollectionChanged;
        SelectedTileSetThumbnail = AvailableTileSetThumbnails.FirstOrDefault();
        UpdateEverything();
        return Task.CompletedTask;
    }

    protected override Task DeInitializeCoreAsync()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }
        _subscriptions.Clear();
        return Task.CompletedTask;
    }

    [RelayCommand]
    public void SetActiveGrid(AreaOfInterest selectedGrid)
    {
        SelectedGrid = selectedGrid;
        UpdateEverything();
    }

    private void UpdateEverything()
    {
        if (ActiveProject != null && SelectedTileSetThumbnail != null)
        {
            HandleRegionsOfInterestChange(ActiveProject.RegionsOfInterest);
            HandleSelectedTilesetChange(SelectedTileSetThumbnail);
        }
        else if (SelectedTileSetThumbnail is null)
        {
            ClearNavigationImage();
        }
    }

    private void ClearNavigationImage()
    {
        NavigationImage = null;
        HandleRegionsOfInterestChange(ActiveProject?.RegionsOfInterest ?? []);
    }

    private void HandleActiveProjectChange(ObservableProject? project)
    {
        _activeViewSubscription?.Dispose();
        _activeProjectTileSetsSubscription?.Dispose();
        ActiveProject = project;
        SelectedGrid = AvailableGrids.FirstOrDefault();

        _navigationRoisSource.Clear();
        NavigationImage = null;
        TileSetThumbnails.Clear();

        if (ActiveProject is not null)
        {
            _activeViewSubscription = ActiveProject.ActiveView.Subscribe(HandleActiveViewChange);

            _regionsOfInterestSubscription =
                ActiveProject
                    .RegionsOfInterestObservable
                    .Subscribe(_ => HandleRegionsOfInterestChange(ActiveProject.RegionsOfInterest));
            HandleRegionsOfInterestChange(ActiveProject.RegionsOfInterest);

            _activeProjectTileSetsSubscription =
                ActiveProject
                    .RegionsOfInterestObservable
                    .Subscribe(_ => UpdateTileSetThumbnails());
            UpdateTileSetThumbnails();
        }
    }

    private void HandleLinearStagePositionChange(StageState state)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        StageState = state;
    }

    private void HandleActiveViewChange(ActiveView view)
    {
        ActiveView = view;
        UpdateActiveViewVM(view);
    }

    private void UpdateActiveViewVM(ActiveView? view)
    {
        if (SelectedGrid != null && view is not null)
        {
            GetStageCenterAndPixelFactor(out LengthPoint center, out _, out double pixelMeterFactor);

            ActiveViewVM = new()
            {
                X = (view.Center.X - center.X - view.Size.Width / 2).Meters * pixelMeterFactor + PixelRadius,
                Y = (view.Center.Y - center.Y - view.Size.Height / 2).Meters * pixelMeterFactor + PixelRadius,
                Width = view.Size.Width.Meters * pixelMeterFactor,
                Height = view.Size.Height.Meters * pixelMeterFactor
            };
        }
        else
        {
            ActiveViewVM = null;
        }
    }

    private void HandleRegionsOfInterestChange(IEnumerable<RegionOfInterest> rois)
    {
        _navigationRoisSource.Clear();

        if (SelectedGrid != null && ActiveProject != null)
        {
            GetStageCenterAndPixelFactor(out LengthPoint center, out _, out double pixelMeterFactor);

            foreach (var item in rois)
            {
                NavigationRoiVM vm = new(ActiveProject, item, _stageNavigation, _safeStageControlling, _windowService)
                {
                    X = (item.Position.X - center.X).Meters * pixelMeterFactor + PixelRadius,
                    Y = (item.Position.Y - center.Y).Meters * pixelMeterFactor + PixelRadius
                };
                _navigationRoisSource.Add(vm);
            }
        }
    }

    private void HandleSelectedTilesetChange(ImageWithMetadata image)
    {
        NavigationImage = null;

        if (ActiveProject != null && SelectedGrid != null)
        {
            _selectedThumbnailPerGrid[SelectedGrid] = image;
            GetStageCenterAndPixelFactor(out LengthPoint center, out _, out double pixelMeterFactor);

            try
            {
                var stagePosition = image.GetStagePosition();
                var planePosition = _stageNavigation.GetPlanePosition(stagePosition, image.GetSource());

                if (SelectedGrid.BoundingRectangle.IsPointInsideRectangle(planePosition))
                {
                    var metadata = image.Coordinates;

                    BitmapSource bitmapSource = image.Image.Mat.ToBitmapSource();
                    PixelSize pixelSize = image.GetPixelSize();

                    var realImageWidth = bitmapSource.Width * pixelSize.X;
                    var realImageHeight = bitmapSource.Height * pixelSize.Y;
                    NavigationImageVM vm = new()
                    {
                        Image = bitmapSource,
                        Width = realImageWidth.Meters * pixelMeterFactor,
                        Height = realImageHeight.Meters * pixelMeterFactor,
                        X = (planePosition.X - center.X - realImageWidth / 2).Meters * pixelMeterFactor + PixelRadius,
                        Y = (planePosition.Y - center.Y - realImageHeight / 2).Meters * pixelMeterFactor + PixelRadius
                    };
                    NavigationImage = vm;
                    HandleRegionsOfInterestChange(ActiveProject.RegionsOfInterest);
                }
            }
            catch
            {

            }
        }
    }

    private void GetStageCenterAndPixelFactor(out LengthPoint stageCenter, out Length diameter, out double pixelMeterFactor)
    {
        var (center, radius) = SelectedGrid switch
        {
            CircleAreaOfInterest cg => (cg.Center, cg.Radius),
            _ => throw new NotSupportedException("Only circular grids are supported.")
        };

        diameter = (2 * radius);
        pixelMeterFactor = 2 * PixelRadius / diameter.Meters;

        stageCenter = center;
    }

    private LengthRectangle GetImageLengthRectangle(ImageMetadata imageMetadata)
    {
        var planePosition = _stageNavigation.GetPlanePosition(imageMetadata.GetStagePosition(), imageMetadata.GetSource());
        var width = imageMetadata.Coordinates.ImageSize.Width * imageMetadata.Coordinates.PixelSize.X;
        var height = imageMetadata.Coordinates.ImageSize.Height * imageMetadata.Coordinates.PixelSize.Y;
        return new LengthRectangle
        {
            Top = planePosition.Y - height / 2,
            Left = planePosition.X - width / 2,
            Right = planePosition.X + width / 2,
            Bottom = planePosition.Y + height / 2
        };
    }

    private void UpdateTileSetThumbnails()
    {
        if (ActiveProject is null)
        {
            TileSetThumbnails.Clear();
            return;
        }

        var thumbnails = ActiveProject.RegionsOfInterest
            .SelectMany(roi => roi.TileSets)
            .Select(tileset =>
            {
                var layerContent = tileset.StitchedImageThumbnail;
                var path = ActiveProject.GetContentFilePath(tileset, layerContent);
                return TiffImage.Load(path);
            }).ToArray();

        var selectedId = SelectedTileSetThumbnail?.ImageId ?? Guid.Empty;
        var removeImageIds = TileSetThumbnails.Select(t => t.ImageId).Except(thumbnails.Select(t => t.ImageId)).ToArray();
        var addImageIds = thumbnails.Select(t => t.ImageId).Except(TileSetThumbnails.Select(t => t.ImageId)).ToArray();

        TileSetThumbnails.Remove(TileSetThumbnails.Where(t => removeImageIds.Contains(t.ImageId)).ToArray());
        TileSetThumbnails.AddRange(thumbnails.Where(t => addImageIds.Contains(t.ImageId)));

        if (removeImageIds.Contains(selectedId))
        {
            SelectedTileSetThumbnail = TileSetThumbnails.FirstOrDefault(t =>
            {
                var rect = GetImageLengthRectangle(t);
                return SelectedGrid!.Overlaps(rect);
            });
        }
    }

    private bool IsImageFromGrid(ImageWithMetadata image, AreaOfInterest grid)
    {
        var stagePosition = image.GetStagePosition();
        var planePosition = _stageNavigation.GetPlanePosition(stagePosition, image.GetSource());
        return grid.BoundingRectangle.IsPointInsideRectangle(planePosition);
    }

    partial void OnSelectedTileSetThumbnailChanged(ImageWithMetadata? value)
    {
        if (value != null)
        {
            HandleSelectedTilesetChange(value);
        }
    }

    partial void OnSelectedGridChanged(AreaOfInterest? value)
    {
        if (value is not null)
        {
            _ = _selectedThumbnailPerGrid.TryGetValue(value, out ImageWithMetadata? img);
            SelectedTileSetThumbnail = img;
            UpdateActiveViewVM(value.BoundingRectangle.IntersectsWith(ActiveView?.BoundingRectangle ?? LengthRectangle.Zero) ? ActiveView : null);
            OnPropertyChanged(nameof(NavigationImage));
        }
    }

    private ObservableCollection<ImageWithMetadata> GetThumbnailsFromCurrentGrid()
    {
        if (SelectedGrid is not null)
        {
            return [.. TileSetThumbnails.Where(img =>
            {
                var rect = GetImageLengthRectangle(img);
                return SelectedGrid.Overlaps(rect);
            })];
        }
        return [];
    }

    private void TileSetThumbnails_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(AvailableTileSetThumbnails));
        OnPropertyChanged(nameof(ShowAvailableTileSetThumbnails));
        SelectedTileSetThumbnail = AvailableTileSetThumbnails.LastOrDefault();
    }

    private void InitializeSelectedThumbnails()
    {
        foreach (AreaOfInterest grid in AvailableGrids)
        {
            _ = _selectedThumbnailPerGrid.TryAdd(grid, TileSetThumbnails.Where(img => IsImageFromGrid(img, grid)).FirstOrDefault());
        }
    }

    private string CreateActivityName([CallerMemberName] string methodName = "")
        => $"{nameof(StageOverviewViewModel)}::{methodName}";
}
