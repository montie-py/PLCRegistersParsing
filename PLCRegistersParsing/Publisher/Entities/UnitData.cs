
using System.Net.Sockets;
using System.Text;
using UniBot.Enums;
using UniBot.Services;

namespace PLCRegistersParsing.Publisher.Entities
{
    public class UnitData
    {
        public Unit Unit { get; set; }
        public int ChallengeWaitTime { get; set; }
        public int ACKWaitTime { get; set; }
        public DateTime LatestUpdate { get; private set; }
        public DateTime FirstTransmissionDateTime { get; private set; }
        public DateTime ChallengeReceivedDateTime { get; private set; }
        public DateTime ACKReceivedDateTime { get; private set; }
        public DateTime LastTransmittedDateTime { get; private set; }
        public DateTime LastReceivedDateTime { get; private set; }
        public UnitStatusEnum Status { get; set; }
        public string Challenge { get; private set; }
        public string HashedPassword { get; set; }
        public string OriginalHeader { get; set; }
        public string OriginalContent { get; set; }
        public string OriginalFullMessage { get; set; }
        public byte[] HeaderBytes { get; set; }
        public byte[] ContentBytes { get; set; }
        public byte[] FullMessageBytes { get; set; }
        public TcpClient Client { get; set; }

        public UnitData()
        {
        }

        public UnitData(Unit unit)
        {
            Client = new TcpClient();
            Unit = unit;
        }

        public void SetFirstTransmissionDateTime(DateTime dateTime)
        {
            FirstTransmissionDateTime = dateTime;
            SetLastTransmittedDateTime(dateTime);
        }

        public void SetLastTransmittedDateTime(DateTime dateTime)
        {
            LastTransmittedDateTime = dateTime;
            LatestUpdate = dateTime;
        }

        public void SetLastReceivedDateTime(DateTime dateTime)
        {
            LastReceivedDateTime = dateTime;
            LatestUpdate = dateTime;
        }

        public void SetChallenge(string challenge, DateTime dateTime)
        {
            Challenge = challenge.Substring(4, challenge.Length - 2 - 4);
            ChallengeReceivedDateTime = dateTime;
            SetLastReceivedDateTime(dateTime);
        }

        public void SetACKReceived(DateTime dateTime)
        {
            ACKReceivedDateTime = dateTime;
            SetLastReceivedDateTime(dateTime);
        }

        private void SetHeashedPassword()
        {
            string hashedPassword = EncryptionService.GenerateMD5String($"{Unit.Password}{Challenge}");
            HashedPassword = hashedPassword;
        }

        public void SetStatus(UnitStatusEnum status)
        {
            Status = status;
            Unit.CurrentStatus = status;
        }

        public void CreateMessage()
        {
            SetHeader();
            GenerateMessage();
            OriginalFullMessage = OriginalHeader + OriginalContent;
        }

        public void AssembleMessage()
        {
            ContentBytes = ContentBytes == null ? Encoding.UTF8.GetBytes(OriginalContent) : ContentBytes;
            HeaderBytes = Encoding.UTF8.GetBytes(OriginalHeader);
            FullMessageBytes = HeaderBytes.Concat(ContentBytes).ToArray();
        }

        private void SetHeader()
        {
            SetHeashedPassword();
            int transmissionIntervalSeconds = Unit.TransmissionInterval * 60;

            OriginalHeader = $"CMD=1&MODULE=UNIB&V=1.0&SN={Unit.Name}&NAME=\"\"&INT={transmissionIntervalSeconds}&USR=\"{Unit.UserName}\"&PSW=\"{HashedPassword}\"";

            if (Unit.UseEncryption)
            {
                OriginalHeader += "&AES=128";
            }
            OriginalHeader += "\r\n";
        }

        private void GenerateMessage()
        {
            // Checks how many measurements are necessary
            int measurementQuantity = Unit.TransmissionInterval / Unit.MeasurementInterval;
            string message;

            message = SetMeasurementsHeader(Unit.Parameters);

            for (int i = measurementQuantity - 1; i >= 0; i--)
            {
                string measurementDateTime = DateTime.UtcNow.AddMinutes(-i).ToString("yyMMddHHmmss");

                Random rnd = new Random();
                int randomNumber = rnd.Next(0, 30);

                // Randomly generates a system error just for fun
                if (randomNumber == 0)
                {
                    message += GenerateSystemErrorLog(measurementDateTime);
                }

                // Generates measurements
                message += GenerateMeasurements(Unit.Parameters, measurementDateTime);
            }

            OriginalContent = message + (char)13 + (char)10 + (char)26;
        }

        private string SetMeasurementsHeader(List<Parameter> parameters)
        {
            string messageHeader = "L";
            foreach (Parameter parameter in parameters)
            {
                messageHeader += $";{parameter.Abbreviation};{parameter.Name};{parameter.MeasurementUnit}";
            }
            messageHeader += "\r\n";

            return messageHeader;
        }

        private string GenerateMeasurements(List<Parameter> parameters, string measurementTime)
        {
            string measurementLine = $"D;{measurementTime}";

            foreach (Parameter parameter in parameters)
            {
                Random rnd = new Random();
                int number = rnd.Next(0, 30);
                double randomNumber = 0;

                if (number <= 15)
                {
                    randomNumber = rnd.NextDouble() * (parameter.MaxValue - parameter.MinValue) + parameter.MinValue;
                }
                else
                {
                    randomNumber = rnd.Next(Convert.ToInt32(parameter.MinValue), Convert.ToInt32(parameter.MaxValue));
                }

                measurementLine += $";{parameter.Abbreviation};{randomNumber}";
            }
            measurementLine += "\r\n";

            return measurementLine;
        }

        private string GenerateSystemErrorLog(string errorDate)
        {
            Random rnd = new Random();
            int randomNumber = rnd.Next(0, SystemError.SystemErrorsQuantity);

            string errorMessage = SystemError.GetSystemError(randomNumber);

            string content = $"S;{errorDate};{errorMessage}\r\n";

            return content;
        }
    }
}
