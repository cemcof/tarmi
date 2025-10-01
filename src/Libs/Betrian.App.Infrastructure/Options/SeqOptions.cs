namespace Betrian.App.Infrastructure.Options;

public class SeqOptions
{
    public bool Enabled { get; set; }

    public Uri Uri { get; set; } = new Uri(uriString: "http://localhost:5341");

    public string ApiKey { get; set; } = string.Empty;
}
