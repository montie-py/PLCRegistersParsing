using System.Collections.Concurrent;
using System.Threading.Channels;
using EasyModbus;

namespace PLCRegistersParsing.Config;

class DeviceRuntime
{
    public DeviceConfig? Config { get; set; }
    public ModbusClient? Connection { get; set; }
    public int BatchSize { get; set; }
    public Channel<KeyValuePair<string, List<string>>>? Channel { get; set; }
    public ConcurrentQueue<KeyValuePair<string, List<string>>>? Backlog { get; set; }
    public ManualResetEventSlim PauseEvent { get; set; } = new(false);
    public Dictionary<int, string> DecodeMap {get; set; } = new();
    public string? OutputFilename { get; set; }

}
