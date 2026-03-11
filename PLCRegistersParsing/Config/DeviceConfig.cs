namespace PLCRegistersParsing.Config;

public class DeviceConfig
{
    public string SerialNumber { get; set; }
    public string DeviceIp { get; set; }
    public int DevicePort { get; set; }
    public int RegistersRangeFrom { get; set; }
    public int RegistersRangeTo { get; set; }
}
