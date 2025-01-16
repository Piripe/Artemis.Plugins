using Artemis.Plugins.Devices.iDotMatrix.Enums;
using Example;
using RGB.NET.Core;
using System;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Artemis.Plugins.Devices.iDotMatrix.Extensions;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SkiaSharp;
using System.Threading.Tasks;
using FluentAvalonia.Core;

namespace Artemis.Plugins.Devices.iDotMatrix
{
    public class iDotMatrixUpdateQueue : UpdateQueue
    {
        protected BluetoothLEDevice BleDevice;
        protected GattCharacteristic? WriteCharacteristic;
        protected SKBitmap? Bitmap;

        protected iDotMatrixModel model = iDotMatrixModel.x16;
        public iDotMatrixUpdateQueue(IDeviceUpdateTrigger updateTrigger, BluetoothLEDevice device) : base(updateTrigger)
        {
            BleDevice = device;
        }

        protected override void OnStartup(object? sender, CustomUpdateData customData)
        {
            base.OnStartup(sender, customData);

            WriteCharacteristic = BleDevice.GetCharacteristic(Shared.UUID_WRITE_DATA);
            if (WriteCharacteristic == null) throw new Exception("Wrong characteristics in device " + BleDevice.Name + ".");

            // TODO: Reverse engineer the matrix to understand how to get the model (I personnaly have a 16x16 model so I'll leave it as it is for now)
            model = iDotMatrixModel.x16;
            int size = model == iDotMatrixModel.x16 ? 16 : 32;
            Bitmap = new SKBitmap(size, size, SKColorType.Rgb565, SKAlphaType.Opaque);
            SetPngMode(1);
        }

        protected override bool Update(ReadOnlySpan<(object key, Color color)> dataSet)
        {
            try
            {
                if (Bitmap == null) return false;

                foreach ((object key, Color color) in dataSet)
                {
                    Point position = (Point)key;
                    if (position.X > Bitmap.Width || position.Y > Bitmap.Height) continue;
                    Bitmap.SetPixel((int)position.X, (int)position.Y, new SKColor(color.GetR(), color.GetG(), color.GetB()));
                }

                // SkiaSharp Way
                using (var image = SKImage.FromBitmap(Bitmap))
                using (var data = image.Encode(SKEncodedImageFormat.Png, 50))
                using (var stream = data.AsStream())
                {
                    SetPng(stream);
                }

                // GDI+ Way (Note: You must enable Windows Forms first to use this way (It's not worth at all))
                /*using (var stream = new MemoryStream()) {
                    Bitmap.Save(stream, ImageCodecInfo.GetImageEncoders().First((x)=>x.MimeType=="image/gif"), new EncoderParameters() { Param = [new EncoderParameter(Encoder.Quality, 100L), new EncoderParameter(Encoder.ColorDepth, 24L)] });
                    stream.Position = 0;
                    SetGif(stream);
                }*/
                return true;
            }
            catch (Exception ex)
            {
                RGBiDotMatrixDeviceProvider.Instance.Throw(ex);
            }

            return false;
        }
        internal iDotMatrixModel GetModel()
        {
            return model;
        }

        public async Task CloseDeviceAsync()
        {
            await TurnOffAsync();
            WriteCharacteristic = null;
            BleDevice.Dispose();
        }


        public void TurnOn()
        {
            Write([0x05, 0x00, 0x07, 0x01, 0x01], true);
        }
        public void TurnOff()
        {
            Write([0x05, 0x00, 0x07, 0x01, 0x00], true);
        }
        public async Task TurnOffAsync()
        {
            await WriteAsync([0x05, 0x00, 0x07, 0x01, 0x00], true);
        }
        public void SetPixel(byte x, byte y, Color color)
        {
            Write([0x0a, 0x00, 0x05, 0x01, 0x01, color.GetR(), color.GetG(), color.GetB(), x, y]);
        }
        public void SetPngMode(byte mode)
        {
            Write([0x05, 0x00, 0x04, 0x01, mode], true);
        }
        long frameId = 0;
        public void SetPng(Stream stream)
        {
            _ = SetPngAsync(stream);
        }
        public async Task SetPngAsync(Stream stream)
        {
            frameId++;
            long currentFrame = frameId;

            var payloads = CreatePngPayloads(stream);


            foreach (var payload in payloads)
            {
                bool retrying = false;
                while (frameId == currentFrame && !await WriteAsync(payload, false, retrying))
                {
                    await Task.Delay(1000);
                    if (frameId != currentFrame) return;
                    retrying = true;
                }
            }
        }
        public List<byte[]> CreatePngPayloads(Stream stream)
        {
            int chunkCount = (int)Math.Ceiling(stream.Length / 4096d);
            int idk = (int)stream.Length + chunkCount;
            byte[] idkBytes = BitConverter.GetBytes((short)idk);
            byte[] pngLenBytes = BitConverter.GetBytes((int)stream.Length);
            byte[] buffer = new byte[4096];

            List<byte[]> payloads = new List<byte[]>();
            for (int i = 0; i < chunkCount; i++)
            {
                int readCount = Math.Min(4096, (int)stream.Length - i * 4096);
                stream.Read(buffer, 0, readCount);
                byte[] payload = idkBytes.Concat(new byte[] { 0, 0, (byte)(i > 0 ? 2 : 0) })
                    .Concat(pngLenBytes)
                    .Concat(buffer.Take(readCount))
                    .ToArray();

                payloads.Add(payload);
            }
            return payloads;
        }
        public void SetGif(Stream stream)
        {

            var payloads = CreateGifPayloads(stream);

            foreach (var payload in payloads)
            {
                Write(payload);
            }
        }
        public List<byte[]> CreateGifPayloads(Stream stream)
        {
            uint crc = Utils.Crc32.ComputeChecksum(stream);

            byte[] header =
            [
                255, 255, 1, 0, 0, 255, 255, 255, 255, 255, 255, 255, 255, 5, 0, 13
            ];

            byte[] lengthBytes = BitConverter.GetBytes(stream.Length + header.Length);
            Array.Copy(lengthBytes, 0, header, 5, 4);

            byte[] crcBytes = BitConverter.GetBytes(crc);
            Array.Copy(crcBytes, 0, header, 9, 4);

            int chunkCount = (int)Math.Ceiling(stream.Length / 4096d);

            byte[] buffer = new byte[4096];

            List<byte[]> payloads = new List<byte[]>();

            for (int i = 0; i < chunkCount; i++)
            {
                header[4] = (byte)(i > 0 ? 2 : 0);
                int readCount = Math.Min(4096, (int)stream.Length - i * 4096);
                stream.Read(buffer, 0, readCount);
                int chunkLen = readCount + header.Length;
                byte[] chunkLenBytes = BitConverter.GetBytes((ushort)chunkLen);
                Array.Copy(chunkLenBytes, 0, header, 0, 2);

                byte[] payload = header.Concat(buffer.Take(readCount)).ToArray();
                payloads.Add(payload);
            }

            return payloads;
        }
        int waitingWriting = 0;
        int lastSync = 0;
        public bool Write(byte[] data, bool force = false)
        {
            return WriteAsync(data, force).Result;
        }
        public async Task<bool> WriteAsync(byte[] data, bool force = false, bool withResponse = false)
        {
            // Easiest way to avoid sending multiple frame at once. Cons: The matrix may not be fully updated
            if (waitingWriting > (data.Length > 150 ? 1 : 0) && !force) return false;
            waitingWriting++;
            lastSync++;
            var chunks = data.Chunk(4096).ToArray(); // Max chunk size supported by my device
            int i = 0;
            foreach (var chunk in chunks)
            {
                i++;
                GattWriteResult result;
                if (withResponse) await Task.Delay(200);
                do
                {
                     result = await WriteCharacteristic?.WriteValueWithResultAsync(chunk.AsBuffer(), ((i == chunks.Count() && (chunk.Length > 150 || lastSync > 10)) || withResponse ? GattWriteOption.WriteWithResponse : GattWriteOption.WriteWithoutResponse));
                } while ((force || withResponse) && result.Status != GattCommunicationStatus.Success);
                await Task.Delay(Math.Max(10,(int)(chunk.Length * 0.5)));
            }
            if (lastSync > 10) lastSync = 0;
            waitingWriting--;
            return true;
        }
    }
}
