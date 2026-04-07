using System.Globalization;

namespace PLCRegistersParsing.Simulation.ServerLogic;

public static class ValueEncoders
{
    public static void EncodeDate(string value, List<short> output)
    {
        var dt = DateTime.ParseExact(value, "yyyy/MM/dd", CultureInfo.InvariantCulture);
        int ts = (int)((DateTimeOffset)dt).ToUnixTimeSeconds();
        var hi = (short)((ts >> 16) & 0xFFFF);
        var lo = (short)(ts & 0xFFFF);
        output.Add(hi);
        output.Add(lo);
    }

    public static void EncodeTime(string value, List<short> output)
    {
       var t = TimeSpan.ParseExact(value, "hh\\:mm\\:ss", CultureInfo.InvariantCulture);
        int seconds = (int)t.TotalSeconds;

        var hi = (short)((seconds >> 16) & 0xFFFF);
        var lo = (short)(seconds & 0xFFFF);
        output.Add(hi);
        output.Add(lo);

    }

    public static void EncodeFloat(string value, List<short> output)
    {
        float f = float.Parse(value, CultureInfo.InvariantCulture);
        byte[] bytes = BitConverter.GetBytes(f);

        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);

        var lo = (short)BitConverter.ToUInt16(bytes, 0);
        var hi = (short)BitConverter.ToUInt16(bytes, 2);

        output.Add(hi);
        output.Add(lo);
    }

    public static void EncodeInt(string value, List<short> output)
    {
        output.Add(short.Parse(value));
    }
}