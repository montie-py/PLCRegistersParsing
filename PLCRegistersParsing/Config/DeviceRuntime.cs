using EasyModbus;

namespace PLCRegistersParsing.Config;

class DeviceRuntime
{
    public DeviceConfig Config { get; set; }
    public ModbusClient Connection { get; set; }
    public List<List<string>> CsvBuffer { get; set; } = new();
    public object BufferLock { get; set; } = new();
    public ManualResetEventSlim PauseEvent { get; set; } = new(false);
    public Dictionary<int, string> DecodeMap {get; set; } = new();
    public string OutputFilename { get; set; }

}
