using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Tarmi.App.Controls;

public class PromptDialog : Window
{
    private Button? _cancelButton;
    private TextBox? _inputTextBox;
    private Button? _okButton;

    public PromptDialog()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }

    public override void OnApplyTemplate()
    {
        if (_okButton != null)
        {
            _okButton.Click -= OkClick;
        }

        if (_cancelButton != null)
        {
            _cancelButton.Click -= CancelClick;
        }

        _okButton = GetTemplateChild("PART_Ok") as Button;
        _cancelButton = GetTemplateChild("PART_Cancel") as Button;
        _inputTextBox = GetTemplateChild("PART_Input") as TextBox;

        if (_okButton != null)
        {
            _okButton.Click += OkClick;
        }

        if (_cancelButton != null)
        {
            _cancelButton.Click += CancelClick;
        }

        base.OnApplyTemplate();
    }

    private void CancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OkClick(object sender, RoutedEventArgs e)
    {
        if (_inputTextBox != null && Validation.GetErrors(_inputTextBox).Any())
        {
            return;
        }

        DialogResult = true;
        Close();
    }
}


public static class Prompt
{
    public static bool ShowDialog<T>(string prompt, T defaultValue, Window? owner, out T result)
    {
        PromptViewModel<T> promptViewModel = new()
        {
            Prompt = prompt,
            Value = defaultValue
        };

        PromptDialog dialog = new()
        {
            Owner = owner,
            DataContext = promptViewModel
        };

        if (dialog.ShowDialog() is true)
        {
            result = promptViewModel.Value;
            return true;
        }
        else
        {
            result = default!;
            return false;
        }
    }

    private sealed class PromptViewModel<T>
    {
        public required string Prompt { get; set; }
        public T? Value { get; set; }
    }
}
