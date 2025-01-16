using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artemis.Plugins.Devices.iDotMatrix.Utils
{
    public static class Crc32
    {
        private static uint[]? _table;

        public static uint ComputeChecksum(Stream stream)
        {
            if (_table == null)
            {
                _table = CreateTable();
            }

            long position = stream.Position;

            uint crc = 0xffffffff;
            for (int i = 0; i < (int)stream.Length; ++i)
            {
                byte index = (byte)(((crc) & 0xff) ^ stream.ReadByte());
                crc = (_table[index] ^ (crc >> 8));
            }

            stream.Position = position;

            return ~crc;
        }

        private static uint[] CreateTable()
        {
            uint[] table = new uint[256];
            const uint polynomial = 0xedb88320;
            for (uint i = 0; i < 256; ++i)
            {
                uint crc = i;
                for (uint j = 8; j > 0; --j)
                {
                    if ((crc & 1) == 1)
                    {
                        crc = (crc >> 1) ^ polynomial;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
                table[i] = crc;
            }
            return table;
        }
    }
}
