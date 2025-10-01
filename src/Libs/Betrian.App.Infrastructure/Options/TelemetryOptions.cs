namespace Betrian.App.Infrastructure.Options;

public class TelemetryOptions
{
    public bool MetricsEnabled { get; set; }

    public bool TracingEnabled { get; set; }

    public string TracingOtlpUri { get; set; } = "http://localhost:5341/ingest/otlp/v1/traces";

    public string TracingOtlpHeaders { get; set; } = string.Empty;
}
