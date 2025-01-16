using RGB.NET.Core;

namespace Artemis.Plugins.Devices.iDotMatrix
{
    public sealed class iDotMatrixDeviceInfo : IRGBDeviceInfo
    {
        public RGBDeviceType DeviceType => RGBDeviceType.LedMatrix;

        public string DeviceName { get; }

        public string Manufacturer => "iDotMatrix";

        public string Model { get; }

        public object? LayoutMetadata { get; set; }

        public iDotMatrixDeviceInfo(string deviceName, string model)
        {
            DeviceName = deviceName;
            Model = model;
        }
    }
}
