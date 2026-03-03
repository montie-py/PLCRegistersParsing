using EasyModbus;

namespace PLCRegistersParsing.Simulation.ServerLogic;

public class CSVFeeder
{
    private readonly ModbusServer _server;
    private readonly string _csvPath;

    public CSVFeeder(ModbusServer server, string csvPath)
    {
        _server = server;
        _csvPath = csvPath;
    }

    public void Start()
    {
        new Thread(FeedLoop) { IsBackground = true }.Start();
    }

    private void FeedLoop()
    {
        using var reader = new StreamReader(_csvPath);
        string? header = reader.ReadLine(); // skip header

        while (!reader.EndOfStream)
        {
            string? line = reader.ReadLine();
            if (line == null) continue;

            var values = new List<short>();
            var parts = line.Replace("\"", "").Split(',');

            foreach (var value in parts)
            {
                if (value.Contains('/'))
                    ValueEncoders.EncodeDate(value, values);
                else if (value.Contains(':'))
                    ValueEncoders.EncodeTime(value, values);
                else if (value.Contains('.'))
                    ValueEncoders.EncodeFloat(value, values);
                else
                    ValueEncoders.EncodeInt(value, values);
            }

            for (int i = 0; i < values.Count && i < _server.holdingRegisters.localArray.Length; i++)
            {
                _server.holdingRegisters[i+1] = values[i];
            }

            Console.WriteLine("Updated registers: " + string.Join(", ", values));

            Thread.Sleep(5000);
        }
    }
}