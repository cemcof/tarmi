using System.ComponentModel;
using System.Windows;

namespace Betrian.WPF;

public class DesignTimeResourceDictionary : ResourceDictionary
{
    private string? _designTimeSource;

    public string? DesignTimeSource
    {
        get => _designTimeSource;

        set
        {
            _designTimeSource = value;
            if ((bool)DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue && _designTimeSource != null)
            {
                base.Source = new Uri(_designTimeSource);
            }
        }
    }

    public new Uri Source
    {
        get
        {
            throw new InvalidOperationException("Use DesignTimeSource instead Source!");
        }

        set
        {
            throw new InvalidOperationException("Use DesignTimeSource instead Source!");
        }
    }
}
