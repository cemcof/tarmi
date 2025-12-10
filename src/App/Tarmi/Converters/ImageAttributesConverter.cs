using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Tarmi.App.ViewModels.ROIs;

namespace Tarmi.App.Converters;

[ValueConversion(typeof(ImageSource), typeof(UIElement[]))]
internal class ImageAttributesConverter : IValueConverter
{
    private const int ImageHeight = 16;
    private const int ImageWidth = 16;
    private static readonly Thickness ImageMargin = new() { Left = 0, Top = 0, Right = 2, Bottom = 0 };
    private static readonly EllipseGeometry Circle = new() { RadiusX = 3, RadiusY = 3 };

    private static readonly SolidColorBrush RedFluorescenceBrush = (SolidColorBrush)Application.Current.FindResource("FluorescenceRedBrush");
    private static readonly SolidColorBrush GreenFluorescenceBrush = (SolidColorBrush)Application.Current.FindResource("FluorescenceGreenBrush");
    private static readonly SolidColorBrush BlueFluorescenceBrush = (SolidColorBrush)Application.Current.FindResource("FluorescenceBlueBrush");
    private static readonly SolidColorBrush UltravioletFluorescenceBrush = (SolidColorBrush)Application.Current.FindResource("FluorescenceUltravioletBrush");

    private static readonly SolidColorBrush RedReflectionBrush = (SolidColorBrush)Application.Current.FindResource("ReflectionRedBrush");
    private static readonly SolidColorBrush GreenReflectionBrush = (SolidColorBrush)Application.Current.FindResource("ReflectionGreenBrush");
    private static readonly SolidColorBrush BlueReflectionBrush = (SolidColorBrush)Application.Current.FindResource("ReflectionBlueBrush");
    private static readonly SolidColorBrush UltravioletReflectionBrush = (SolidColorBrush)Application.Current.FindResource("ReflectionUltravioletBrush");

    private static readonly GeometryDrawing RedFluorescenceDot = new() { Geometry = Circle, Brush = RedFluorescenceBrush };
    private static readonly GeometryDrawing GreenFluorescenceDot = new() { Geometry = Circle, Brush = GreenFluorescenceBrush };
    private static readonly GeometryDrawing BlueFluorescenceDot = new() { Geometry = Circle, Brush = BlueFluorescenceBrush };
    private static readonly GeometryDrawing UltravioletFluorescenceDot = new() { Geometry = Circle, Brush = UltravioletFluorescenceBrush };

    private static readonly GeometryDrawing RedReflectionDot = new() { Geometry = Circle, Brush = RedReflectionBrush };
    private static readonly GeometryDrawing GreenReflectionDot = new() { Geometry = Circle, Brush = GreenReflectionBrush };
    private static readonly GeometryDrawing BlueReflectionDot = new() { Geometry = Circle, Brush = BlueReflectionBrush };
    private static readonly GeometryDrawing UltravioletReflectionDot = new() { Geometry = Circle, Brush = UltravioletReflectionBrush };

    private static Image CreateReferenceIcon() => new() { Source = (ImageSource)Application.Current.FindResource("icon_ref_image3_white"), Margin = ImageMargin, Width = ImageWidth, Height = ImageHeight };
    private static Image CreateTileSetIcon() => new() { Source = (ImageSource)Application.Current.FindResource("icon_stage_white"), Margin = ImageMargin, Width = ImageWidth, Height = ImageHeight };
    private static Image CreateZStackIcon() => new() { Source = (ImageSource)Application.Current.FindResource("icon_zstack_3layers_white"), Margin = ImageMargin, Width = ImageWidth, Height = ImageHeight };
    private static Image CreateMipImageIcon() => new() { Source = (ImageSource)Application.Current.FindResource("icon_mip_image"), Margin = ImageMargin, Width = ImageWidth, Height = ImageHeight };
    private static Image CreateSemIcon() => new() { Source = (ImageSource)Application.Current.FindResource("icon_atom_white"), Margin = ImageMargin, Width = ImageWidth, Height = ImageHeight };
    private static Image CreateFibIcon() => new() { Source = (ImageSource)Application.Current.FindResource("icon_ion"), Margin = ImageMargin, Width = ImageWidth, Height = ImageHeight };
    private static Image CreateLuminescenceIcon() => new() { Source = (ImageSource)Application.Current.FindResource("light_bulb_white"), Margin = ImageMargin, Width = ImageWidth, Height = ImageHeight };
    private static Image CreateReflectionIcon() => new() { Source = (ImageSource)Application.Current.FindResource("icon_reflection_1beam_white"), Margin = ImageMargin, Width = ImageWidth, Height = ImageHeight };
    private static Image CreateFluorescenceRedLightIcon() => new() { Source = new DrawingImage(RedFluorescenceDot), Margin = ImageMargin, Width = ImageWidth, Height = ImageHeight };
    private static Image CreateFluorescenceGreenLightIcon() => new() { Source = new DrawingImage(GreenFluorescenceDot), Margin = ImageMargin, Width = ImageWidth, Height = ImageHeight };
    private static Image CreateFluorescenceBlueLightIcon() => new() { Source = new DrawingImage(BlueFluorescenceDot), Margin = ImageMargin, Width = ImageWidth, Height = ImageHeight };
    private static Image CreateFluorescenceUltravioletLightIcon() => new() { Source = new DrawingImage(UltravioletFluorescenceDot), Margin = ImageMargin, Width = ImageWidth, Height = ImageHeight };
    private static Image CreateReflectionRedLightIcon() => new() { Source = new DrawingImage(RedReflectionDot), Margin = ImageMargin, Width = ImageWidth, Height = ImageHeight };
    private static Image CreateReflectionGreenLightIcon() => new() { Source = new DrawingImage(GreenReflectionDot), Margin = ImageMargin, Width = ImageWidth, Height = ImageHeight };
    private static Image CreateReflectionBlueLightIcon() => new() { Source = new DrawingImage(BlueReflectionDot), Margin = ImageMargin, Width = ImageWidth, Height = ImageHeight };
    private static Image CreateReflectionUltravioletLightIcon() => new() { Source = new DrawingImage(UltravioletReflectionDot), Margin = ImageMargin, Width = ImageWidth, Height = ImageHeight };
    private static Image CreateConfocalIcon() => new() { Source = (ImageSource)Application.Current.FindResource("icon_confocal1_white"), Margin = ImageMargin, Width = ImageWidth, Height = ImageHeight };
    private static Image CreateFolderIcon() => new() { Source = (ImageSource)Application.Current.FindResource("folder"), Margin = ImageMargin, Width = ImageWidth, Height = ImageHeight };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ImageAttributes attributes)
        {
            throw new NotSupportedException();
        }

        List<UIElement> result = [];

        // first reference mark
        if (attributes.HasFlag(ImageAttributes.Reference)) { result.Add(CreateReferenceIcon()); }

        // then folder if applicable
        if (attributes.HasFlag(ImageAttributes.Folder)) { result.Add(CreateFolderIcon()); }

        // first grouping info tileset/z-stack
        if (attributes.HasFlag(ImageAttributes.TileSet)) { result.Add(CreateTileSetIcon()); }
        if (attributes.HasFlag(ImageAttributes.ZStack)) { result.Add(CreateZStackIcon()); }
        if (attributes.HasFlag(ImageAttributes.MipImage)) { result.Add(CreateMipImageIcon()); }

        // source
        if (attributes.HasFlag(ImageAttributes.SEM)) { result.Add(CreateSemIcon()); }
        if (attributes.HasFlag(ImageAttributes.FIB)) { result.Add(CreateFibIcon()); }
        if (attributes.HasFlag(ImageAttributes.Luminescence))
        {
            result.Add(CreateLuminescenceIcon());

            if (attributes.HasFlag(ImageAttributes.Red)) { result.Add(CreateFluorescenceRedLightIcon()); }
            if (attributes.HasFlag(ImageAttributes.Green)) { result.Add(CreateFluorescenceGreenLightIcon()); }
            if (attributes.HasFlag(ImageAttributes.Blue)) { result.Add(CreateFluorescenceBlueLightIcon()); }
            if (attributes.HasFlag(ImageAttributes.UltraViolet)) { result.Add(CreateFluorescenceUltravioletLightIcon()); }
        }
        if (attributes.HasFlag(ImageAttributes.Reflection))
        {
            result.Add(CreateReflectionIcon());

            if (attributes.HasFlag(ImageAttributes.Red)) { result.Add(CreateReflectionRedLightIcon()); }
            if (attributes.HasFlag(ImageAttributes.Green)) { result.Add(CreateReflectionGreenLightIcon()); }
            if (attributes.HasFlag(ImageAttributes.Blue)) { result.Add(CreateReflectionBlueLightIcon()); }
            if (attributes.HasFlag(ImageAttributes.UltraViolet)) { result.Add(CreateReflectionUltravioletLightIcon()); }
        }
        if (attributes.HasFlag(ImageAttributes.Confocal)) { result.Add(CreateConfocalIcon()); }

        // colors if applicable

        return result.ToArray();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
