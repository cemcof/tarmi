namespace Tarmi.Imaging.Common.Metadata;

public record TiffMetadata
{
    public string? Software { get; init; }
    public string? ImageDescription { get; init; }
    public DateTimeOffset TimeOfAcquisition { get; init; } = DateTimeOffset.Now;
    public string? CameraModel { get; init; }
}
