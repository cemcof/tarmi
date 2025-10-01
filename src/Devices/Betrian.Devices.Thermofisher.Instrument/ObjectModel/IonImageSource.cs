using Betrian.Devices.Thermofisher.Instrument.ObjectModel.Abstractions;
using Betrian.Models;
using CFLMnavi.Configuration;
using CFLMnavi.Configuration.Devices;
using Fei.XT.Instrument.gen;
using Fei.XT.ViewServer.gen;
using Microsoft.Extensions.Logging;
using static Betrian.Devices.Thermofisher.Instrument.ObjectModel.PathLiterals.Instrument.Alignments.Optics;

namespace Betrian.Devices.Thermofisher.Instrument.ObjectModel;

internal sealed class IonImageSource : ImageSourceBase
{
    private readonly IXtObjectHandle<PatternDataSource> _patternDataSource;
    private static ImageSourceDefinitions CreateImageSourceDefinitions(ThermofisherInstrument instrument) => new()
    {
        BeamType = Fei.XT.Instrument.gen.enDBBeamType.enIonBeam,
        ViewType = GetViewName(instrument.IonQuad),
        CompositeImageEventsClientId = 1222
    };

    public IonImageSource(ILogger<SemImageSource> logger, IXtObjectsCollection xtObjectsCollection, ApplicationConfig appConfig)
        : base(logger, xtObjectsCollection, CreateImageSourceDefinitions(appConfig.Microscope.ThermofisherInstrument))
    {
        var viewType = GetViewName(appConfig.Microscope.ThermofisherInstrument.IonQuad);
        _patternDataSource = xtObjectsCollection.GetPatterningDataSource(viewType);
        xtObjectsCollection.ConnectObjects();
    }

    private static void RemoveAllPatterns(PatternListEx patternList)
    {
        if (patternList.Count > 0)
        {
            var idsToRemove = (from IPatternShapeEx pattern in patternList! select pattern.ID).ToList();
            idsToRemove.ForEach(patternList.Remove);
        }
    }

    public void ClearMillingDefinitions()
    {
        try
        {
            var patternList = _patternDataSource.Object.PatternList as PatternListEx;
            RemoveAllPatterns(patternList!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear pattern definitions.");
        }
    }

    public void AddMillingDefinition(IonBeam beam, RatioRectangle rectangle)
    {
        try
        {
            var resolution = beam.GetResolution().Value;
            var hfw = beam.GetHorizontalFieldWidth().Value;
            var pixelSize = (hfw / resolution!.Width).Meters;
            var dwell = beam.GetDwellTime().Value.Seconds;
            var patternList = _patternDataSource.Object.PatternList as PatternListEx;
            var patternFactory = _patternDataSource.Object.PatternFactory as PatternFactoryEx;
            var rectanglePattern = patternFactory!.CreateRectangle();

            rectanglePattern.Beam.Value = enDBBeamType.enIonBeam;

            rectanglePattern.CenterX.Value = ((rectangle.GetCenter().X.DecimalFractions * resolution.Width) - (resolution.Width / 2)) * pixelSize;
            rectanglePattern.CenterY.Value = ((rectangle.GetCenter().Y.DecimalFractions * resolution.Height) - (resolution.Height / 2)) * pixelSize;

            rectanglePattern.Width.Value = rectangle.Width.DecimalFractions * resolution.Width * pixelSize;
            rectanglePattern.Length.Value = rectangle.Height.DecimalFractions * resolution.Height * pixelSize;
            rectanglePattern.PitchX.Value = pixelSize;
            rectanglePattern.PitchY.Value = pixelSize;
            rectanglePattern.ScanDirection.Value = enParameterRectangularScanDirection.enParameterRectangularScanDirectionTopToBottom;
            rectanglePattern.ScanType.Value = enParameterRectangularScanType.enParameterRectangularScanSerpentine;
            rectanglePattern.DwellTime.Value = dwell;
            rectanglePattern.FillStyle.Value = enParameterFillStyle.enParameterFillStyleFrame;
            //rectanglePattern.PassCount.Value = 1;
            //rectanglePattern.Enable.Value = true;

            patternList!.Add(rectanglePattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear pattern definitions.");
        }
    }

    public override Task AutoContrastBrightness(CancellationToken cancellationToken) => throw new NotImplementedException();
    public override Task AutoFocus(CancellationToken cancellationToken) => throw new NotImplementedException();
    public override Task AutoStigmation(CancellationToken cancellationToken) => throw new NotImplementedException();
}
