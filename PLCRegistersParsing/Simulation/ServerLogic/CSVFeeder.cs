using EasyModbus;

namespace PLCRegistersParsing.Simulation.ServerLogic;

public class CSVFeeder
{
    private readonly string _csvPath;

    public CSVFeeder(string csvPath)
    {
        _csvPath = csvPath;
    }

    public void Start(Action<List<short>> sendCSVvalues)
    {
        var thread = new Thread(() => FeedLoop(sendCSVvalues))
        {
            IsBackground = true
        };

        thread.Start();
    }

    private void FeedLoop(Action<List<short>> sendCSVvalues)
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

           sendCSVvalues(values);

            Console.WriteLine("Updated registers: " + string.Join(", ", values));

            Thread.Sleep(5000);
        }
    }
}