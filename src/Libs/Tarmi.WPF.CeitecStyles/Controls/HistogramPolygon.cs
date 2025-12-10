using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Tarmi.WPF.CeitecStyles.Controls;

public class HistogramPolygon : Shape
{
    public static readonly DependencyProperty HistogramProperty = DependencyProperty.Register(nameof(Histogram), typeof(SortedDictionary<int, double>), typeof(HistogramPolygon), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender, OnHistogramChanged));

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
        if (Histogram is null || Histogram.Count <= 2)
        {
            return;
        }

        PathFigure path = new()
        {
            StartPoint = new Point(0, 1)
        };

        path.Segments.Add(new LineSegment(new Point(0, 1 - Histogram[0]), false));

        PolyLineSegment curve = new();
        for (int i = 1; i < Histogram.Count; i++)
        {
            curve.Points.Add(new Point(i, 1 - Histogram[i]));
        }
        path.Segments.Add(curve);

        path.Segments.Add(new LineSegment(new Point(Histogram.Count - 1, 1), false));
        path.Segments.Add(new LineSegment(new Point(0, 1), false));

        path.IsClosed = true;

        PathGeometry geometry = new();
        geometry.Figures.Add(path);
        _definingGeometry = geometry;
    }
}
