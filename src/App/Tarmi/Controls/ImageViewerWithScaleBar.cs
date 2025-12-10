using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Tarmi.Imaging.Common;
using OpenCvSharp.WpfExtensions;

namespace Tarmi.App.Controls;

public class ImageViewerWithScaleBar : ContentControl
{
    public static readonly DependencyProperty ImageScaleProperty = DependencyProperty.Register(nameof(ImageScale), typeof(double), typeof(ImageViewerWithScaleBar), new PropertyMetadata(1.0));
    public static readonly DependencyProperty ImageWithMetadataProperty = DependencyProperty.Register(nameof(ImageWithMetadata), typeof(ImageWithMetadata), typeof(ImageViewerWithScaleBar), new PropertyMetadata(null, ImageChanged));

    public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(nameof(Scale), typeof(double), typeof(ImageViewerWithScaleBar), new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    private static readonly DependencyPropertyKey ImagePropertyKey = DependencyProperty.RegisterReadOnly(nameof(Image), typeof(ImageSource), typeof(ImageViewerWithScaleBar), new PropertyMetadata());
    public static readonly DependencyProperty ImageProperty = ImagePropertyKey.DependencyProperty;

    public static readonly DependencyProperty IsScaleBarVisibleProperty = DependencyProperty.Register(nameof(IsScaleBarVisible), typeof(bool), typeof(ImageViewerWithScaleBar), new PropertyMetadata(true, IsScaleBarVisibleChanged));

    private Image? _imageControl;
    private ScaleBar? _scaleBar;

    public ImageSource? Image
    {
        get => (ImageSource)GetValue(ImagePropertyKey.DependencyProperty);
        private set => SetValue(ImagePropertyKey, value);
    }

    public double ImageScale
    {
        get => (double)GetValue(ImageScaleProperty);
        set => SetValue(ImageScaleProperty, value);
    }

    public ImageWithMetadata ImageWithMetadata
    {
        get => (ImageWithMetadata)GetValue(ImageWithMetadataProperty);
        set => SetValue(ImageWithMetadataProperty, value);
    } 
    
    public double Scale
    {
        get => (double)GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public bool IsScaleBarVisible
    {
        get => (bool)GetValue(IsScaleBarVisibleProperty);
        set => SetValue(IsScaleBarVisibleProperty, value);
    }

    public ImageViewerWithScaleBar()
    {
        SizeChanged += OnSizeChanged;
    }

    public override void OnApplyTemplate()
    {
        if (_imageControl != null)
        {
            _imageControl.SizeChanged -= ImageControl_SourceUpdated;
        }

        _scaleBar = GetTemplateChild("PART_ScaleBar") as ScaleBar;
        _imageControl = GetTemplateChild("PART_Image") as Image;

        if (_imageControl != null)
        {
            _imageControl.SizeChanged += ImageControl_SourceUpdated;
        }

        OnSizeChanged(this, EventArgs.Empty);
        base.OnApplyTemplate();
    }

    private static void ImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ImageViewerWithScaleBar imageViewerWithScaleBar)
        {
            imageViewerWithScaleBar.ImageChanged(e.NewValue as ImageWithMetadata);
        }
    }

    private static void IsScaleBarVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ImageViewerWithScaleBar imageViewerWithScaleBar)
        {
            imageViewerWithScaleBar.IsScaleBarVisibleChanged((bool)e.NewValue);
        }
    }

    private void ImageControl_SourceUpdated(object? sender, EventArgs e)
    {
        OnSizeChanged(this, EventArgs.Empty);
    }

    private void IsScaleBarVisibleChanged(bool isVisible)
    {
        if (_scaleBar != null)
        {
            _scaleBar.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void ImageChanged(ImageWithMetadata? imageWithMetadata)
    {
        if (imageWithMetadata is null)
        {
            Image = null;
            return;
        }

        Image = imageWithMetadata.Image.Mat.ToBitmapSource();
        OnSizeChanged(this, EventArgs.Empty);

        if (_scaleBar != null)
        {
            var pixelSize = imageWithMetadata.GetPixelSize();
            _scaleBar.PixelSize = pixelSize.X.Meters;
            _scaleBar.PixelSizeUnit = "m";
        }
    }

    private void OnSizeChanged(object sender, EventArgs e)
    {
        if (_imageControl != null && _imageControl.Source != null)
        {
            ImageScale = _imageControl.ActualWidth / _imageControl.Source.Width;
        }
    }
}
