using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace Tarmi.App.Controls;

[ContentProperty(nameof(Content))]
public partial class ScrollDragZoomControl : ContentControl
{
    public static readonly DependencyProperty IsZoomEnabledProperty = DependencyProperty.Register(nameof(IsZoomEnabled), typeof(bool), typeof(ScrollDragZoomControl), new PropertyMetadata(true));

    public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(nameof(Scale), typeof(double), typeof(ScrollDragZoomControl),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender, OnScaleChanged, CoerceScale));

    private const double MinimumVisiblePx = 50;

    private ContentPresenter? content;
    private Grid? grid;
    private Point? lastDragPoint;
    private MatrixTransform? transform;

    public bool IsZoomEnabled
    {
        get => (bool)GetValue(IsZoomEnabledProperty);
        set => SetValue(IsZoomEnabledProperty, value);
    }

    public double Scale
    {
        get => (double)GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public override void OnApplyTemplate()
    {
        if (grid != null)
        {
            grid.MouseLeftButtonUp -= OnMouseLeftButtonUp;
            grid.MouseLeftButtonDown -= OnMouseLeftButtonDown;
            grid.MouseWheel -= OnMouseWheel;
            grid.MouseMove -= OnMouseMove;
            grid.ManipulationDelta -= OnTouchDelta;
            grid.ManipulationInertiaStarting -= OnInertia;
        }

        transform = GetTemplateChild("PART_Transform") as MatrixTransform;
        grid = GetTemplateChild("PART_Grid") as Grid;
        content = GetTemplateChild("PART_Content") as ContentPresenter;

        if (grid != null)
        {
            grid.MouseLeftButtonUp += OnMouseLeftButtonUp;
            grid.MouseLeftButtonDown += OnMouseLeftButtonDown;
            grid.MouseWheel += OnMouseWheel;
            grid.MouseMove += OnMouseMove;
            grid.ManipulationDelta += OnTouchDelta;
            grid.ManipulationInertiaStarting += OnInertia;
        }
    }

    private static void CoercePan(ref Matrix matrix, Size renderSize)
    {
        double maxX = renderSize.Width - MinimumVisiblePx;
        double maxY = renderSize.Height - MinimumVisiblePx;
        double minX = -(renderSize.Width * matrix.M11 - MinimumVisiblePx);
        double minY = -(renderSize.Height * matrix.M22 - MinimumVisiblePx);

        matrix.OffsetX = Math.Clamp(matrix.OffsetX, minX, maxX);
        matrix.OffsetY = Math.Clamp(matrix.OffsetY, minY, maxY);
    }

    private static object CoerceScale(DependencyObject d, object baseValue)
    {
        if (baseValue is double scale)
        {
            return Math.Clamp(scale, 0.001, 1000);
        }
        return baseValue;
    }

    private static void OnScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollDragZoomControl control
            && e.OldValue is double oldValue
            && e.NewValue is double newValue
            && control.transform is not null)
        {
            Matrix matrix = control.transform.Matrix;
            control.Zoom(ref matrix, newValue, oldValue, null);
            control.transform.Matrix = matrix;
        }
    }

    private void OnInertia(object? sender, ManipulationInertiaStartingEventArgs e)
    {
        e.TranslationBehavior.DesiredDeceleration = 0.1;
        e.ExpansionBehavior.DesiredDeceleration = 0.01;
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (grid is not null)
        {
            Point mousePos = e.GetPosition(grid);
            lastDragPoint = mousePos;
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (grid is not null)
        {
            grid.Cursor = null;
            lastDragPoint = null;
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (lastDragPoint.HasValue && Mouse.LeftButton == MouseButtonState.Pressed && transform is not null && content is not null && grid is not null)
        {
            grid.Cursor = Cursors.SizeAll;
            Point posNow = e.GetPosition(grid);

            double dX = posNow.X - lastDragPoint.Value.X;
            double dY = posNow.Y - lastDragPoint.Value.Y;

            lastDragPoint = posNow;

            Matrix matrix = transform.Matrix;
            matrix.Translate(dX, dY);
            CoercePan(ref matrix, content.RenderSize);
            transform.Matrix = matrix;
            e.Handled = true;
        }
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (IsZoomEnabled && transform is not null)
        {
            Point mousePosition = Mouse.GetPosition(content);
            double scaleMultiplier = e.Delta >= 0 ? 1.1 : (1 / 1.1);
            double newScale = Scale * scaleMultiplier;
            Matrix matrix = transform.Matrix;
            Zoom(ref matrix, newScale, Scale, mousePosition);
            transform.Matrix = matrix;

            e.Handled = true;
        }
    }

    private void OnTouchDelta(object? sender, ManipulationDeltaEventArgs e)
    {
        if (transform is not null && content is not null && grid is not null)
        {
            Vector translation = e.DeltaManipulation.Translation;
            double scaleMultiplier = e.DeltaManipulation.Scale.X;

            Matrix matrix = transform.Matrix;
            matrix.Translate(translation.X, translation.Y);

            double newScale = Scale * scaleMultiplier;
            Zoom(ref matrix, newScale, Scale, e.ManipulationOrigin);
            transform.Matrix = matrix;
        }
    }

    private void Zoom(ref Matrix matrix, double newScale, double oldScale, Point? zoomCenter)
    {
        if (content is not null && newScale >= 0.2 && newScale <= 100)
        {
            double scaleMultiplier = newScale / oldScale;
            if (zoomCenter is Point center)
            {
                matrix.ScaleAtPrepend(scaleMultiplier, scaleMultiplier, center.X, center.Y);
            }
            else
            {
                if (newScale == 1.0)
                {
                    matrix = Matrix.Identity;
                }
                else
                {
                    matrix.ScaleAtPrepend(scaleMultiplier, scaleMultiplier, content.RenderSize.Width / 2, content.RenderSize.Height / 2);
                }
            }
            Scale = newScale;
            CoercePan(ref matrix, content.RenderSize);
        }
    }
}
