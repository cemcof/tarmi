using Tarmi.Devices.Thermofisher.Instrument.ObjectModel.Abstractions;
using Fei.XT.Instrument.gen;
using Fei.XT.ViewServer.gen;

namespace Tarmi.Devices.Thermofisher.Instrument.ObjectModel;

internal static class ViewServerExtensions
{
    public static DualBeamDataSource GetDualBeamDataSource(this View view)
    {
        var datasource = view.Datasources["DualBeam"];
        return (DualBeamDataSource)datasource;
    }

    public static DualBeamDataSource GetDualBeamDataSource(this IXtObjectHandle<View> view)
    {
        return view.Object.GetDualBeamDataSource();
    }

    public static Beam GetBeamType(this Beams beams, enDBBeamType beamType)
    {
        return beamType switch
        {
            enDBBeamType.enElectronBeam => beams.ElectronBeam,
            enDBBeamType.enIonBeam => beams.IonBeam,
            enDBBeamType.enOpticalBeam => beams.OpticalBeam,
            _ => beams.InfraredBeam
        };
    }

}
