using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Tarmi.WPF.CeitecStyles.Controls;

public class HistogramPolygon : Shape
{
    public static readonly DependencyProperty HistogramProperty =
        DependencyProperty.Register(
            nameof(Histogram),
            typeof(SortedDictionary<int, double>),
            typeof(HistogramPolygon),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender,
                OnHistogramChanged));

    private Geometry _definingGeometry = Geometry.Empty;

    public SortedDictionary<int, double> Histogram
    {
        get => (SortedDictionary<int, double>)GetValue(HistogramProperty);
        set => SetValue(HistogramProperty, value);
    }

    protected override Geometry DefiningGeometry => _definingGeometry;

    private static void OnHistogramChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HistogramPolygon polygon)
        {
            polygon.UpdateGeometry();
        }
    }

    private void UpdateGeometry()
    {
        SortedDictionary<int, double>? histogram = Histogram;
        if (histogram is null || histogram.Count <= 2)
        {
            _definingGeometry = Geometry.Empty;
            return;
        }

        StreamGeometry geometry = new();

        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(new Point(0, 1), isFilled: true, isClosed: true);

            using IEnumerator<double> values = histogram.Values.GetEnumerator();
            if (!values.MoveNext())
            {
                _definingGeometry = Geometry.Empty;
                return;
            }

            context.LineTo(new Point(0, 1 - values.Current), isStroked: true, isSmoothJoin: false);

            int pointCount = histogram.Count - 1;
            if (pointCount > 0)
            {
                Point[] points = new Point[pointCount];
                int x = 1;
                int i = 0;

                while (values.MoveNext())
                {
                    points[i++] = new Point(x++, 1 - values.Current);
                }

                context.PolyLineTo(points, isStroked: true, isSmoothJoin: false);
            }

            context.LineTo(new Point(histogram.Count - 1, 1), isStroked: true, isSmoothJoin: false);
        }

        geometry.Freeze();
        _definingGeometry = geometry;
    }
}
