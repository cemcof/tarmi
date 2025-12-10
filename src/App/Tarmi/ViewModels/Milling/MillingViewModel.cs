using System.Collections.ObjectModel;
using Tarmi.Models;
using Tarmi.Projects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using UnitsNet;
using Tarmi.App.ViewModels.ROIs;

namespace Tarmi.App.ViewModels.Milling;

public partial class MillingViewModel : ObservableObject
{
    public ObservableCollection<MillingAreaInfo> MillingAreas { get; } = [];
    public ObservableCollection<MillingArea> MillingAreasOverlay { get; } = [];
    public SingleImageChildVM ImageVM { get; }

    public MillingViewModel(SingleImageChildVM singleImageChildVM)
    {
        ImageVM = singleImageChildVM;
    }

    public void UpdateMillingAreasOverlay()
    {
        if (ImageVM.Content is LayerContentDescriptorWithCorrelationInfo ciContent)
        {
            MillingAreasOverlay.Clear();
            MillingAreas.Clear();
            MillingAreas.AddRange(ciContent.MillingAreas);

            foreach (var item in ciContent.MillingAreas)
            {
                var imageSize = ImageVM.ImageMetadata.Coordinates.ImageSize;

                MillingAreasOverlay.Add(new MillingArea()
                {
                    X = item.Definition.Left.DecimalFractions * imageSize.Width,
                    Y = item.Definition.Top.DecimalFractions * imageSize.Height,
                    Width = (item.Definition.Right - item.Definition.Left).DecimalFractions * imageSize.Width,
                    Height = (item.Definition.Bottom - item.Definition.Top).DecimalFractions * imageSize.Height,
                    Label = item.Name,
                    MillingAreaInfo = item,
                    OnResizeFinished = UpdateMillingArea
                });
            }
        }
    }

    [RelayCommand]
    private void AddMillingArea()
    {
        var millingAreaInfo = new MillingAreaInfo()
        {
            Name = $"MillingArea {ImageVM.MillingAreasCount + 1}",
            Definition = new RatioRectangle()
            {
                Left = Ratio.FromDecimalFractions(0.4),
                Right = Ratio.FromDecimalFractions(0.6),
                Top = Ratio.FromDecimalFractions(0.4),
                Bottom = Ratio.FromDecimalFractions(0.6)
            }
        };

        ImageVM.AddMillingArea(millingAreaInfo);
    }

    [RelayCommand]
    private void Close()
    {
        ImageVM.CloseCorrelationsOptionsCommand.Execute(null);
    }

    [RelayCommand]
    private void RemoveMillingArea(MillingAreaInfo millingAreaInfo)
    {
        ImageVM.RemoveMillingArea(millingAreaInfo);
    }

    private void UpdateMillingArea(Rectangle area)
    {
        if (area is MillingArea { MillingAreaInfo: MillingAreaInfo rawMillingArea } millingArea)
        {
            ImageVM.UpdateMillingArea(rawMillingArea, millingArea);
        }
    }
}
