using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors.Core;

namespace Tarmi.App.Controls;

internal class SpinBox : RangeBase
{
    public ICommand IncrementCommand { get; }
    public ICommand DecrementCommand { get; }

    public SpinBox()
    {
        IncrementCommand = new ActionCommand(() => UpdateValue(Value + SmallChange));
        DecrementCommand = new ActionCommand(() => UpdateValue(Value - SmallChange));
    }

    private void UpdateValue(double value) => Value = double.Clamp(value, Minimum, Maximum);
}
