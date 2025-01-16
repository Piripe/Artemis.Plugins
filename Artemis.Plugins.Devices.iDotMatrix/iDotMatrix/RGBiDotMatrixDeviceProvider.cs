using Artemis.Core;
using Artemis.Plugins.Devices.iDotMatrix.Extensions;
using RGB.NET.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace Artemis.Plugins.Devices.iDotMatrix
{
    public class RGBiDotMatrixDeviceProvider : AbstractRGBDeviceProvider
    {
        private static RGBiDotMatrixDeviceProvider? _instance;
        public static RGBiDotMatrixDeviceProvider Instance
        {
            get
            {
                return _instance ?? new RGBiDotMatrixDeviceProvider();
            }
        }


        public ILogger logger = null!;
        public PluginSettings settings = null!;


        public RGBiDotMatrixDeviceProvider()
        {
            if (_instance != null) throw new InvalidOperationException($"There can be only one instance of type {nameof(RGBiDotMatrixDeviceProvider)}");
            _instance = this;
        }
        protected override void InitializeSDK()
        {
        }

        DeviceWatcher deviceWatcher = null!;
        protected override IEnumerable<IRGBDevice> LoadDevices()
        {
            PluginSetting<List<string>> definitions = settings.GetSetting("DeviceDefinitions", new List<string>());
            var tcs = new TaskCompletionSource<bool>();

            // Additional properties we would like about the device.
            // Property strings are documented here https://msdn.microsoft.com/en-us/library/windows/desktop/ff521659(v=vs.85).aspx
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected", "System.Devices.Aep.Bluetooth.Le.IsConnectable" };

            // BT_Code: Example showing paired and non-paired in a single query.
            string aqsAllBluetoothLEDevices = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";

            deviceWatcher = DeviceInformation.CreateWatcher(
                    aqsAllBluetoothLEDevices,
                    requestedProperties,
                    DeviceInformationKind.AssociationEndpoint);

            int i = 0;
            deviceWatcher.Added += (s, device) =>
            {
                if (Regex.IsMatch(device.Name, Shared.BLUETOOTH_DEVICE_NAME))
                {
                    if (!definitions.Value!.Contains(device.Id)) {
                        definitions.Value!.Add(device.Id);
                        definitions.Save();
                        logger.Information("Device added: "+device.Name+"\nPlease reload the plugin.");
                    }
                }
            };
            deviceWatcher.EnumerationCompleted += (s, e) => { tcs.TrySetResult(true); };

            deviceWatcher.Start();

            foreach (var device in definitions.Value!)
            {
                var bleDevice = BluetoothLEDevice.FromIdAsync(device).AsTask().TimeoutAfter(Shared.TIMEOUT).Result;
                if (bleDevice == null) throw new Exception("Device " + device + " timed out.");
                IDeviceUpdateTrigger updateTrigger = GetUpdateTrigger(i++);
                iDotMatrixUpdateQueue updateQueue = new iDotMatrixUpdateQueue(updateTrigger, bleDevice);
                yield return new iDotMatrixDevice(new iDotMatrixDeviceInfo($"iDotMatrix {(updateQueue.GetModel() == Enums.iDotMatrixModel.x16 ? "16x16" : "32x32")} [{bleDevice.Name}]", $"iDotMatrix {(updateQueue.GetModel() == Enums.iDotMatrixModel.x16 ? "16x16" : "32x32")}"), updateQueue);
            }
        }

    }
}
