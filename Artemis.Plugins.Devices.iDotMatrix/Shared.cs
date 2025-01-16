using System;

namespace Artemis.Plugins.Devices.iDotMatrix
{
    internal static class Shared
    {
        public const string BLUETOOTH_DEVICE_NAME = @"^IDM(\-|_)";

        public static readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(5);

        public static readonly Guid UUID_WRITE_DATA = new Guid("0000fa02-0000-1000-8000-00805f9b34fb");

    }
}
