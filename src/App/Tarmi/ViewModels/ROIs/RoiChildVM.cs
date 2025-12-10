using System.Diagnostics.CodeAnalysis;
using System.IO;
using Tarmi.App.Infrastructure;
using Tarmi.Projects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Tarmi.App.ViewModels.ROIs;

public abstract partial class RoiChildVM : ObservableProjectVMBase
{
    public RoiVM RoiVM { get; }

    public VirtualChildVM? ParentVM { get; }

    public RoiChildBehaviors Behaviors { get; }

    [MemberNotNullWhen(true, nameof(ParentVM))]
    public bool HasParent
        => ParentVM is not null;

    public int NestingLevel { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
    public partial bool CanBeRemoved { get; protected set; }

    public virtual CorrelationInfo CorrelationInfo
        => throw new NotSupportedException();

    public Guid FiducialsGroupId
        => CorrelationInfo?.FiducialsGroupId ?? Guid.Empty;

    public bool IsBound => FiducialsGroupId.IsEmpty();

    public virtual bool IsBindable { get; } = false;

    public RoiChildVM GetRoot()
    {
        var root = this;
        while (root.HasParent)
        {
            root = root.ParentVM;
        }
        return root;
    }

    [ObservableProperty]
    public partial bool CanBeMarkedAsReference { get; protected set; }

    protected RoiChildVM(RoiVM roiVM, VirtualChildVM? parentVM, RoiChildBehaviors behaviors)
        : base(roiVM.ObservableProject)
    {
        RoiVM = roiVM;
        ParentVM = parentVM;
        Behaviors = behaviors;
        CanBeRemoved = behaviors.HasMarkAsReferenceMenu;
        CanBeMarkedAsReference = behaviors.HasMarkAsReferenceMenu;
        NestingLevel = GetNestingLevel(this);
    }

    private static int GetNestingLevel(RoiChildVM? child)
        => child?.ParentVM is null ? 0 : 1 + GetNestingLevel(child.ParentVM);

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

        RemoveFiles();
        await RemoveFromTree();
    }

    protected bool RemovePreCheckImplementation()
    {
        if (!CorrelationInfo.IsReferenceImage || CorrelationInfo.FiducialPoints.Count == 0)
        {
            return true;
        }
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
        return confirmed;
    }

    public virtual Task RemoveFromTree() => Task.Run(() => RoiVM.RemoveChild(this));

    public virtual void RemoveFiles() { }

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
