using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UnitsNet;

namespace Betrian.CflmNavi.App.ViewModels.ROIs;

public partial class CorrelationOptionsViewModel : ObservableObject
{
    internal ImageChildVM ImageChild { get; }

    public CorrelationOptionsViewModel(ImageChildVM imageChild)
    {
        ImageChild = imageChild;
        Opacity = ImageChild.CorrelationInfo.Opacity.Percent;
    }

    [ObservableProperty]
    public partial double OpacityMin { get; private set; } = 0.0;

    [ObservableProperty]
    public partial double OpacityMax { get; private set; } = 100.0;

    [ObservableProperty]
    public partial double OpacityStep { get; private set; } = 5.0;

    [ObservableProperty]
    public partial double Opacity { get; set; }

    [RelayCommand]
    private async Task SetOpacity()
    {
        await Task.Run(async () =>
        {
            await ImageChild.UpdateOpacitySettings(Ratio.FromPercent(Opacity));
        });
    }

    [RelayCommand]
    private void Close()
    {
        ImageChild.CloseCorrelationsOptionsCommand.Execute(null);
    }
}
