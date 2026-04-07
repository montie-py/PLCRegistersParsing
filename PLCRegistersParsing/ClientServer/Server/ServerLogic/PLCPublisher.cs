using EasyModbus;

namespace PLCRegistersParsing.Simulation.ServerLogic;

public class PLCPublisher
{
    public ModbusClient Client { get; }
    private string ModbusClientIp { get; }
    private int ModbusClientPort { get; }
    public int holdingRegistersLength = 100;

    public PLCPublisher()
    {
        ModbusClientIp = Environment.GetEnvironmentVariable("PLC_IP") ?? "127.0.0.1";
        ModbusClientPort = int.TryParse(Environment.GetEnvironmentVariable("PLC_PORT"),
            out var plcPort)
            ? plcPort
            : 502;
        
        Client = new ModbusClient(ModbusClientIp, ModbusClientPort);

    }
}