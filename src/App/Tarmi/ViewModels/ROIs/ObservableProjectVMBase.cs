using Tarmi.Projects.Implementation;

using CommunityToolkit.Mvvm.ComponentModel;

namespace Tarmi.App.ViewModels.ROIs;

public abstract partial class ObservableProjectVMBase : ObservableObject
{
    protected readonly ObservableProject _observableProject;

    protected ObservableProjectVMBase(ObservableProject observableProject)
    {
        _observableProject = observableProject;
    }

    [ObservableProperty]
    private string _name = string.Empty;

    partial void OnNameChanged(string value)
        => NameChanged(value);

    virtual protected void NameChanged(string value) { }
}
