namespace Betrian.Imaging.Common.Metadata.Thermofisher.IniFormat;

public class UserSection
{
    public DateOnly? Date { get; set; }
    public TimeOnly? Time { get; set; }
    public string User { get; set; } = string.Empty;
    public string UserText { get; set; } = string.Empty;
    public string UserTextUnicode { get; set; } = string.Empty;
}
