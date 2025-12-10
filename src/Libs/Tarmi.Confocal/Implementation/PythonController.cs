using System.Text;
using Tarmi.Configuration.Application;
using CliWrap;

namespace Tarmi.Confocal.Implementation;

public class PythonController
{
    private static readonly string DefaultImagePath = @"c:\ProgramData\Betrian\CFLMnavi\Python";
    private static readonly string Light = "light-filter";
    private static readonly string LaserColor = "laser-color";
    private static readonly string Intensity = "intensity";
    private static readonly string Dwell = "dwell";
    private static readonly string FieldOfViewWidth = "field-of-view-width";
    private static readonly string FieldOfViewHeight = "field-of-view-height";
    private static readonly string PixelSize = "pixel-size";
    private static readonly string Gain = "gain";
    private static readonly string ADC = "adc";
    private static readonly string ImagePath = "image-path";

    private readonly PythonConfig _pythonConfig;
    private TextWriter? _stdInWriter;
    private MemoryStream? _stdIn;

    public PythonController(PythonConfig pythonConfig) => _pythonConfig = pythonConfig;

    public static List<string> GeneratePythonArgs(IConfocalDevice confocalDevice, string? imagePath)
    {
        List<string> args = [
                $"--{Light}={confocalDevice.LuminescenceMode}",
                $"--{LaserColor}={confocalDevice.LaserColor.Nanometers}",
                $"--{Intensity}={confocalDevice.Intensity.Percent}",
                $"--{Dwell}={confocalDevice.Dwell.Nanoseconds}",
                $"--{FieldOfViewWidth}={confocalDevice.FieldWidth.Nanometers}",
                $"--{FieldOfViewHeight}={confocalDevice.FieldHeight.Nanometers}",
                $"--{PixelSize}={confocalDevice.PixelSize.X.Nanometers}",
                $"--{Gain}={confocalDevice.Gain.Decibels}",
                $"--{ADC}={confocalDevice.ADC.Volts}"
            ];

        if (!string.IsNullOrWhiteSpace(imagePath))
        {
            args.Add($"--{ImagePath}=\"{imagePath}\"");
        }
        else
        {
            args.Add($"--{ImagePath}=\"{DefaultImagePath}\"");
        }

        return args;
    }

    public static List<string> GeneratePythonArg(IConfocalDevice confocalDevice, string propertyName)
    {
        var list = new List<string>();

        switch (propertyName)
        {
            case nameof(confocalDevice.LuminescenceMode):
                {
                    return [$"--{Light}={confocalDevice.LuminescenceMode}"];
                }

            case nameof(confocalDevice.LaserColor):
                {
                    return [$"--{LaserColor}={confocalDevice.LaserColor.Nanometers}"];
                }

            case nameof(confocalDevice.Intensity):
                {
                    return [$"--{Intensity}={confocalDevice.Intensity.Percent}"];
                }

            case nameof(confocalDevice.Dwell):
                {
                    return [$"--{Dwell}={confocalDevice.Dwell.Nanoseconds}"];
                }

            case nameof(confocalDevice.FieldWidth):
                {
                    return [$"--{FieldOfViewWidth}={confocalDevice.FieldWidth.Nanometers}"];
                }

            case nameof(confocalDevice.FieldHeight):
                {
                    return [$"--{FieldOfViewHeight}={confocalDevice.FieldHeight.Nanometers}"];
                }

            case nameof(confocalDevice.PixelSize):
                {
                    return [$"--{PixelSize}={confocalDevice.PixelSize.X.Nanometers}"];
                }

            case nameof(confocalDevice.Gain):
                {
                    return [$"--{Gain}={confocalDevice.Gain.Decibels}"];
                }

            case nameof(confocalDevice.ADC):
                {
                    return [$"--{ADC}={confocalDevice.ADC.Volts}"];
                }
        }

        return list;
    }

    public static string GetPythonArg(IConfocalDevice confocalDevice, string propertyName)
    {
        switch (propertyName)
        {
            case nameof(confocalDevice.LuminescenceMode):
                {
                    return $"--{Light}={confocalDevice.LuminescenceMode}";
                }

            case nameof(confocalDevice.LaserColor):
                {
                    return $"--{LaserColor}={confocalDevice.LaserColor.Nanometers}";
                }

            case nameof(confocalDevice.Intensity):
                {
                    return $"--{Intensity}={confocalDevice.Intensity.Percent}";
                }

            case nameof(confocalDevice.Dwell):
                {
                    return $"--{Dwell}={confocalDevice.Dwell.Nanoseconds}";
                }

            case nameof(confocalDevice.FieldWidth):
                {
                    return $"--{FieldOfViewWidth}={confocalDevice.FieldWidth.Nanometers}";
                }

            case nameof(confocalDevice.FieldHeight):
                {
                    return $"--{FieldOfViewHeight}={confocalDevice.FieldHeight.Nanometers}";
                }

            case nameof(confocalDevice.PixelSize):
                {
                    return $"--{PixelSize}={confocalDevice.PixelSize.X.Nanometers}";
                }

            case nameof(confocalDevice.Gain):
                {
                    return $"--{Gain}={confocalDevice.Gain.Decibels}";
                }

            case nameof(confocalDevice.ADC):
                {
                    return $"--{ADC}={confocalDevice.ADC.Volts}";
                }
        }

        return string.Empty;
    }

    public async Task<(bool, string)> ExecuteScriptWithArgs(List<string> pythonArgs)
    {
        if (!new FileInfo(_pythonConfig.ScriptPath).Exists)
        {
            throw new FileNotFoundException($"Python script was not found on {_pythonConfig.ScriptPath}");
        }

        var stdErrBuffer = new StringBuilder();

        var result = await Cli.Wrap(_pythonConfig.ExecutablePath)
            .WithArguments([_pythonConfig.ScriptPath, .. pythonArgs])
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
            .ExecuteAsync();

        return (result.IsSuccess, stdErrBuffer.ToString());
    }

    public async Task<(bool, string)> StartTuningScriptWithArgs(List<string> pythonArgs)
    {
        if (!new FileInfo(_pythonConfig.ScriptTuningPath).Exists)
        {
            throw new FileNotFoundException($"Python script was not found on {_pythonConfig.ScriptTuningPath}");
        }

        CancellationTokenSource cts = new CancellationTokenSource();

        var stdErrBuffer = new StringBuilder();
        _stdIn = new MemoryStream(0x1000);
        _stdInWriter = new StreamWriter(_stdIn, Encoding.ASCII);//, 0, true);
        using var shutDownCommandGuard = cts.Token.Register(() => _stdInWriter.WriteLine("quit"));

        var result = await Cli.Wrap(_pythonConfig.ExecutablePath)
            .WithArguments([_pythonConfig.ScriptTuningPath, .. pythonArgs])
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
            .WithStandardInputPipe(PipeSource.FromStream(_stdIn, true))
            .ExecuteAsync();

        return (result.IsSuccess, stdErrBuffer.ToString());
    }

    public void ExecuteTuningCommand(string command)
    {
        if (_stdInWriter != null && command.IsNotNullOrEmpty())
        {
            _stdInWriter.WriteLine(command);
        }
    }

    public void EndTuning()
    {
        if (_stdInWriter != null)
        {
            _stdInWriter.WriteLine("quit");
            _stdInWriter.Dispose();
            _stdIn?.Dispose();
        }
    }
}
