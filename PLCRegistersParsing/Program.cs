// See https://aka.ms/new-console-template for more information

using System;
using System.Collections.Generic;
using PLCRegistersParsing.Config;

namespace PLCRegistersParsing;

using Simulation;
using Microsoft.Extensions.Configuration;

class Program
{
    private static List<DeviceConfig>? DevicesConfig { get; set; }
    
    static async Task Main(string[] args)
    {
        LoadConfig();
        
        if (args.Length == 0)
        {
            Console.WriteLine("Specify 'client' or 'server'");
            return;
        }

        switch (args[0].ToLower())
        {
            case "client":
                await Client.Run(DevicesConfig);
                break;

            case "server":
                Server.Run();
                break;

            default:
                Console.WriteLine("Unknown mode");
                break;
        }
    }

    private static void LoadConfig()
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();
        
        var config = builder.Build();

        DevicesConfig = config.GetSection("devices").Get<List<DeviceConfig>>();
    }
}
