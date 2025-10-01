using System.Windows;
using System.Windows.Controls;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace Betrian.CflmNavi.App.Controls;

internal class MatThumbnail : Image
{
    public static readonly DependencyProperty MatProperty = DependencyProperty.Register(nameof(Mat), typeof(Mat), typeof(MatThumbnail), new PropertyMetadata(null, OnMatChanged));

    public Mat Mat
    {
        get => (Mat)GetValue(MatProperty);
        set => SetValue(MatProperty, value);
    }

    private static void OnMatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = d as MatThumbnail;
        control?.UpdateImageSource();
    }

    private void UpdateImageSource()
    {
        if (Mat == null || Mat.Empty())
        {
            Source = null;
            return;
        }
        Source = Mat.ToWriteableBitmap();
    }
}
