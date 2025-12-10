using System.Diagnostics;

namespace Tarmi.App.Infrastructure;

public static class AppTelemetry
{
    public const string UiActivityName = "CFLMnavi.UI";
    public const string DeviceActivityName = "CFLMnavi.Device";
    public const string ImagePipelineActivityName = "Tarmi.ImagePipeline";
    public const string ImageAlgoActivityName = "CFLMnavi.ImageAlgo";

    private static string Version { get; } = typeof(AppTelemetry).Assembly.GetName().Version?.ToString() ?? "1.0.0.0";

    public static string[] SourceNames { get; } = [UiActivityName, DeviceActivityName, ImagePipelineActivityName];

    public static ActivitySource UiActivitySource { get; } = new ActivitySource(UiActivityName, Version);
    public static ActivitySource DeviceActivitySource { get; } = new ActivitySource(DeviceActivityName, Version);
    public static ActivitySource ImagePipelineActivitySource { get; } = new ActivitySource(ImagePipelineActivityName, Version);
    public static ActivitySource ImageAlgoActivitySource { get; } = new ActivitySource(ImageAlgoActivityName, Version);
}
