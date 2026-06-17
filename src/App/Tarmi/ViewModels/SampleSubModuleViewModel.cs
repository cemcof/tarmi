using Tarmi.WPF;

using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.Extensions.Logging;

namespace Tarmi.App.ViewModels;

public partial class SampleSubModuleViewModel : ViewModelBase
{
    //private readonly ILogger<SampleSubModuleViewModel> _logger;

    [ObservableProperty]
    private string _name;

    public SampleSubModuleViewModel(ILogger<SampleSubModuleViewModel> logger)
    {
        Name = "Bound data";
        //_logger = logger;
    }

    //protected override Task DeInitializeCoreAsync()
    //{
    //    return base.DeInitializeCoreAsync();
    //}

    //protected override Task InitializeCoreAsync()
    //{
    //    return base.InitializeCoreAsync();
    //}
}
