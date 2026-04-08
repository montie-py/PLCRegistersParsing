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
        // plcPublisher.Client.LogFileFilename = "here";
        Action<List<short>> sendCSVvalues = values =>
        {
            for (int i = 0; i < values.Count && i < plcPublisher.holdingRegistersLength; i++)
            {
                // plcPublisher.Client.WriteSingleRegister(i, values[i]);
                
                List<int> intValued = values.Select(v => Convert.ToInt32(v)).ToList();
                
                plcPublisher.Client.WriteMultipleRegisters(0, intValued.ToArray());
            }
        };
        feeder.Start(sendCSVvalues);
       

        Thread.Sleep(Timeout.Infinite);
    }
}