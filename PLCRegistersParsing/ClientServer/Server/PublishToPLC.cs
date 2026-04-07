using PLCRegistersParsing.Config;
using PLCRegistersParsing.Simulation.ServerLogic;

namespace PLCRegistersParsing.Simulation;

public class PublishToPLC : IRunnable
{
    public static async Task Run(List<DeviceConfig> devicesConfigs)
    {
        var plcPublisher = new PLCPublisher();
        var feeder = new CSVFeeder();
        
        plcPublisher.Client.Connect();
        Action<List<short>> sendCSVvalues = values =>
        {
            for (int i = 0; i < values.Count && i < plcPublisher.holdingRegistersLength; i++)
            {
                plcPublisher.Client.WriteSingleRegister(i+1, values[i]);
            }
        };
        feeder.Start(sendCSVvalues);
        plcPublisher.Client.Disconnect();
       

        Thread.Sleep(Timeout.Infinite);
    }
}