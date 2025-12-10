using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using Tarmi.App.Controls;

namespace Tarmi.App.ViewModels.ROIs;

public abstract partial class VirtualChildVM : RoiChildVM
{
    protected readonly List<RoiChildVM> _children = [];

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    public IReadOnlyList<RoiChildVM> Children => _children;

    public ImageAttributes Attributes { get; protected set; }

    protected VirtualChildVM(RoiVM roiVM, VirtualChildVM? parentVM, RoiChildBehaviors behaviors)
        : base(roiVM, parentVM, behaviors)
    {
    }

    public void UpdateAttributes()
    {
        foreach (var child in _children.ToImmutableArray())
        {
            if (child is VirtualChildVM vChild)
            {
                vChild.UpdateAttributes();
            }
            else if (child is ImageChildVM iChild)
            {
                iChild.UpdateAttributes();
            }
        }
    }

    public IEnumerable<ImageChildVM> GetImageChildrenVMs(Func<ImageChildVM, bool> selectionPredicate)
    {
        foreach (var child in _children)
        {
            if (child is ImageChildVM imageChild)
            {
                if (selectionPredicate(imageChild))
                {
                    yield return imageChild;
                }
            }
            else if (child is VirtualChildVM virtualChild)
            {
                var nestedChildren = virtualChild.GetImageChildrenVMs(selectionPredicate);
                foreach (var nestedChild in nestedChildren)
                {
                    yield return nestedChild;
                }
            }
        }
    }

    protected async Task DeselectAllVisibleImages()
    {
        // untoggle in correct order
        var secondarySelected =
            GetImageChildrenVMs(imageVM => imageVM.SelectionType == ImageSelection.Secondary)
                .ToImmutableArray();
        foreach (var img in secondarySelected)
        {
            if (img.SelectionType == ImageSelection.Secondary)
            {
                // could change during the loop
                await img.ToggleVisibilityCommand.ExecuteAsync(null);
            }
            
        }

        var overlaysSelected =
            GetImageChildrenVMs(imageVM => imageVM.SelectionType == ImageSelection.Overlay)
                .ToImmutableArray();
        foreach (var img in overlaysSelected)
        {
            if (img.SelectionType == ImageSelection.Overlay)
            {
                // could change during the loop
                await img.ToggleVisibilityCommand.ExecuteAsync(null);
            }
        }

        var primarySelected =
            GetImageChildrenVMs(imageVM => imageVM.SelectionType == ImageSelection.Primary)
                .ToImmutableArray();
        foreach (var img in primarySelected)
        {
            if (img.SelectionType == ImageSelection.Primary)
            {
                // could change during the loop
                await img.ToggleVisibilityCommand.ExecuteAsync(null);
            }
        }
    }
}
