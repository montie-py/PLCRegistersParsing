using EasyModbus;

namespace PLCRegistersParsing.Simulation.ServerLogic;

public class ModbusServerHost
{
    public ModbusServer Server { get; }
    private int ModBusServerPort { get; set; }
    public ModbusServerHost()
    {
        ModBusServerPort = int.TryParse(Environment.GetEnvironmentVariable("SERVER_PORT"),
            out var serverPort)
            ? serverPort
            : 3703;
        Server = new ModbusServer
        {
            Port = ModBusServerPort,
            UnitIdentifier = 1
        };

        // Allocate 100 holding registers
        ModbusServer.HoldingRegisters holdingRegisters = new(Server);
        holdingRegisters.localArray = new short[100];
    }

    public void Start()
    {
        Server.Listen();
        Console.WriteLine("Modbus TCP server started on port {0}",  ModBusServerPort);
    }
}