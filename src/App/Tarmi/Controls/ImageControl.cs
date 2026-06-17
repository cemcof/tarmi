using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenCvSharp.WpfExtensions;
using Tarmi.Imaging.Common;

namespace Tarmi.App.Controls;

public partial class ImageControl : ContentControl
{
    public static readonly DependencyProperty ImageScaleProperty = DependencyProperty.Register(nameof(ImageScale), typeof(double), typeof(ImageControl), new PropertyMetadata(1.0));
    public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(ImageControl), new PropertyMetadata(ImageSourceChanged));
    public static readonly DependencyProperty IsZoomEnabledProperty = DependencyProperty.Register(nameof(IsZoomEnabled), typeof(bool), typeof(ImageControl), new PropertyMetadata(true));
    public static readonly DependencyProperty MouseDragBeginCommandProperty = DependencyProperty.Register(nameof(MouseDragBeginCommand), typeof(ICommand), typeof(ImageControl), new PropertyMetadata());
    public static readonly DependencyProperty MouseDragEndCommandProperty = DependencyProperty.Register(nameof(MouseDragEndCommand), typeof(ICommand), typeof(ImageControl), new PropertyMetadata());
    public static readonly DependencyProperty MouseDragMoveCommandProperty = DependencyProperty.Register(nameof(MouseDragMoveCommand), typeof(ICommand), typeof(ImageControl), new PropertyMetadata());
    public static readonly DependencyProperty MouseClickCommandProperty = DependencyProperty.Register(nameof(MouseClickCommand), typeof(ICommand), typeof(ImageControl), new PropertyMetadata());
    public static readonly DependencyProperty TransformProperty = DependencyProperty.Register(nameof(Transform), typeof(Matrix), typeof(ImageControl), new FrameworkPropertyMetadata(Matrix.Identity, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(ImageControl), new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender, OnZoomChanged, CoerceZoomValue));
    public static readonly DependencyProperty ImageWithMetadataProperty = DependencyProperty.Register(nameof(ImageWithMetadata), typeof(ImageWithMetadata), typeof(ImageControl), new PropertyMetadata(null, ImageChanged));
    public static readonly DependencyProperty IsScaleBarVisibleProperty = DependencyProperty.Register(nameof(IsScaleBarVisible), typeof(bool), typeof(ImageControl), new PropertyMetadata(true));
    public static readonly DependencyProperty IsCenterCrossVisibleProperty = DependencyProperty.Register(nameof(IsCenterCrossVisible), typeof(bool), typeof(ImageControl), new PropertyMetadata(false));
    public static readonly DependencyProperty MinZoomProperty = DependencyProperty.Register(nameof(MinZoom), typeof(double), typeof(ImageControl), new PropertyMetadata(0.01, MinZoomChanged));
    public static readonly DependencyProperty MaxZoomProperty = DependencyProperty.Register(nameof(MaxZoom), typeof(double), typeof(ImageControl), new PropertyMetadata(100.0, MaxZoomChanged));

    private static object CoerceZoomValue(DependencyObject d, object baseValue)
    {
        if (d is ImageControl imageControl && baseValue is double value)
        {
            return imageControl.CoerceZoomValue(value);
        }
        return baseValue;
    }

    private double CoerceZoomValue(double zoom) => Math.Clamp(zoom, MinZoom, MaxZoom);


    private static void MaxZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ImageControl imageControl)
        {
            return;
        }
        if (imageControl.MinZoom > imageControl.MaxZoom)
        {
            imageControl.MinZoom = imageControl.MaxZoom;
        }
        if (imageControl.Zoom > imageControl.MaxZoom)
        {
            imageControl.Zoom = imageControl.MaxZoom;
        }
    }

    private static void MinZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ImageControl imageControl)
        {
            return;
        }
        if (imageControl.MaxZoom < imageControl.MinZoom)
        {
            imageControl.MaxZoom = imageControl.MinZoom;
        }
        if (imageControl.Zoom < imageControl.MinZoom)
        {
            imageControl.Zoom = imageControl.MinZoom;
        }
    }
   

    private const double MinimumVisiblePx = 50;

    private FrameworkElement? _content;
    private Image? _image;
    private bool _isDragging;
    private Point _lastDragPoint;
    private Point _firstDragPoint;
    private MatrixTransform? _transform;
    private ScaleBar? _scaleBar;
    private bool _isZoomingByMouse;

    public double MinZoom
    {
        get => (double)GetValue(MinZoomProperty);
        set => SetValue(MinZoomProperty, value);
    }

    public double MaxZoom
    {
        get => (double)GetValue(MaxZoomProperty);
        set => SetValue(MaxZoomProperty, value);
    }

    public double ImageScale
    {
        get => (double)GetValue(ImageScaleProperty);
        set => SetValue(ImageScaleProperty, value);
    }

    public ImageSource? ImageSource
    {
        get => (ImageSource)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    public bool IsZoomEnabled
    {
        get => (bool)GetValue(IsZoomEnabledProperty);
        set => SetValue(IsZoomEnabledProperty, value);
    }

    public ICommand MouseDragBeginCommand
    {
        get => (ICommand)GetValue(MouseDragBeginCommandProperty);
        set => SetValue(MouseDragBeginCommandProperty, value);
    }

    public ICommand MouseDragEndCommand
    {
        get => (ICommand)GetValue(MouseDragEndCommandProperty);
        set => SetValue(MouseDragEndCommandProperty, value);
    }

    public ICommand MouseDragMoveCommand
    {
        get => (ICommand)GetValue(MouseDragMoveCommandProperty);
        set => SetValue(MouseDragMoveCommandProperty, value);
    }

    public ICommand MouseClickCommand
    {
        get => (ICommand)GetValue(MouseClickCommandProperty);
        set => SetValue(MouseClickCommandProperty, value);
    }

    public Matrix Transform
    {
        get => (Matrix)GetValue(TransformProperty);
        set => SetValue(TransformProperty, value);
    }

    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public ImageWithMetadata ImageWithMetadata
    {
        get => (ImageWithMetadata)GetValue(ImageWithMetadataProperty);
        set => SetValue(ImageWithMetadataProperty, value);
    }

    public bool IsScaleBarVisible
    {
        get => (bool)GetValue(IsScaleBarVisibleProperty);
        set => SetValue(IsScaleBarVisibleProperty, value);
    }

    public bool IsCenterCrossVisible
    {
        get => (bool)GetValue(IsCenterCrossVisibleProperty);
        set => SetValue(IsCenterCrossVisibleProperty, value);
    }

    static ImageControl()
    {
        EventManager.RegisterClassHandler(typeof(ImageControl), Mouse.LostMouseCaptureEvent, new MouseEventHandler(OnLostMouseCapture));
    }

    public Point GetOffset(Visual visual)
    {
        return _image?.TransformToVisual(visual).Transform(new Point()) ?? new Point();
    }

    public BitmapSource GetRenderedImage(double renderScale = 1)
    {
        SetZoom(1);
        UpdateLayout();

        double scale = Transform.M11;

        Size size = new(ActualWidth / scale * renderScale, ActualHeight / scale * renderScale);

        BitmapCacheBrush brush = new(this)
        {
            BitmapCache = new()
            {
                SnapsToDevicePixels = true,
                EnableClearType = true,
                RenderAtScale = 2 * renderScale / scale
            }
        };

        Border border = new()
        {
            Width = size.Width,
            Height = size.Height,
            Background = brush
        };

        border.Arrange(new Rect(size));
        border.UpdateLayout();

        RenderTargetBitmap result = new((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
        result.Render(border);
        return result;
    }

    public override void OnApplyTemplate()
    {
        if (_content is not null)
        {
            _content.PreviewMouseUp -= OnMouseLeftButtonUp;
            _content.PreviewMouseDown -= OnMouseLeftButtonDown;
            _content.PreviewMouseWheel -= OnMouseWheel;
            _content.PreviewMouseMove -= OnMouseMove;
        }

        if (_image is not null)
        {
            _image.SizeChanged -= ImageSizeChanged;
        }

        _transform = GetTemplateChild("PART_Transform") as MatrixTransform;
        _content = GetTemplateChild("PART_Content") as FrameworkElement;
        _image = GetTemplateChild("PART_Image") as Image;
        _scaleBar = GetTemplateChild("PART_ScaleBar") as ScaleBar;


        if (_content is not null)
        {
            _content.PreviewMouseUp += OnMouseLeftButtonUp;
            _content.PreviewMouseDown += OnMouseLeftButtonDown;
            _content.PreviewMouseWheel += OnMouseWheel;
            _content.PreviewMouseMove += OnMouseMove;
        }

        if (_image is not null)
        {
            _image.SizeChanged += ImageSizeChanged;
        }
    }

    public void SetZoom(double zoom)
    {
        if (!_isZoomingByMouse && _transform is not null && _image is not null && _content is not null)
        {
            Zoom = zoom;

            Matrix matrix = _transform.Matrix;
            matrix.ScaleAtPrepend(zoom, zoom, _image.ActualWidth / 2, _image.ActualHeight / 2);
            CoercePan(ref matrix, _content.RenderSize);
            _transform.Matrix = matrix;

            UpdateTransform();
        }
    }

    private static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ImageControl imageControl)
        {
            imageControl.ZoomChanged((double)e.NewValue, (double)e.OldValue);
        }
    }

    private void ZoomChanged(double zoom, double previous)
    {
        if (!_isZoomingByMouse && _transform is not null && _image is not null && _content is not null)
        {
            if (zoom == 1)
            {
                _transform.Matrix = Matrix.Identity;
            }
            else
            {
                double scale = zoom / previous;
                Matrix matrix = _transform.Matrix;
                matrix.ScaleAtPrepend(scale, scale, 0.5 * _image.ActualWidth, 0.5 * _image.ActualHeight);
                CoercePan(ref matrix, _content.RenderSize);
                _transform.Matrix = matrix;
            }
            UpdateTransform();
        }
    }

    private static void CoercePan(ref Matrix matrix, Size renderSize)
    {
        double maxX = renderSize.Width - MinimumVisiblePx;
        double maxY = renderSize.Height - MinimumVisiblePx;
        double minX = -renderSize.Width * matrix.M11 + MinimumVisiblePx;
        double minY = -renderSize.Height * matrix.M22 + MinimumVisiblePx;

        matrix.OffsetX = Math.Clamp(matrix.OffsetX, minX, maxX);
        matrix.OffsetY = Math.Clamp(matrix.OffsetY, minY, maxY);
    }

    private static void OnLostMouseCapture(object sender, MouseEventArgs e)
    {
        if (sender is ImageControl imageControl && e.Source == imageControl && imageControl._content is not null && imageControl._isDragging)
        {
            if (imageControl._content.IsMouseCaptured)
            {
                imageControl._content.ReleaseMouseCapture();
            }
            imageControl._content.Cursor = null;
            imageControl._isDragging = false;
        }
    }

    private static void ImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ImageControl imageControl)
        {
            imageControl.UpdateTransform();
        }
    }

    private void ImageSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateTransform();
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Point mousePos = e.GetPosition(_content);
        _lastDragPoint = mousePos;
        _firstDragPoint = mousePos;
        if (Mouse.LeftButton == MouseButtonState.Pressed && _content is not null)
        {
            _content.Cursor = Cursors.SizeAll;
            _isDragging = true;
            _ = _content.CaptureMouse();
            e.Handled = true;
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging && _content is not null)
        {
            _content.Cursor = null;
            _content.ReleaseMouseCapture();
            _isDragging = false;

            Point point = e.GetPosition(_image);
            Point descaledPoint = new(point.X / ImageScale, point.Y / ImageScale);

            if (IsClick() && MouseClickCommand?.CanExecute(descaledPoint) == true)
            {
                MouseClickCommand.Execute(descaledPoint);
            }

            _lastDragPoint = new();
            _firstDragPoint = new();
            e.Handled = true;
        }
    }

    private bool IsClick()
    {
        return Math.Abs(_lastDragPoint.X - _firstDragPoint.X) <= SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(_lastDragPoint.Y - _firstDragPoint.Y) <= SystemParameters.MinimumVerticalDragDistance;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging && Mouse.LeftButton == MouseButtonState.Pressed && _transform is not null && _content is not null)
        {
            Point mousePos = e.GetPosition(_content);
            var offset = mousePos - _lastDragPoint;

            Matrix matrix = _transform.Matrix;
            matrix.Translate(offset.X, offset.Y);
            CoercePan(ref matrix, _content.RenderSize);
            _transform.Matrix = matrix;
            UpdateTransform();

            _lastDragPoint = mousePos;
            e.Handled = true;
        }
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (IsZoomEnabled && _transform is not null && _image is not null && _content is not null)
        {
            try
            {
                _isZoomingByMouse = true;
                Point mousePosition = Mouse.GetPosition(_image);
                double zoomFactor = e.Delta >= 0 ? 1.1 : (1 / 1.1);

                var newZoom = CoerceZoomValue(Zoom * zoomFactor);
                zoomFactor = newZoom / Zoom;
                Zoom = newZoom;
                Matrix matrix = _transform.Matrix;
                matrix.ScaleAtPrepend(zoomFactor, zoomFactor, mousePosition.X, mousePosition.Y);
                CoercePan(ref matrix, _content.RenderSize);
                _transform.Matrix = matrix;
                UpdateTransform();
                e.Handled = true;
            }
            finally
            {
                _isZoomingByMouse = false;
            }
        }
    }

    private void UpdateTransform()
    {
        if (_image is not null && ImageSource is not null && _transform is not null)
        {
            double sourceWidth = ImageSource.Width;
            double displayWidth = _image.ActualWidth;
            double scale = displayWidth / sourceWidth;

            Matrix scaledTransform = _transform.Matrix;
            scaledTransform.ScalePrepend(scale, scale);
            Transform = scaledTransform;
            ImageScale = displayWidth / sourceWidth;
        }
    }

    private static void ImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ImageControl imageControl)
        {
            imageControl.ImageChanged(e.NewValue as ImageWithMetadata);
        }
    }

    private void ImageChanged(ImageWithMetadata? imageWithMetadata)
    {
        if (imageWithMetadata is null)
        {
            ImageSource = null;
            return;
        }

        ImageSource = imageWithMetadata.Image.Mat.ToBitmapSource();
        UpdateTransform();

        if (_scaleBar != null)
        {
            var pixelSize = imageWithMetadata.GetPixelSize();
            _scaleBar.PixelSize = pixelSize.X.Meters;
            _scaleBar.PixelSizeUnit = "m";
        }
    }
}
