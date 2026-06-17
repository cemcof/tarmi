using System.ComponentModel;
using Tarmi.Devices.Thermofisher.Instrument.ObjectModel;
using Dumpify;
using OpenCvSharp;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DevicesTestApp.Commands.ThermofisherInstrument;

[Description("Grabs an SEM image.")]
internal class GrabSemImageCommand : Command<GrabSemImageCommand.Settings>
{
    public sealed class Settings : ThermofisherInstrumentSettings
    {
    }

    private readonly SemImageSource _imageSource;

    public GrabSemImageCommand(SemImageSource imageSource)
    {
        _imageSource = imageSource;
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            var result = _imageSource.GrabImage(TimeSpan.FromSeconds(15));
            if (result.IsSuccess)
            {
                Cv2.ImShow("SEM Image", result.Value!.Image.Mat);
                _ = Cv2.WaitKey();

                _ = result.Value.FeiXmlMetadata?.Dump("FEI XML Metadata");

            }
            else
            {
                AnsiConsole.WriteException(result.Exception!);
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }

        return 0;
    }
}
