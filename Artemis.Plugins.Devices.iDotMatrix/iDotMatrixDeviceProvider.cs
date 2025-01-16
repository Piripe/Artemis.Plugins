using Artemis.Core;
using Artemis.Core.DeviceProviders;
using Artemis.Core.Services;
using RGB.NET.Core;
using Serilog;
using System.Threading.Tasks;

namespace Artemis.Plugins.Devices.iDotMatrix
{
    [PluginFeature(Name = "iDotMatrix Device Provider")]
    public class iDotMatrixDeviceProvider : DeviceProvider
    {
        private readonly ILogger _logger;
        private readonly IDeviceService _deviceService;
        private readonly IRenderService _renderService;
        private readonly PluginSettings _settings;
        public override RGBiDotMatrixDeviceProvider RgbDeviceProvider => RGBiDotMatrixDeviceProvider.Instance;
        public iDotMatrixDeviceProvider(ILogger logger, IDeviceService deviceService, IRenderService renderService, PluginSettings settings)
        {
            _logger = logger;
            _deviceService = deviceService;
            _renderService = renderService;
            _settings = settings;
            RgbDeviceProvider.settings = settings;
            RgbDeviceProvider.logger = logger;
        }
        public override void Disable()
        {
            // Turn off devices
            _ = CloseDevicesAsync();

            // Give time to turn off devices
            System.Threading.Thread.Sleep(300);

            _deviceService.RemoveDeviceProvider(this);
            RgbDeviceProvider.Exception -= Provider_Exception;
            RgbDeviceProvider.Dispose();
        }
        private async Task CloseDevicesAsync()
        {
            foreach (iDotMatrixDevice device in RgbDeviceProvider.Devices)
            {
                await device.CloseDeviceAsync();
            }
        }

        public override void Enable()
        {
            RgbDeviceProvider.Exception += Provider_Exception;
            _deviceService.AddDeviceProvider(this);
        }

        private void Provider_Exception(object? sender, ExceptionEventArgs e) => _logger.Debug(e.Exception, "iDotMatrix Exception: {message}", e.Exception.Message);
    }
}
