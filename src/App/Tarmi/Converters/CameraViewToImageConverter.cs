using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Tarmi.Models;

namespace Tarmi.App.Converters;

[ValueConversion(typeof(StageCameraView), typeof(object))]
public class CameraViewToImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is StageCameraView stageCameraView)
        {
            return stageCameraView switch
            {
                StageCameraView.SEM => Application.Current.FindResource("icon_atom_white"),
                StageCameraView.FIB_Milling => Application.Current.FindResource("icon_ion"),
                StageCameraView.FIB_RightAngle => Application.Current.FindResource("icon_ion"),
                StageCameraView.LM => Application.Current.FindResource("light_bulb_white"),
                StageCameraView.Confocal => Application.Current.FindResource("light_bulb_white"),
                StageCameraView.Unknown => DependencyProperty.UnsetValue,
                _ => DependencyProperty.UnsetValue
            };
        }
        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException("Cannot convert back");
}
