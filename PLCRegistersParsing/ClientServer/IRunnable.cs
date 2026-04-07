using PLCRegistersParsing.Config;

namespace PLCRegistersParsing.Simulation;

public interface IRunnable
{
    public static abstract Task Run(List<DeviceConfig> devicesConfigs);
}