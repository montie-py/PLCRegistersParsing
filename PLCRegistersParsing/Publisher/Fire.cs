using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using PLCRegistersParsing.Config;
using PLCRegistersParsing.Publisher.Entities;
using PLCRegistersParsing.Publisher.Enums;
using PLCRegistersParsing.Publisher.Services;

namespace PLCRegistersParsing.Publisher;

public class Fire
{
    private const string UnitName = "CWTUnit";
    private List<ParameterBase> UnitParameters { get; set; }
    private Options FiringOptions { get; set; }
    private bool SendingBytes  { get; set; }
    
    private static bool SettingMessageHeader = bool.TryParse(Environment.GetEnvironmentVariable("SET_MESSAGE_HEADER"), out var value) && value;

    public Fire(List<ParameterBase> unitParameters, bool sendingBytes, DeviceConfig deviceConfig)
    {
        UnitParameters = unitParameters;
        SendingBytes = sendingBytes;
        var creds = new ServerCredentials(
            deviceConfig.ServerHost,
            deviceConfig.ServerPort,
            deviceConfig.ServerUser,
            deviceConfig.ServerPass,
            deviceConfig.UnitPrefix,
            Environment.GetEnvironmentVariable("MODULE_NAME") ?? "CWT"
        );

        FiringOptions = new Options(
            Host: creds.Host,
            Port: creds.Port,
            Username: creds.Username,
            Password: creds.Password,
            UnitsCount: creds.UnitsCount,
            UnitNamePrefix: creds.UnitNamePrefix,
            TransmissionDelay: 1,
            UnitsQuantity: 1,
            UnitTransmissionInterval: 5,
            MeasurementsTimeInterval: 5,
            WaitChallenge: 2000,
            WaitAck: 2000
        );
        
        var unit = CreateUnit();
        unit.ModuleName = creds.ModuleName;
        unit.SerialNumber = deviceConfig.SerialNumber;
        HandleUnit(unit);
    }

    private Unit CreateUnit()
    {
        var unitName = UnitName;
        var unitParameters = UnitParameters;
        var unitTransmissionInterval = FiringOptions.UnitTransmissionInterval;
        var measurementsInterval = FiringOptions.MeasurementsTimeInterval;

        Unit unit = new Unit(unitName, FiringOptions, unitParameters, unitTransmissionInterval,
            measurementsInterval);
        return unit;
    }

    private void HandleUnit(Unit unit)
    {
        // do
        // {
            try
            {
                UnitData unitData = unit.NewUnitData();
                SetUnitDataParams(unitData);
                
                // Send request
                SendInitialRequest(unitData);
                
                // Receive challenge
                ReceiveChallenge(unitData);
                
                // Create the header
                CreateMessage(unitData, sendingBytes:SendingBytes, settingMessageHeader:SettingMessageHeader);
                
                // Encrypt Message
                EncryptMessage(unitData, sendingBytes:SendingBytes);
                
                // Assemble Message
                AssembleMessage(unitData);
                
                // Send content
                SendMessage(unitData);
                
                // Confirm Receipt
                ReceiveConfirmationReceipt(unitData);
                
                // Finish connection
                CloseConnection(unitData);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        // } while (!token.IsCancellationRequested);
    }
    
    private void SetUnitDataParams(UnitData unitData)
    {
        unitData.ChallengeWaitTime = unitData.Unit.ChallengeWaitTimeMode;
        unitData.ACKWaitTime =unitData.Unit.ACKWaitTimeMode;
    }
    
    private void SendInitialRequest(UnitData unitData)
    {
        unitData.SetStatus(UnitStatusEnum.Transmitting);
        unitData.SetFirstTransmissionDateTime(DateTime.Now);
        TCPService.Connect(unitData.Client, FiringOptions.Host, FiringOptions.Port);
        unitData.SetStatus(UnitStatusEnum.WaitingForChallenge);

        Console.WriteLine($"Unit {unitData.Unit.Name} sending connection request.");
    }
    
    private void ReceiveChallenge(UnitData unitData)
    {
        try
        {
            byte[] receivedChallenge = TCPService.ReadData(unitData.Client, unitData.ChallengeWaitTime).Result;
            string challenge = Encoding.ASCII.GetString(receivedChallenge);

            unitData.SetChallenge(challenge, DateTime.Now);
        }
        catch (AggregateException ex)
        {
            ex.Handle(e =>
            {
                if (e is IOException)
                {
                    unitData.SetStatus(UnitStatusEnum.ChallengeFailed);
                }

                return true;
            });
            throw;
        }
    }
    
    private void CreateMessage(UnitData unitData, bool sendingBytes = false, bool settingMessageHeader = true)
    {
        unitData.CreateMessage(sendingBytes:sendingBytes, settingMessageHeader:settingMessageHeader);
    }
    
    private void EncryptMessage(UnitData unitData, bool sendingBytes = false)
    {
        string key = EncryptionService.GenerateMD5String($"{unitData.Challenge}{FiringOptions.Password}");
        object encryptionContent = unitData.OriginalContent;
        if (sendingBytes)
        {
            encryptionContent = unitData.OriginalContentBytesArray;
        }
        unitData.ContentBytes = EncryptionService.Encrypt(encryptionContent, key, sendingBytes:sendingBytes);
    }
    
    private void SendMessage(UnitData unitData)
    {
        TCPService.SendData(unitData.Client, unitData.FullMessageBytes);
        unitData.SetStatus(UnitStatusEnum.WaitingForACK);
        unitData.SetLastTransmittedDateTime(DateTime.Now);
    }
    
    private void ReceiveConfirmationReceipt(UnitData unitData)
    {
        try
        {
            byte[] receivedConfirmation = TCPService.ReadData(unitData.Client, unitData.ACKWaitTime).Result;
            string challenge = Encoding.ASCII.GetString(receivedConfirmation);
            unitData.SetACKReceived(DateTime.Now);
        }
        catch (AggregateException ex)
        {
            ex.Handle(e =>
            {
                if (e is IOException)
                {
                    unitData.SetStatus(UnitStatusEnum.ACKFailed);
                }

                return true;
            });

            throw;
        }
    }
    
    private void CloseConnection(UnitData unitData)
    {
        TCPService.CloseConnection(unitData.Client);

        unitData.SetStatus(UnitStatusEnum.WaitingToTransmit);
        unitData.Status = UnitStatusEnum.Finished;
    }
    
    private void AssembleMessage(UnitData unitData)
    {
        unitData.AssembleMessage();
    }

}