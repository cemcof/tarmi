using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Tarmi.WPF.CeitecStyles.Controls;

public class EditableTextBlock : Control
{
    public static readonly DependencyProperty IsInEditModeProperty = DependencyProperty.Register(nameof(IsInEditMode), typeof(bool), typeof(EditableTextBlock), new PropertyMetadata(false));
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(EditableTextBlock), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    private TextBox? _textBox;

    public bool IsInEditMode
    {
        get => (bool)GetValue(IsInEditModeProperty);
        set => SetValue(IsInEditModeProperty, value);
    }

    public string? Text
    {
        get => GetValue(TextProperty) as string;
        set => SetValue(TextProperty, value);
    }

    public override void OnApplyTemplate()
    {
        if (_textBox != null)
        {
            _textBox.LostFocus -= OnLostFocus;
        }
        _textBox = GetTemplateChild("PART_TextBox") as TextBox;
        if (_textBox != null)
        {
            _textBox.Focus();
            _textBox.LostFocus += OnLostFocus;
            _textBox.TextChanged += TextChanged;
        }
        base.OnApplyTemplate();
    }

    private void TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_textBox != null)
        {
            _textBox.TextChanged -= TextChanged;
            _textBox.SelectAll();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (IsInEditMode && DidEditFinished(e.Key))
        {
            IsInEditMode = false;
        }
        base.OnKeyDown(e);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            IsInEditMode = true;
        }
        base.OnMouseLeftButtonDown(e);
    }

    private bool DidEditFinished(Key key)
    {
        switch (key)
        {
            case Key.Enter:
            case Key.Tab:
                UpdateBinding();
                return true;

            case Key.Escape:
                return true;

            default:
                return false;
        }
    }

    private void UpdateBinding()
    {
        var bindingExpression = _textBox?.GetBindingExpression(TextBox.TextProperty);
        bindingExpression?.UpdateSource();
    }

    private void OnLostFocus(object sender, RoutedEventArgs e)
    {
        IsInEditMode = false;
    }
}
