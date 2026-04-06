using PLCRegistersParsing.Simulation.ServerLogic;

namespace PLCRegistersParsing.Simulation;

public class Server
{
    public static void Run()
    {
        string csvPath = Path.Combine(AppContext.BaseDirectory, "input.csv");

        var host = new ModbusServerHost();
        var feeder = new CSVFeeder(csvPath);
        Action<List<short>> sendCSVvalues = values =>
        {
            for (int i = 0; i < values.Count && i < host.Server.holdingRegisters.localArray.Length; i++)
            {
                host.Server.holdingRegisters[i+1] = values[i];
            }
        };
        feeder.Start(sendCSVvalues);
        host.Start();

        Thread.Sleep(Timeout.Infinite);
    }
}