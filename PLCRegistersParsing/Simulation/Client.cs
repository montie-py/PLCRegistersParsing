using System.Text;
using PLCRegistersParsing.Config;
using PLCRegistersParsing.Publisher;
using PLCRegistersParsing.Publisher.Entities;
using PLCRegistersParsing.Simulation.ClientLogic;

namespace PLCRegistersParsing.Simulation;

using EasyModbus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

public class Client : IPublisher
{

    static bool sendingBytes =
        bool.TryParse(Environment.GetEnvironmentVariable("SENDING_BYTES"), out var value) && value;

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
            
            int[] registers =
                localConnection.ReadHoldingRegisters(localConfig.RegistersRangeFrom,
                    localConfig.RegistersRangeTo);
            
            List<List<string>> csvOutputList = new();
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
            tasks.Add(Task.Run(() => CsvWriterLoop(cts.Token, deviceRuntime)));
        }


        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            var allExceptions = new AggregateException(tasks.Where(t => t.IsFaulted).SelectMany(t => t.Exception.InnerExceptions));
            foreach (var innerEx in allExceptions.InnerExceptions)
            {
                Console.WriteLine($"Inner exception: {innerEx.Message}");
            }
        }

        // Thread pollingThread = new Thread(PollingLoop) { IsBackground = true };
        // Thread csvWriterThread = new Thread(CsvWriterLoop) { IsBackground = true };
        //
        // pollingThread.Start();
        // csvWriterThread.Start();

        // while (true)
        //     Thread.Sleep(1000);
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
                    deviceRuntime.Connection.ReadHoldingRegisters(deviceRuntime.Config.RegistersRangeFrom,
                        deviceRuntime.Config.RegistersRangeTo);

                lock (deviceRuntime.BufferLock)
                {
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

                    deviceRuntime.CsvBuffer.Add(parsedRegisters);
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

                List<List<string>> snapshot;

                lock (deviceRuntime.BufferLock)
                {
                    if (deviceRuntime.CsvBuffer.Count == 0)
                        continue;

                    deviceRuntime.PauseEvent.Set(); // pause polling
                    snapshot = new List<List<string>>(deviceRuntime.CsvBuffer);
                    deviceRuntime.CsvBuffer.Clear();
                }

                using (var writer = new StreamWriter(deviceRuntime.OutputFilename))
                {
                    foreach (var row in snapshot)
                    {
                        writer.WriteLine(string.Join(",", row));
                    }
                }

                SendingDataToFieldTracker(snapshot, deviceRuntime.Config.SerialNumber, deviceRuntime.OutputFilename);

                deviceRuntime.PauseEvent.Reset(); // resume polling
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("CsvWriterLoop Exception: " + ex.Message);
        }
    }

    private static void SendingDataToFieldTracker(List<List<string>> snapshot, string serialNumber, string outputFileName)
    {
        List<ParameterBase> parameters = new List<ParameterBase>();

        if (sendingBytes)
        {
            BytesParameter fireParameter = new()
            {
                Value = File.ReadAllBytes(outputFileName),
                Abbreviation = "Output",
                Name = "CWTOutput",
                MeasurementUnit = "CsvFile"
            };

            parameters.Add(fireParameter);
        }
        else
        {
            var pollingValuesHeadersArray = PollingValuesHeaders.PollingValuesHeadersArray;
            using var reader = new StreamReader(outputFileName);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var indices = line.Split(",");
                for (int i = 0; i < indices.Length; i++)
                {
                    StringParameter fireParameter = new()
                    {
                        Value = indices[i],
                        Abbreviation = pollingValuesHeadersArray[i].Abbreviation,
                        Name = pollingValuesHeadersArray[i].Abbreviation,
                        MeasurementUnit = pollingValuesHeadersArray[i].MeasurementUnit
                    };
                    parameters.Add(fireParameter);
                }
            }
        }

        new Fire(parameters, sendingBytes, serialNumber);

        Console.WriteLine($"CSV written with {snapshot.Count} rows");
    }
}