using System;
using System.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Artemis.Plugins.Devices.iDotMatrix.Extensions
{
    public static class BLEExtensions
    {
        public static GattCharacteristic? GetCharacteristic(this BluetoothLEDevice device, Guid uuid)
        {
            var result = device.GetGattServicesAsync(BluetoothCacheMode.Uncached).AsTask().Result;
            if (result.Status != GattCommunicationStatus.Success) return null;
            return result.Services.SelectMany(x=> {
                var result = x.GetCharacteristicsAsync(BluetoothCacheMode.Uncached).AsTask().Result;
                if (result.Status != GattCommunicationStatus.Success) return [];
                return result.Characteristics;
            }).FirstOrDefault(x=>x.Uuid == uuid);
        }
    }
}
