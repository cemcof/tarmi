using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Betrian.Models;
using Betrian.Models.Serialization;
using UnitsNet;

namespace CFLMnavi.Projects;

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
