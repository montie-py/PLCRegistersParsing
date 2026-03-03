// See https://aka.ms/new-console-template for more information

namespace PLCRegistersParsing;

using PLCRegistersParsing.Simulation;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Specify 'client' or 'server'");
            return;
        }

        switch (args[0].ToLower())
        {
            case "client":
                Client.Run();
                break;

            case "server":
                Server.Run();
                break;

            default:
                Console.WriteLine("Unknown mode");
                break;
        }
    }
}
