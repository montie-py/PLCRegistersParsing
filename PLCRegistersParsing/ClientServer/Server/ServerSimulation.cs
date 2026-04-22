using PLCRegistersParsing.Config;
using PLCRegistersParsing.Simulation.ServerLogic;

namespace PLCRegistersParsing.Simulation;

public class ServerSimulation : IRunnable
{
    public static async Task Run(List<DeviceConfig> devicesConfigs)
    {
        var host = new ModbusServerHost();
        var feeder = new CSVFeeder();
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