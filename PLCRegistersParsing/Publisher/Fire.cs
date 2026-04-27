using System.Text;
using PLCRegistersParsing.Publisher.Entities;
using PLCRegistersParsing.Publisher.Enums;
using PLCRegistersParsing.Publisher.Services;

namespace PLCRegistersParsing.Publisher;

public class Fire
{
    private const string UnitName = "CWTUnit";
    private Dictionary<string, List<ParameterBase>> UnitParameters { get; set; }
    private Options FiringOptions { get; set; }
    
    private static bool SettingMessageHeader = bool.TryParse(Environment.GetEnvironmentVariable("SET_MESSAGE_HEADER"), out var value) && value;

    private CancellationToken Token { get; set; }

    public Fire(Dictionary<string, List<ParameterBase>> unitParameters, string serialNumber)
    {
        UnitParameters = unitParameters;
        var creds = new ServerCredentials(
            Environment.GetEnvironmentVariable("SERVER_HOST")!,
            int.Parse(Environment.GetEnvironmentVariable("SERVER_PORT")!),
            Environment.GetEnvironmentVariable("SERVER_USER")!,
            Environment.GetEnvironmentVariable("SERVER_PASS")!,
            Environment.GetEnvironmentVariable("UNIT_NAME_PREFIX")!,
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
            WaitChallenge: 2000,
            WaitAck: 2000
        );
        
        var unit = CreateUnit();
        unit.ModuleName = creds.ModuleName;
        unit.SerialNumber = serialNumber;
        Token = new CancellationToken();
        HandleUnit(unit, Token);
    }

    private Unit CreateUnit()
    {
        var unitName = UnitName;
        var unitParameters = UnitParameters;

        Unit unit = new Unit(unitName, FiringOptions, unitParameters);
        return unit;
    }

    private void HandleUnit(Unit unit, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                UnitData unitData = unit.NewUnitData();
                SetUnitDataParams(unitData);
                
                // Send request
                SendInitialRequest(unitData);
                
                // Receive challenge
                ReceiveChallenge(unitData);
                
                // Create the header
                CreateMessage(unitData, settingMessageHeader:SettingMessageHeader);
                
                // Encrypt Message
                EncryptMessage(unitData);
                
                // Assemble Message
                AssembleMessage(unitData);
                
                // Send content
                SendMessage(unitData);
                
                // Confirm Receipt
                ReceiveConfirmationReceipt(unitData);
                
                // Finish connection
                CloseConnection(unitData);
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine($"HandleUnit failed: {e.Message}");
                Console.WriteLine("Retrying...");
                
                Thread.Sleep(1000);
            }
        }
    }
    
    private void SetUnitDataParams(UnitData unitData)
    {
        unitData.ChallengeWaitTime = unitData.Unit!.ChallengeWaitTimeMode;
        unitData.ACKWaitTime =unitData.Unit.ACKWaitTimeMode;
    }
    
    private void SendInitialRequest(UnitData unitData)
    {
        unitData.SetStatus(UnitStatusEnum.Transmitting);
        unitData.SetFirstTransmissionDateTime(DateTime.Now);
        TCPService.Connect(unitData.Client!, FiringOptions.Host, FiringOptions.Port);
        unitData.SetStatus(UnitStatusEnum.WaitingForChallenge);

        Console.WriteLine($"Unit {unitData.Unit!.Name} sending connection request.");
    }
    
    private void ReceiveChallenge(UnitData unitData)
    {
        try
        {
            byte[] receivedChallenge = TCPService.ReadData(unitData.Client!, unitData.ChallengeWaitTime).Result;
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
    
    private void CreateMessage(UnitData unitData, bool settingMessageHeader = true)
    {
        unitData.CreateMessage(settingMessageHeader:settingMessageHeader);
    }
    
    private void EncryptMessage(UnitData unitData)
    {
        string key = EncryptionService.GenerateMD5String($"{unitData.Challenge}{FiringOptions.Password}");

        unitData.ContentBytes = EncryptionService.Encrypt(unitData.OriginalContent!, key);
    }
    
    private void SendMessage(UnitData unitData)
    {
        TCPService.SendData(unitData.Client!, unitData.FullMessageBytes!);
        unitData.SetStatus(UnitStatusEnum.WaitingForACK);
        unitData.SetLastTransmittedDateTime(DateTime.Now);
    }
    
    private void ReceiveConfirmationReceipt(UnitData unitData)
    {
        try
        {
            byte[] receivedConfirmation = TCPService.ReadData(unitData.Client!, unitData.ACKWaitTime).Result;
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
        TCPService.CloseConnection(unitData.Client!);

        unitData.SetStatus(UnitStatusEnum.WaitingToTransmit);
        unitData.Status = UnitStatusEnum.Finished;
    }
    
    private void AssembleMessage(UnitData unitData)
    {
        unitData.AssembleMessage();
    }

}