using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Tarmi.Models;
using Tarmi.Models.Serialization;
using UnitsNet;

namespace Tarmi.Projects;

[DataContract(Namespace = Helpers.AppNamespace)]
public record FocusPoint
{
    [DataMember]
    public LengthPoint PlaneLocation { get; init; } = LengthPoint.Zero;

    [DataMember]
    public Length? Z { get; init; }

    [DataMember]
    public Length? WorkingDistance { get; init; }

    [IgnoreDataMember]
    [MemberNotNullWhen(true, nameof(Z))]
    public bool IsAutofocused => Z.HasValue;
}
