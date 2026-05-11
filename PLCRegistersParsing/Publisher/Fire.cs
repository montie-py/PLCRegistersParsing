using System.Net.Sockets;
using System.Text;
using PLCRegistersParsing.Publisher.Entities;
using PLCRegistersParsing.Publisher.Enums;
using PLCRegistersParsing.Publisher.Services;

namespace PLCRegistersParsing.Publisher;

public class Fire
{
    private const string UnitName = "CWTUnit";
    private Dictionary<string, List<ParameterBase>> UnitParameters { get; set; }

    private static bool IsMessageHeaderSet =
        bool.TryParse(Environment.GetEnvironmentVariable("SET_MESSAGE_HEADER"), out var value) && value;

    private CancellationToken Token { get; set; }

    private Unit Unit { get; set; }

    public Fire(Dictionary<string, List<ParameterBase>> unitParameters, string serialNumber)
    {
        UnitParameters = unitParameters;
        Unit = new Unit();
        CreateUnit(serialNumber);
        Token = new CancellationTokenSource(
            int.Parse(Environment.GetEnvironmentVariable("CONNECTION_TRYING_TIMEOUT_MILLS")!)).Token;
    }

    public async Task FireUnit()
    {
        await HandleUnit();
    }

    private void CreateUnit(string serialNumber)
    {
        Unit.Name = UnitName;
        Unit.UserName = Environment.GetEnvironmentVariable("SERVER_USER")!;
        Unit.Password = Environment.GetEnvironmentVariable("SERVER_PASS")!;
        Unit.Client = new TcpClient();
        Unit.UseEncryption = true;
        Unit.ChallengeWaitTime = 2000;
        Unit.AckWaitTime = 2000;
        Unit.ParametersList = UnitParameters;
        Unit.ModuleName = Environment.GetEnvironmentVariable("MODULE_NAME") ?? "CWT";
        Unit.SerialNumber = serialNumber;
    }

    private async Task HandleUnit()
    {
        while (!Token.IsCancellationRequested)
        {
            try
            {
                // Send request
                await SendInitialRequest();

                // Receive challenge
                await ReceiveChallenge();

                // Create the header
                CreateMessage(IsMessageHeaderSet);

                // Encrypt Message
                EncryptMessage();

                // Assemble Message
                AssembleMessage();

                // Send content
                await SendMessage();

                // Confirm Receipt
                await ReceiveConfirmationReceipt();

                // Finish connection
                CloseConnection();
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine($"HandleUnit failed: {e.Message}");
                Console.WriteLine("Retrying...");

                Thread.Sleep(1000);
                throw new Exception($"HandleUnit failed: {e.Message}");
            }
        }
    }

    private async Task SendInitialRequest()
    {
        Unit.SetStatus(UnitStatusEnum.Transmitting);
        Unit.SetFirstTransmissionDateTime(DateTime.Now);
            
        await TcpService.Connect(Unit.Client!, Environment.GetEnvironmentVariable("SERVER_HOST")!,
            int.Parse(Environment.GetEnvironmentVariable("SERVER_PORT")!), Token);
        Unit.SetStatus(UnitStatusEnum.WaitingForChallenge);

        Console.WriteLine($"Unit {Unit.Name} sending connection request.");
    }

    private async Task ReceiveChallenge()
    {
        try
        {
            byte[] receivedChallenge = await TcpService.ReadData(Unit.Client!, Unit.ChallengeWaitTime);
            string challenge = Encoding.ASCII.GetString(receivedChallenge);
            Unit.SetChallenge(challenge, DateTime.Now);
        }
        catch (AggregateException ex)
        {
            ex.Handle(e =>
            {
                if (e is IOException)
                {
                    Unit.SetStatus(UnitStatusEnum.ChallengeFailed);
                }

                return true;
            });
            throw;
        }
    }

    private void CreateMessage(bool isMessageHeaderSet = true)
    {
        Unit.CreateMessage(isMessageHeaderSet);
    }

    private void EncryptMessage()
    {
        string key =
            EncryptionService.GenerateMD5String(
                $"{Unit.Challenge}{Environment.GetEnvironmentVariable("SERVER_PASS")!}");

        Unit.ContentBytes = EncryptionService.Encrypt(Unit.OriginalContent!, key);
    }

    private async Task SendMessage()
    {
        await TcpService.SendData(Unit.Client!, Unit.FullMessageBytes!, Token);
        Unit.SetStatus(UnitStatusEnum.WaitingForACK);
        Unit.SetLastTransmittedDateTime(DateTime.Now);
    }

    private async Task ReceiveConfirmationReceipt()
    {
        try
        {
            byte[] receivedConfirmation = await TcpService.ReadData(Unit.Client!, Unit.AckWaitTime);
            string challenge = Encoding.ASCII.GetString(receivedConfirmation);
            Unit.SetACKReceived(DateTime.Now);
        }
        catch (AggregateException ex)
        {
            ex.Handle(e =>
            {
                if (e is IOException)
                {
                    Unit.SetStatus(UnitStatusEnum.ACKFailed);
                }

                return true;
            });

            throw;
        }
    }

    private void CloseConnection()
    {
        TcpService.CloseConnection(Unit.Client!);

        Unit.SetStatus(UnitStatusEnum.WaitingToTransmit);
        Unit.CurrentStatus = UnitStatusEnum.Finished;
    }

    private void AssembleMessage()
    {
        Unit.AssembleMessage();
    }
}