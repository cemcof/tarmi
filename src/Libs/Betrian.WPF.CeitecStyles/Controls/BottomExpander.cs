using System.Windows;
using System.Windows.Controls;

namespace Betrian.WPF.CeitecStyles.Controls;

public class BottomExpander : ContentControl
{
    public static readonly DependencyProperty NeedsClippingProperty = DependencyProperty.Register(nameof(NeedsClipping), typeof(bool), typeof(BottomExpander), new PropertyMetadata(false));
    public static readonly DependencyProperty SecondaryContentProperty = DependencyProperty.Register(nameof(SecondaryContent), typeof(object), typeof(BottomExpander), new PropertyMetadata());

    private ContentPresenter? _mainContentPresenter;
    private ContentPresenter? _secondaryContentPresenter;

    public bool NeedsClipping
    {
        get => (bool)GetValue(NeedsClippingProperty);
        set => SetValue(NeedsClippingProperty, value);
    }

    public object SecondaryContent
    {
        get => GetValue(SecondaryContentProperty);
        set => SetValue(SecondaryContentProperty, value);
    }

    public override void OnApplyTemplate()
    {
        if (_mainContentPresenter != null)
        {
            _mainContentPresenter.SizeChanged -= UpdateClipping;
        }
        if (_secondaryContentPresenter != null)
        {
            _secondaryContentPresenter.SizeChanged -= UpdateClipping;
        }

        _mainContentPresenter = GetTemplateChild("PART_ContentPresenter") as ContentPresenter;
        _secondaryContentPresenter = GetTemplateChild("PART_SecondaryContentPresenter") as ContentPresenter;

        if (_mainContentPresenter != null)
        {
            _mainContentPresenter.SizeChanged += UpdateClipping;
        }
        if (_secondaryContentPresenter != null)
        {
            _secondaryContentPresenter.SizeChanged += UpdateClipping;
        }
        base.OnApplyTemplate();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        UpdateNeedsClipping();
    }

    private void UpdateClipping(object sender, SizeChangedEventArgs e)
    {
        UpdateNeedsClipping();
    }

    private void UpdateNeedsClipping()
    {
        NeedsClipping = ActualHeight < ((_mainContentPresenter?.ActualHeight ?? 0) + (_secondaryContentPresenter?.ActualHeight ?? 0));
    }
}
