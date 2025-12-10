using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using UnitsNet;

namespace Tarmi.Configuration.Application;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record FocusFunctions
{
    [DataMember]
    public Ratio CenterFocusAreaSize { get; init; } = Ratio.FromPercent(25);

    [DataMember]
    public Ratio FocusRangeRatio { get; init; } = Ratio.FromPercent(5);

    [DataMember]
    public Length SEMFocusRange { get; init; }

    [DataMember]
    public Length SEMDefaultWorkingDistance { get; init; } = Length.FromMillimeters(4);

    [DataMember]
    public Length SEMAutoFocusStep { get; init; } = Length.FromMillimeters(0.1);

    [DataMember]
    public Length FIBFocusRange { get; init; }
    
    [DataMember]
    public Length FIBDefaultWorkingDistance { get; init; } = Length.FromMillimeters(17);

    [DataMember]
    public Length FIBAutoFocusStep { get; init; } = Length.FromMillimeters(0.15);
    
    [DataMember]
    public Length LMFocusRange { get; init; }
}
