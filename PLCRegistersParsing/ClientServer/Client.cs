using System.Collections.Concurrent;
using System.Threading.Channels;

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

public class Client : IRunnable
{
    static bool _isDebugMode = bool.TryParse(Environment.GetEnvironmentVariable("DEBUG"), out var value) && value;
    private static bool _isConnectionDisrupted;
    private static int RegistersBufferMaxRows { get; set; }

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
            {
                Connect(localConnection, cts);
            }

            ManualResetEventSlim pauseEvent = new(false);

            Dictionary<int, string> decodeMap = new()
            {
                { 0, "date" },
                { 1, "time" },
                { 2, "int" }
            };

            var outputFileName = $"output_{localConfig.DeviceIp}.csv";
            RegistersBufferMaxRows = int.TryParse(Environment.GetEnvironmentVariable("REGISTERS_BUFFER_MAX_ROWS"),
                out var bufferCapacity)
                ? bufferCapacity
                : 3600;

            var deviceRuntime = new DeviceRuntime();
            deviceRuntime.Config = localConfig;
            deviceRuntime.Connection = localConnection;

            //creating a channel with registersBufferMaxRows capacity
            deviceRuntime.Channel = Channel.CreateBounded<KeyValuePair<string, List<string>>>(
                new BoundedChannelOptions(RegistersBufferMaxRows)
                {
                    FullMode = BoundedChannelFullMode.DropOldest
                });
            deviceRuntime.PauseEvent = pauseEvent;
            deviceRuntime.DecodeMap = decodeMap;
            deviceRuntime.OutputFilename = outputFileName;
            deviceRuntime.Backlog = new ConcurrentQueue<KeyValuePair<string, List<string>>>();
            deviceRuntime.BatchSize = int.TryParse(Environment.GetEnvironmentVariable("NR_OF_ROWS_TO_SEND"),
                out var nrOfRowsToSend)
                ? nrOfRowsToSend
                : 1000;

            if (!deviceRuntime.Connection.Connected)
                deviceRuntime.Connection.Connect();

            //producer/consumer loops
            tasks.Add(Task.Run(async () => await PollingLoop(cts.Token, deviceRuntime)));
            if (!_isDebugMode)
            {
                // tasks.Add(Task.Run(async () => await FieldTrackerWriterLoop(cts.Token, deviceRuntime)));

                var readerTask = Task.Run(() => ReaderLoop(cts.Token, deviceRuntime));
                var senderTask = Task.Run(() => SenderLoop(cts.Token, deviceRuntime));

                tasks.Add(readerTask);
                tasks.Add(senderTask);
            }
        }


        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            var allExceptions =
                new AggregateException(tasks.Where(t => t.IsFaulted).SelectMany(t => t.Exception!.InnerExceptions));
            foreach (var innerEx in allExceptions.InnerExceptions)
            {
                Console.WriteLine($"Inner exception: {innerEx.Message}");
            }

            Console.WriteLine($"Outer exception: {ex.Message}");
        }
    }

    private static void Connect(ModbusClient localConnection, CancellationTokenSource cts)
    {
        while (!cts.IsCancellationRequested)
        {
            try
            {
                localConnection.Connect();
                if (_isConnectionDisrupted)
                {
                    Console.WriteLine($"{DateTime.Now} - The connection to PLC/HMI is now established!");
                    _isConnectionDisrupted = false;
                }
                return;
            }
            catch (Exception e)
            {
                var reconnectInterval = int.TryParse(
                    Environment.GetEnvironmentVariable("RECONNECT_TO_PLC_INTERVAL_MILLS"),
                    out var reconnectIntervalMills)
                    ? reconnectIntervalMills
                    : 1000;
                var dateTime = DateTime.Now;
                Console.WriteLine($"{dateTime} - PLC/HMI is inaccessible: {e.Message}\n {e.StackTrace}\n Retrying...");
                _isConnectionDisrupted = true;
                Thread.Sleep(reconnectInterval);
            }
        }
    }

    static async Task PollingLoop(CancellationToken token, DeviceRuntime deviceRuntime)
    {
        var skipRowsCount = 0;
        while (!token.IsCancellationRequested)
        {
            try
            {
                // reading holding registers
                int[] registers =
                    deviceRuntime.Connection!.ReadHoldingRegisters(deviceRuntime.Config!.RegistersRangeFrom,
                        deviceRuntime.Config.RegistersRangeQuantity);

                if (_isConnectionDisrupted)
                {
                    Console.WriteLine($"{DateTime.Now} - Connection to PLC/HMI is restored!");
                    _isConnectionDisrupted = false;
                    skipRowsCount = 10;
                }

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
                var pollingLoopIntervalMills = int.TryParse(Environment.GetEnvironmentVariable("POLLING_LOOP_INTERVAL_MILLS"),
                    out var intervalMills)
                    ? intervalMills
                    : 1000;
                
                if (skipRowsCount > 0)
                {
                    skipRowsCount--;
                    Console.WriteLine("Skipping...");
                    await Task.Delay(pollingLoopIntervalMills, token); 
                    continue;
                }

                if (_isDebugMode)
                {
                    var parsedRegistersJoined = string.Join(", ", parsedRegisters);
                    Console.WriteLine($"Timestamp: {timeStamp} Registers: {parsedRegistersJoined}");
                }

                await deviceRuntime.Channel!.Writer.WriteAsync(
                    new KeyValuePair<string, List<string>>(timeStamp, parsedRegisters),
                    token);

                await Task.Delay(pollingLoopIntervalMills, token);
            }
            catch (Exception ex)
            {
                
                var dateTime = DateTime.Now; 
                Console.WriteLine($"{dateTime} - Polling from PLC/HMI error: {ex.Message}\n {ex.StackTrace}\n Retrying...");
                _isConnectionDisrupted = true;
            }
        }
    }

    static async Task ReaderLoop(CancellationToken token, DeviceRuntime deviceRuntime)
    {
        await foreach (var row in deviceRuntime.Channel!.Reader.ReadAllAsync(token))
        {
            deviceRuntime.Backlog!.Enqueue(row);
        }
    }

    static async Task SenderLoop(CancellationToken token, DeviceRuntime deviceRuntime)
    {
        var batch = new ConcurrentQueue<KeyValuePair<string, List<string>>>();
        bool lastSendFailed = false;

        while (!token.IsCancellationRequested)
        {
            var row = new KeyValuePair<string, List<string>>();
            // Fill batch
            while (batch.Count < deviceRuntime.BatchSize)
            {
                if (deviceRuntime.Backlog!.TryDequeue(out row))
                {
                    batch.Enqueue(row);
                    // Console.WriteLine(batch.Count);
                }
            }

            try
            {
                if (lastSendFailed)
                {
                    //adding the extra data to the final Queue, while it's trying to reconnect/resend data to FieldTracker
                    while (deviceRuntime.Backlog!.TryDequeue(out var extra))
                    {
                        batch.Enqueue(extra);
                    }

                    //leaving the exact amount of RegistersBufferMaxRows in the batch, in case it overflows 
                    while (batch.Count > RegistersBufferMaxRows)
                    {
                        batch.TryDequeue(out _);
                    }
                }

                await SendingDataToFieldTracker(batch, deviceRuntime.Config!.SerialNumber!);
                lastSendFailed = false;
                batch.Clear();
            }
            catch
            {
                lastSendFailed = true;
                // Console.WriteLine($"Extra: {batch.Count}");
                var fieldTrackerRetryDelayMils = int.TryParse(
                    Environment.GetEnvironmentVariable("FIELDTRACKER_RETRY_DELAY_MILIS"),
                    out var retryDelayMils)
                    ? retryDelayMils
                    : 1000;
                await Task.Delay(fieldTrackerRetryDelayMils, token);
            }
        }
    }

    private static async Task<Task> SendingDataToFieldTracker(
        ConcurrentQueue<KeyValuePair<string, List<string>>> snapshot,
        string serialNumber)
    {
        Dictionary<string, List<ParameterBase>> parameters = new();

        var pollingValuesHeadersArray = PollingValuesHeaders.PollingValuesHeadersArray;
        foreach (var entry in snapshot)
        {
            List<ParameterBase> rowParameters = new();
            for (int i = 0; i < entry.Value.Count; i++)
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

        var fireObject = new Fire(parameters, serialNumber);
        await fireObject.FireUnit();

        var currentDateTime = DateTime.Now;
        Console.WriteLine($"{currentDateTime} - CSV written with {snapshot.Count} rows");
        return Task.CompletedTask;
    }
}