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
            // lo = 23364;
            // hi = 96;
            byte[] bytes = new byte[4];

            // hi word → bytes[0], bytes[1]
            bytes[0] = (byte)(hi >> 8);
            bytes[1] = (byte)(hi & 0xFF);

            // lo word → bytes[2], bytes[3]
            bytes[2] = (byte)(lo >> 8);
            bytes[3] = (byte)(lo & 0xFF);

            double decodeFloat = BitConverter.ToSingle(bytes, 0) * 1.0;
            var outputString = decodeFloat.ToString("0.00", CultureInfo.InvariantCulture);
            return outputString;
        }


        public static string DecodeInt(int value)
        {
            return Convert.ToString(value); 
        }
    }
}
