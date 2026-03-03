using PLCRegistersParsing.Simulation.ServerLogic;

namespace PLCRegistersParsing.Simulation;

public class Server
{
    public static void Run()
    {
        string csvPath = Path.Combine(AppContext.BaseDirectory, "input.csv");

        var host = new ModbusServerHost();
        var feeder = new CSVFeeder(host.Server, csvPath);

        feeder.Start();
        host.Start();

        Thread.Sleep(Timeout.Infinite);
    }
}