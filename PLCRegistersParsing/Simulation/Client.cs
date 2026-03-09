using System.Text;
using PLCRegistersParsing.Publisher;
using PLCRegistersParsing.Publisher.Entities;
using PLCRegistersParsing.Simulation.ClientLogic;

namespace PLCRegistersParsing.Simulation;

using EasyModbus; 
using System; 
using System.Collections.Generic; 
using System.IO; 
using System.Threading;

public class Client
{
    static string serverIp = Environment.GetEnvironmentVariable("SERVER_IP") ?? "127.0.0.1"; 
    static ModbusClient client = new(serverIp, 1502);
    static List<List<string>> csvOutputList = new();
    static ManualResetEventSlim pauseEvent = new(false);
    static object listLock = new();

    static Dictionary<int, string> decodeMap = new()
    {
        {0, "date"},
        {1, "time"},
        {2, "int"}
    };

    public static void Run()
    {
        Thread pollingThread = new Thread(PollingLoop) { IsBackground = true };
        Thread csvWriterThread = new Thread(CsvWriterLoop) { IsBackground = true };

        pollingThread.Start();
        csvWriterThread.Start();

        while (true)
            Thread.Sleep(1000);
    }

    static void PollingLoop()
    {
        while (true)
        {
            if (pauseEvent.IsSet)
            {
                Thread.Sleep(100);
                continue;
            }

            try
            {
                if (!client.Connected)
                    client.Connect();

                int[] registers = client.ReadHoldingRegisters(0, 57);

                lock (listLock)
                {
                    var parsedRegisters = new List<string>();
                    for (int i = 0, decodeMapIndex = 0;  i < registers.Length-1; i++)
                    {
                        string registerValue;
                        if (decodeMap.ContainsKey(decodeMapIndex))
                        {
                            switch (decodeMap[decodeMapIndex])
                            {
                                case "date":
                                    registerValue = ValueDecoders.DecodeDate(registers[i], registers[i + 1]);
                                    i++;
                                    break;
                                case "time":
                                    registerValue = ValueDecoders.DecodeTime(registers[i], registers[i + 1]);
                                    i++;
                                    break;
                                case "int":
                                default:
                                    registerValue = ValueDecoders.DecodeInt(registers[i]);
                                    break;
                            }
                        }
                        else
                        {
                            registerValue = ValueDecoders.DecodeFloat(registers[i], registers[i + 1]);
                            i++;
                        }
                        parsedRegisters.Add(registerValue);
                        ++decodeMapIndex;
                    }
                    csvOutputList.Add(parsedRegisters);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }

            Thread.Sleep(5000);
        }
    }

    static void CsvWriterLoop()
    {
        while (true)
        {
            Thread.Sleep(30000);

            List<List<string>> snapshot;

            lock (listLock)
            {
                if (csvOutputList.Count == 0)
                    continue;

                pauseEvent.Set(); // pause polling
                snapshot = new List<List<string>>(csvOutputList);
                csvOutputList.Clear();
            }

            using (var writer = new StreamWriter("output.csv"))
            {
                foreach (var row in snapshot)
                {
                    writer.WriteLine(string.Join(",", row));
                }
            }

            using (var reader = new StreamReader("output.csv"))
            {
                BytesParameter fireParameter = new BytesParameter
                {
                    Value = Encoding.UTF8.GetBytes(reader.ReadToEnd())
                };
                List<ParameterBase> parameters = new List<ParameterBase>();
                parameters.Add(fireParameter);
                new Fire(parameters);
            }

            Console.WriteLine($"CSV written with {snapshot.Count} rows");

            pauseEvent.Reset(); // resume polling
        }
    }
}
