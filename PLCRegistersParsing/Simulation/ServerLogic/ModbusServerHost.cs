using EasyModbus;

namespace PLCRegistersParsing.Simulation.ServerLogic;

public class ModbusServerHost
{
    public ModbusServer Server { get; }
    public ModbusServerHost()
    {
        Server = new ModbusServer
        {
            Port = 1502,
            UnitIdentifier = 1
        };

        // Allocate 100 holding registers
        ModbusServer.HoldingRegisters holdingRegisters = new(Server);
        holdingRegisters.localArray = new short[100];
    }

    public void Start()
    {
        Server.Listen();
        Console.WriteLine("Modbus TCP server started on port 1502");
    }
}