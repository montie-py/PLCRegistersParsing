using System.Collections.Concurrent;
using EasyModbus;

namespace PLCRegistersParsing.Config;

class DeviceRuntime
{
    public DeviceConfig? Config { get; set; }
    public ModbusClient? Connection { get; set; }
    public ConcurrentQueue<KeyValuePair<string, List<string>>> RegistersBuffer { get; set; } = new();
    public object BufferLock { get; set; } = new();
    public ManualResetEventSlim PauseEvent { get; set; } = new(false);
    public Dictionary<int, string> DecodeMap {get; set; } = new();
    public string? OutputFilename { get; set; }

}
