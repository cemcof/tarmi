using Betrian.Devices.Thermofisher.Instrument.ObjectModel.Abstractions;
using CFLMnavi.Configuration;
using CFLMnavi.Configuration.Devices;
using Microsoft.Extensions.Logging;

namespace Betrian.Devices.Thermofisher.Instrument.ObjectModel;

internal sealed class SemImageSource : ImageSourceBase
{
    private static ImageSourceDefinitions CreateImageSourceDefinitions(ThermofisherInstrument instrument) => new()
    {
        BeamType = Fei.XT.Instrument.gen.enDBBeamType.enElectronBeam,
        ViewType = GetViewName(instrument.SemQuad),
        CompositeImageEventsClientId = 1221
    };

    public SemImageSource(ILogger<SemImageSource> logger, IXtObjectsCollection xtObjectsCollection, ApplicationConfig appConfig)
        : base(logger, xtObjectsCollection, CreateImageSourceDefinitions(appConfig.Microscope.ThermofisherInstrument))
    {
        xtObjectsCollection.ConnectObjects();
    }

    public override Task AutoContrastBrightness(CancellationToken cancellationToken) => throw new NotImplementedException();
    public override Task AutoFocus(CancellationToken cancellationToken) => throw new NotImplementedException();
    public override Task AutoStigmation(CancellationToken cancellationToken) => throw new NotImplementedException();
}
