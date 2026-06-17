using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace Tarmi.WPF;

// TODO: An abstract baseclass implementing this interface may reduce code duplication,
//       however it may require changes in ViewModel registration.
public interface IDialogViewModel : IViewModel
{
    public event EventHandler<bool>? CloseRequested;

    IRelayCommand CancelCommand { get; }
}
