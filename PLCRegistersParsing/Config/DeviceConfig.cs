namespace PLCRegistersParsing.Config;

public class DeviceConfig
{
    public string SerialNumber { get; set; }
    public string ServerHost { get; set; }
    public string ServerIp { get; set; }
    public string ServerPass { get; set; }
    public int ServerPort { get; set; }
    public string ServerUser { get; set; }
    public string UnitPrefix { get; set; }
}
