using System.Collections.Generic;
using PLCRegistersParsing.Config;

namespace PLCRegistersParsing.Simulation;

public interface IPublisher
{
    public static abstract Task Run(List<DeviceConfig> devicesConfig);
}