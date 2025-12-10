namespace Tarmi.Installer.Helpers;

internal static class SharedData
{
    public static readonly Guid ProductUpgradeCode = new("C32E964E-A0C6-44A1-A86D-CBD128B6FC41");
    public const string ProductName = "Tarmi";
    public const string ProductDescription = "SW for Helios V Hydra CX with luminescence module";

    public static readonly Guid BundleUpgradeCode = new("1B20DEA1-B1FC-409C-BCD7-6AF02927F625");
    public const string BundleName = "Tarmi Bundle";
    public const string BundleDescription = "SW for Helios V Hydra CX with luminescence module including .Net Desktop Runtime";

    public const string Manufacturer = "Betrian";
    public const string ManufacturerContact = "info@betrian.cz";
}
