using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Tarmi.App.ViewModels.ROIs;
public record class RoiChildBehaviors
{
    public bool HasContextMenu { get; init; } = true;
    public bool SupportsRemoveCommand { get; init; }
    public bool HasMarkAsReferenceMenu { get; init; }
    public bool CanHaveReferenceAttribute { get; init; }
    public bool CanEditFiducials { get; init; }
    public bool CanEditCorrelationOptions { get; init; }
    public bool CanExportToMaps { get; init; }
    public bool CanEditMilling { get; init; }
    public bool CanBindCorrelation { get; init; }
    public bool CanRegenerateMipImage { get; init; }
}
