using System.Windows.Controls;
using System.Windows.Input;

namespace Betrian.WPF.CeitecStyles.Controls;

public class ScrollSlider : Slider
{
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        int delta = e.Delta;
        if (delta > 0)
        {
            if (IncreaseLarge?.CanExecute(null, this) is true)
            {
                IncreaseLarge?.Execute(null, this);
            }
        }
        else
        {
            if (DecreaseLarge?.CanExecute(null, this) is true)
            {
                DecreaseLarge?.Execute(null, this);
            }
        }

        base.OnMouseWheel(e);
    }
}
