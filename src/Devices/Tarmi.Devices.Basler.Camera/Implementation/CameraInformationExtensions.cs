using Basler.Pylon;

namespace Tarmi.Devices.Basler.Camera.Implementation;
internal static class CameraInformationExtensions
{
    private const string DeviceClassKey = "DeviceClass";
    private const string DeviceFactoryKey = "DeviceFactory";
    private const string FriendlyNameKey = "FriendlyName";
    private const string FullNameKey = "FullName";
    private const string InterfaceIDKey = "InterfaceID";
    private const string ModelNameKey = "ModelName";
    private const string SerialNumberKey = "SerialNumber";
    private const string TLTypeKey = "TLType";
    private const string UserDefinedNameKey = "UserDefinedName";
    private const string VendorNameKey = "VendorName";

    private const string EmulationDeviceCLass = "BaslerCamEmu";

    public static CameraInformation ToCameraInformation(this ICameraInfo cameraInfo)
    {
        return new CameraInformation
        {
            Name = cameraInfo.GetValueOrDefault(FriendlyNameKey, string.Empty),
            FullName = cameraInfo.GetValueOrDefault(FullNameKey, string.Empty),
            UserDefinedName = cameraInfo.GetValueOrDefault(UserDefinedNameKey, string.Empty),
            Vendor = cameraInfo.GetValueOrDefault(VendorNameKey, string.Empty),
            Model = cameraInfo.GetValueOrDefault(ModelNameKey, string.Empty),
            IsEmulated = cameraInfo.GetValueOrDefault(DeviceClassKey, string.Empty).Equals(EmulationDeviceCLass, StringComparison.OrdinalIgnoreCase)
        };
    }
}
