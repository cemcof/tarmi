using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using OpenCvSharp.WpfExtensions;
using Tarmi.App.Services.Application;
using Tarmi.App.ViewModels.Navigation;
using Tarmi.Configuration.Holders;
using Tarmi.Devices.Thermofisher.Instrument.Types;
using Tarmi.Imaging.Common;
using Tarmi.Models;
using Tarmi.Projects;
using Tarmi.Projects.Implementation;
using Tarmi.VirtualDevices;
using Tarmi.WPF;

namespace Tarmi.App.ViewModels;

public partial class OverviewImageViewModel : ViewModelBase
{
    private const double PixelRadius = 140;

    private readonly IStageNavigation _stageNavigation;
    private readonly IProjectManager _projectManager;
    private readonly IWindowService _windowService;
    private readonly ISafeStageControlling _safeStageControlling;
    private IDisposable? _activeProjectSubscription;
    private IDisposable? _activeViewSubscription = null;
    private IDisposable? _regionsOfInterestSubscription = null;
    private readonly Dictionary<AreaOfInterest, ImageWithMetadata?> _selectedThumbnailPerGrid = [];

    public List<AreaOfInterest> AvailableGrids => ActiveProject?.Holder.Grids ?? [];

    [ObservableProperty]
    public partial StageState StageState { get; set; } = StageState.Zero;
    [ObservableProperty]
    public partial ActiveView? ActiveView { get; set; }
    [ObservableProperty]
    public partial ActiveViewVM? ActiveViewVM { get; set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AvailableTileSetThumbnails))]
    [NotifyPropertyChangedFor(nameof(SelectedTileSetThumbnail))]
    [NotifyPropertyChangedFor(nameof(ShowAvailableTileSetThumbnails))]
    public partial AreaOfInterest? SelectedGrid { get; set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AvailableGrids))]
    public partial ObservableProject? ActiveProject { get; set; }
    [ObservableProperty]
    public partial bool OverviewEdit { get; set; }
    [ObservableProperty]
    public partial ImageWithMetadata? SelectedTileSetThumbnail { get; set; }
    public ObservableCollection<ImageWithMetadata> TileSetThumbnails { get; } = [];

    public ObservableCollection<ImageWithMetadata> AvailableTileSetThumbnails => new(GetThumbnailsFromGrid(SelectedGrid));

    public bool ShowAvailableTileSetThumbnails => AvailableTileSetThumbnails.Any();

    [ObservableProperty]
    public partial NavigationImageVM? NavigationImage { get; set; }

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
        if (SelectedGrid is null)
        {
            return;
        }
        SelectedTileSetThumbnail = null;
        ImageWithMetadata? image = null;
        _ = _selectedThumbnailPerGrid.AddOrUpdate(SelectedGrid, image, (_, _) => null);
        NavigationImage = null;
    }

    protected override Task InitializeCoreAsync()
    {
        _activeProjectSubscription = _projectManager.ActiveProject.Subscribe(HandleActiveProjectChange);
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
        _activeProjectSubscription?.Dispose();
        _activeProjectSubscription = null;
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
        if (SelectedTileSetThumbnail is null)
        {
            ClearNavigationImage();
        }
        else if (ActiveProject is not null)
        {
            HandleRegionsOfInterestChange(ActiveProject.RegionsOfInterest);
            HandleSelectedTilesetChange(SelectedTileSetThumbnail);
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
        _regionsOfInterestSubscription?.Dispose();

        ActiveProject = project;
        SelectedGrid = AvailableGrids.FirstOrDefault();

        _navigationRoisSource.Clear();
        NavigationImage = null;
        TileSetThumbnails.Clear();

        if (ActiveProject is null)
        {
            return;
        }
        _activeViewSubscription = ActiveProject.ActiveView.Subscribe(HandleActiveViewChange);

        _regionsOfInterestSubscription =
            ActiveProject
                .RegionsOfInterestObservable
                .Subscribe(_ =>
                {
                    HandleRegionsOfInterestChange(ActiveProject.RegionsOfInterest);
                    UpdateTileSetThumbnails();
                });
        HandleRegionsOfInterestChange(ActiveProject.RegionsOfInterest);
        UpdateTileSetThumbnails();
    }

    private void HandleActiveViewChange(ActiveView view)
    {
        ActiveView = view;
        UpdateActiveViewVM(view);
    }

    private void UpdateActiveViewVM(ActiveView? view)
    {
        if (SelectedGrid is null || view is null)
        {
            ActiveViewVM = null;
            return;
        }
        GetStageCenterAndPixelFactor(out LengthPoint center, out double pixelMeterFactor);

        ActiveViewVM = new()
        {
            X = (view.Center.X - center.X - view.Size.Width / 2).Meters * pixelMeterFactor + PixelRadius,
            Y = (view.Center.Y - center.Y - view.Size.Height / 2).Meters * pixelMeterFactor + PixelRadius,
            Width = view.Size.Width.Meters * pixelMeterFactor,
            Height = view.Size.Height.Meters * pixelMeterFactor
        };
    }

    private void HandleRegionsOfInterestChange(IEnumerable<RegionOfInterest> rois)
    {
        _navigationRoisSource.Clear();

        if (SelectedGrid is null || ActiveProject is null)
        {
            return;
        }
        GetStageCenterAndPixelFactor(out LengthPoint center, out double pixelMeterFactor);

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

    private void HandleSelectedTilesetChange(ImageWithMetadata? image)
    {
        NavigationImage = null;

        if (ActiveProject is null || SelectedGrid is null || image is null)
        {
            return;
        }
        foreach (var grid in _selectedThumbnailPerGrid.Keys)
        {
            if (IsImageFromGrid(image, grid))
            {
                _selectedThumbnailPerGrid[grid] = image;
            }
        }

        try
        {
            if (!IsImageFromGrid(image, SelectedGrid))
            {
                return;
            }
            var metadata = image.Coordinates;

            BitmapSource bitmapSource = image.Image.Mat.ToBitmapSource();
            PixelSize pixelSize = image.GetPixelSize();

            var realImageWidth = bitmapSource.Width * pixelSize.X;
            var realImageHeight = bitmapSource.Height * pixelSize.Y;

            var stagePosition = image.GetStagePosition();
            var planePosition = _stageNavigation.GetPlanePosition(stagePosition, image.GetSource());
            GetStageCenterAndPixelFactor(out LengthPoint center, out double pixelMeterFactor);
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
        catch
        {

        }
    }

    private void GetStageCenterAndPixelFactor(out LengthPoint stageCenter, out double pixelMeterFactor)
    {
        var (center, radius) = SelectedGrid switch
        {
            CircleAreaOfInterest cg => (cg.Center, cg.Radius),
            _ => throw new NotSupportedException("Only circular grids are supported.")
        };
        pixelMeterFactor = PixelRadius / radius.Meters;

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

        var idsAndPathsOf3DTilesetThumbnails = ActiveProject.RegionsOfInterest
            .SelectMany(roi => roi.TileSets3D)
            .Select<TileSet3DDescriptor, (Guid Id, string Path)>(tileset3D =>
            {
                var content = tileset3D.StitchedImageThumbnail;
                var path = ActiveProject.GetContentFilePath(tileset3D, content);
                return (tileset3D.Id, path);
            });

        var idsAndPathsOfTilesetThumbnails = ActiveProject.RegionsOfInterest
            .SelectMany(roi => roi.TileSets)
            .Select<TileSetDescriptor, (Guid Id, string Path)>(tileset =>
            {
                var layerContent = tileset.StitchedImageThumbnail;
                var path = ActiveProject.GetContentFilePath(tileset, layerContent);
                return (tileset.Id, path);
            });

        var allThumbnails = idsAndPathsOfTilesetThumbnails
            .Concat(idsAndPathsOf3DTilesetThumbnails)
            .OrderBy(pair => pair.Id)
            .Select(pair => TiffImage.Load(pair.Path))
            .ToArray();

        var removeImageIds = TileSetThumbnails
            .Select(t => t.ImageId)
            .Except(allThumbnails.Select(t => t.ImageId))
            .ToArray();

        var addImageIds = allThumbnails
            .Select(t => t.ImageId)
            .Except(TileSetThumbnails.Select(t => t.ImageId))
            .ToArray();

        TileSetThumbnails.Remove(TileSetThumbnails.Where(tilesetThumbnail => removeImageIds.Contains(tilesetThumbnail.ImageId)).ToArray());
        TileSetThumbnails.AddRange(allThumbnails.Where(t => addImageIds.Contains(t.ImageId)));

        var selectedId = SelectedTileSetThumbnail?.ImageId;
        if (!selectedId.HasValue || removeImageIds.Contains(selectedId.Value))
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
        HandleSelectedTilesetChange(value);
    }

    partial void OnSelectedGridChanged(AreaOfInterest? value)
    {
        if (value is null)
        {
            return;
        }
        _ = _selectedThumbnailPerGrid.TryGetValue(value, out ImageWithMetadata? img);
        if (img is null || !TileSetThumbnails.Contains(img))
        {
            img = GetThumbnailsFromGrid(value).LastOrDefault();
            _selectedThumbnailPerGrid[value] = null;
        }
        SelectedTileSetThumbnail = img;
        UpdateActiveViewVM(value.BoundingRectangle.IntersectsWith(ActiveView?.BoundingRectangle ?? LengthRectangle.Zero) ? ActiveView : null);
        OnPropertyChanged(nameof(NavigationImage));
    }

    private IEnumerable<ImageWithMetadata> GetThumbnailsFromGrid(AreaOfInterest? grid)
    {
        return grid is null ? [] : TileSetThumbnails
            .Where(img =>
            {
                var rect = GetImageLengthRectangle(img);
                return grid.Overlaps(rect);
            });
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

    public void UpdateTilesets()
    {
        if (ActiveProject is null)
        {
            return;
        }
        HandleRegionsOfInterestChange(ActiveProject.RegionsOfInterest);
        UpdateTileSetThumbnails();
    }
}
