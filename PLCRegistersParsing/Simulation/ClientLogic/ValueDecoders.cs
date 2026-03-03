using System;
using System.Globalization;

namespace PLCRegistersParsing.Simulation.ClientLogic
{
    public static class ValueDecoders
    {
        // ---------------------------------------------------------
        // 32-bit reconstruction helper
        // ---------------------------------------------------------
        private static int CombineToUInt32(int lo, int hi)
        {
            uint value = ((uint)(ushort)hi << 16) | (ushort)lo;
            return unchecked((int)value);
        }

        // ---------------------------------------------------------
        // Date decoder (2 registers → yyyy/MM/dd)
        // ---------------------------------------------------------
        public static string DecodeDate(int hi, int lo)
        {
            int ts = CombineToUInt32(lo, hi);
            var dt = DateTimeOffset.FromUnixTimeSeconds(ts).DateTime;
            return dt.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
        }

        // ---------------------------------------------------------
        // Time decoder (2 registers → HH:mm:ss)
        // ---------------------------------------------------------
        public static string DecodeTime(int hi, int lo)
        {
            int seconds = CombineToUInt32(lo, hi);
            var timeString = TimeSpan.FromSeconds(seconds).ToString();
            return timeString;
        }

        // ---------------------------------------------------------
        // Float decoder (2 registers → float)
        // ---------------------------------------------------------
        public static string DecodeFloat(int hi, int lo)
        {
            ushort uhi = unchecked((ushort)hi);
            ushort ulo = unchecked((ushort)lo);

            byte[] bytes = new byte[4];

            // Big-endian Modbus order: hi word first
            bytes[0] = (byte)(uhi >> 8);
            bytes[1] = (byte)(uhi & 0xFF);
            bytes[2] = (byte)(ulo >> 8);
            bytes[3] = (byte)(ulo & 0xFF);

            // Convert to little-endian if needed
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            return Convert.ToString(BitConverter.ToSingle(bytes, 0), CultureInfo.InvariantCulture);
        }

        public static string DecodeInt(int value)
        {
            return Convert.ToString(value); 
        }
    }
}
