namespace PLCRegistersParsing.Simulation;
using Config;
using Publisher;
using Publisher.Entities;
using ClientLogic;

using EasyModbus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

public class Client : IPublisher, IRunnable
{
    static bool _isDebugMode = bool.TryParse(Environment.GetEnvironmentVariable("DEBUG"), out var value) && value;

    public static async Task Run(List<DeviceConfig> devicesConfigs)
    {
        // cancellation token will be triggered when Ctrl+C is pressed
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, __) => cts.Cancel();

        var tasks = new List<Task>();

        // polling from all the devices
        foreach (var deviceConfig in devicesConfigs)
        {
            var localConfig = deviceConfig;
            var localConnection = new ModbusClient(localConfig.DeviceIp, localConfig.DevicePort);
            
            if (!localConnection.Connected)
                localConnection.Connect();
            
            List<KeyValuePair<string, List<string>>> csvOutputList = new();
            ManualResetEventSlim pauseEvent = new(false);
            object listLock = new();

            Dictionary<int, string> decodeMap = new()
            {
                { 0, "date" },
                { 1, "time" },
                { 2, "int" }
            };

            var outputFileName = $"output_{localConfig.DeviceIp}.csv";
            
            var deviceRuntime = new DeviceRuntime();
            deviceRuntime.Config = localConfig;
            deviceRuntime.Connection = localConnection;
            deviceRuntime.CsvBuffer = csvOutputList;
            deviceRuntime.BufferLock = listLock;
            deviceRuntime.PauseEvent = pauseEvent;
            deviceRuntime.DecodeMap = decodeMap;
            deviceRuntime.OutputFilename = outputFileName;
            
            if (!deviceRuntime.Connection.Connected)
                deviceRuntime.Connection.Connect();

            tasks.Add(Task.Run(() => PollingLoop(cts.Token, deviceRuntime)));
            if (!_isDebugMode)
            {
                tasks.Add(Task.Run(() => CsvWriterLoop(cts.Token, deviceRuntime)));
            }
        }


        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            var allExceptions = new AggregateException(tasks.Where(t => t.IsFaulted).SelectMany(t => t.Exception!.InnerExceptions));
            foreach (var innerEx in allExceptions.InnerExceptions)
            {
                Console.WriteLine($"Inner exception: {innerEx.Message}");
            }
            Console.WriteLine($"Outer exception: {ex.Message}");
        }
    }

    static void PollingLoop(CancellationToken token, DeviceRuntime deviceRuntime)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                
                if (deviceRuntime.PauseEvent.IsSet)
                {
                    Thread.Sleep(int.TryParse(Environment.GetEnvironmentVariable("POLLING_LOOP_PAUSE_MILLS"),
                        out var pauseMills)
                        ? pauseMills
                        : 50);
                    continue;
                }

                int[] registers =
                    deviceRuntime.Connection!.ReadHoldingRegisters(deviceRuntime.Config!.RegistersRangeFrom,
                        deviceRuntime.Config.RegistersRangeQuantity);

                lock (deviceRuntime.BufferLock)
                {
                    // decoding registers' values
                    var parsedRegisters = new List<string>();
                    for (int i = 0, decodeMapIndex = 0; i < registers.Length - 1; i++)
                    {
                        string registerValue;
                        if (deviceRuntime.DecodeMap.ContainsKey(decodeMapIndex))
                        {
                            switch (deviceRuntime.DecodeMap[decodeMapIndex])
                            {
                                case "date":
                                    registerValue = ValueDecoders.DecodeDate(registers[i], registers[i + 1]);
                                    i++;
                                    break;
                                case "time":
                                    registerValue = ValueDecoders.DecodeTime(registers[i], registers[i + 1]);
                                    i++;
                                    break;
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
                        decodeMapIndex++;
                    }

                    string timeStamp = DateTime.UtcNow.ToString("yyMMddHHmmss");

                    if (_isDebugMode)
                    {
                        var parsedRegistersJoined = string.Join(", ", parsedRegisters);
                        Console.WriteLine($"Timestamp: {timeStamp} Registers: {parsedRegistersJoined}");
                    }

                    deviceRuntime.CsvBuffer.Add(new KeyValuePair<string, List<string>>(timeStamp, parsedRegisters));
                }
                
                Thread.Sleep(int.TryParse(Environment.GetEnvironmentVariable("POLLING_LOOP_INTERVAL_MILLS"),
                    out var intervalMills)
                    ? intervalMills
                    : 1000);
            }
        
        }
        catch (Exception ex)
        {
            Console.WriteLine("PollingLoop while() Exception: " + ex.Message);
        }
    }

    static void CsvWriterLoop(CancellationToken token, DeviceRuntime deviceRuntime)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                Thread.Sleep(int.TryParse(Environment.GetEnvironmentVariable("PUBLISHING_LOOP_INTERVAL_MILLS"),
                    out var intervalMills)
                    ? intervalMills
                    : 1000);

                List<KeyValuePair<string, List<string>>> snapshot;

                // pause polling
                lock (deviceRuntime.BufferLock)
                {
                    if (deviceRuntime.CsvBuffer.Count == 0)
                        continue;

                    deviceRuntime.PauseEvent.Set();
                    snapshot = new List<KeyValuePair<string, List<string>>>(deviceRuntime.CsvBuffer);
                    deviceRuntime.CsvBuffer.Clear();
                }

                //generating a separate CSV file (not for fieldtracker)
                using (var writer = new StreamWriter(deviceRuntime.OutputFilename!))
                {
                    foreach (var row in snapshot)
                    {
                        writer.WriteLine(string.Join(",", row));
                    }
                }

                //sending data to fieldtracker
                SendingDataToFieldTracker(snapshot, deviceRuntime.Config!.SerialNumber!);

                // resume polling
                deviceRuntime.PauseEvent.Reset();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("CsvWriterLoop Exception: " + ex.Message);
        }
    }

    private static void SendingDataToFieldTracker(List<KeyValuePair<string, List<string>>> snapshot, string serialNumber)
    {
        Dictionary<string, List<ParameterBase>> parameters = new();
        
        var pollingValuesHeadersArray = PollingValuesHeaders.PollingValuesHeadersArray;
        foreach (var entry in snapshot)
        {
            List<ParameterBase> rowParameters = new();
            for (int i  = 0; i < entry.Value.Count; i++)
            {
                StringParameter fireParameter = new()
                {
                    Value = entry.Value[i],
                    Abbreviation = pollingValuesHeadersArray[i].Abbreviation,
                    Name = pollingValuesHeadersArray[i].Abbreviation,
                    MeasurementUnit = pollingValuesHeadersArray[i].MeasurementUnit
                };
                rowParameters.Add(fireParameter);   
            }
            parameters.Add(entry.Key, rowParameters);
        }

        new Fire(parameters, serialNumber);

        Console.WriteLine($"CSV written with {snapshot.Count} rows");
    }
}