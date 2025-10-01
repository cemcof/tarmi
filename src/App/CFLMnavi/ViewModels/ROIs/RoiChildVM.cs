using System.IO;
using Betrian.App.Infrastructure;
using Betrian.CflmNavi.App.Infrastructure;
using CFLMnavi.Projects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Betrian.CflmNavi.App.ViewModels.ROIs;

public class RoiChildBehaviors
{
    public bool HasContextMenu { get; init; } = true;
    public bool SupportsRemoveCommand { get; init; }
    public bool CanBeMarkedAsReference { get; init; }
    public bool CanEditFiducials { get; init; }
    public bool CanEditCorrelationOptions { get; init; }
    public bool CanExportToMaps { get; init; }
    public bool CanEditMilling { get; init; }
    public bool CanBindCorrelation { get; init; }
}

public abstract partial class RoiChildVM : ObservableProjectVMBase
{
    protected readonly RoiChildBehaviors _behaviors;

    public RoiVM RoiVM { get; }

    public VirtualChildVM? ParentVM { get; }

    public bool HasParent
        => ParentVM is not null;

    public bool HasContextMenu
        => _behaviors.HasContextMenu;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
    public partial bool CanBeRemoved { get; protected set; }

    public virtual CorrelationInfo CorrelationInfo
        => throw new NotSupportedException();

    public Guid FiducialsGroupId
        => CorrelationInfo?.FiducialsGroupId ?? Guid.Empty;

    [ObservableProperty]
    public partial bool CanBeMarkedAsReference { get; protected set; }

    protected RoiChildVM(RoiVM roiVM, VirtualChildVM? parentVM, RoiChildBehaviors behaviors)
        : base(roiVM.ObservableProject)
    {
        RoiVM = roiVM;
        ParentVM = parentVM;
        _behaviors = behaviors;
        CanBeRemoved = behaviors.CanBeMarkedAsReference;
        CanBeMarkedAsReference = behaviors.CanBeMarkedAsReference;
    }

    public virtual Guid SortId => Guid.Empty;

    public virtual void OnModeDeInitialized()
    {
    }

    [RelayCommand(CanExecute = nameof(CanBeRemoved))]
    public async Task Remove()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity("RoiChildVM.Remove");

        if (!CanBeRemoved)
        {
            return;
        }

        if (!RemovePreCheckImplementation())
        {
            return;
        }

        using var winGuard = RoiVM.Parent.WindowService.ShowBusyMessage(Messages.RemovingImageDataBusyMessage);

        await Task.Run(async () =>
            await RemoveImplementation()
        );
    }

    protected bool RemovePreCheckImplementation()
    {
        if (CorrelationInfo.IsReferenceImage && CorrelationInfo.FiducialPoints.Count > 0)
        {
            var confirmed = RoiVM.Parent.WindowService.ShowConfirmationDialog(
                title: "Removing reference image",
                message: """
                         This image is a reference image and has fiducial points.
                         Removing it will remove all fiducial points from all images belonging to active ROI.
                         Are you sure you want to remove it?
                         """,
                acceptText: "Remove",
                rejectText: "Cancel"
            );
            if (!confirmed)
            {
                return false;
            }
        }
        return true;
    }

    public virtual Task RemoveImplementation()
        => Task.CompletedTask;

    protected virtual bool CanExecuteMakeReference()
        => CanBeMarkedAsReference;

    [RelayCommand(CanExecute = nameof(CanExecuteMakeReference))]
    private async Task MakeReference()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity("RoiChildVM.MakeReference");

        if (CanBeMarkedAsReference)
        {
            var changingReference = RoiVM.Parent.ImagesStateManager.WouldSettingReferenceCauseChanges(this);
            if (changingReference)
            {
                var confirmed = RoiVM.WindowService.ShowConfirmationDialog(
                    title: "Changing reference image",
                    message: """
                             Changing reference image will trigger removal of all fiducial points from all images belonging to active ROI.
                             Are you sure you want to change it?
                             """,
                    acceptText: "Change",
                    rejectText: "Cancel"
                    );
                if (!confirmed)
                {
                    return;
                }
            }

            using var winGuard = RoiVM.Parent.WindowService.ShowBusyMessage(Messages.ChangingReferenceImageBusyMessage);

            await Task.Run(async () =>
            {
                _observableProject.SetReference(CorrelationInfo);
                await RoiVM.Parent.ImagesStateManager.OnReferenceChanged();
                RoiVM.Update();
            });
        }
    }

    protected virtual void ExportToMapsImplementation(string baseExportDirectory)
    {
    }

    [RelayCommand]
    private async Task ExportToMaps()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity("RoiChildVM.ExportToMaps");

        using (RoiVM.Parent.WindowService.ShowBusyMessage(Messages.ExportingToMapsBusyMessage))
        {
            var baseExportDirectory = Path.Combine(
                RoiVM.Parent.ActiveProject?.ProjectDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "MapsExports"
            );
            await RoiVM.Parent.Logger.SwallowAsync(
                 Task.Run(
                    () => ExportToMapsImplementation(baseExportDirectory)
                ),
                "Export to MAPS failed"
            );
        }
    }

}
