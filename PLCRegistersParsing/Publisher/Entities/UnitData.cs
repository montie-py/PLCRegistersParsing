using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using PLCRegistersParsing.Publisher.Enums;
using PLCRegistersParsing.Publisher.Services;

namespace PLCRegistersParsing.Publisher.Entities
{
    public class UnitData
    {
        public Unit? Unit { get; set; }
        public int ChallengeWaitTime { get; set; }
        public int ACKWaitTime { get; set; }
        public DateTime LatestUpdate { get; private set; }
        public DateTime FirstTransmissionDateTime { get; private set; }
        public DateTime ChallengeReceivedDateTime { get; private set; }
        public DateTime ACKReceivedDateTime { get; private set; }
        public DateTime LastTransmittedDateTime { get; private set; }
        public DateTime LastReceivedDateTime { get; private set; }
        public UnitStatusEnum Status { get; set; }
        public string? Challenge { get; private set; }
        public string? HashedPassword { get; set; }
        public string? OriginalHeader { get; set; }
        public string? OriginalContent { get; set; }
        public byte[]? OriginalContentBytesArray { get; set; }
        public string? OriginalFullMessage { get; set; }
        public byte[]? OriginalFullMessageBytes { get; set; }
        public byte[]? HeaderBytes { get; set; }
        public byte[]? ContentBytes { get; set; }
        public byte[]? FullMessageBytes { get; set; }
        public TcpClient? Client { get; set; }

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
            string hashedPassword = EncryptionService.GenerateMD5String($"{Unit!.Password}{Challenge}");
            HashedPassword = hashedPassword;
        }

        public void SetStatus(UnitStatusEnum status)
        {
            Status = status;
            if (Unit != null) Unit.CurrentStatus = status;
        }

        public void CreateMessage(bool settingMessageHeader = true)
        {
            SetHeader();
            GenerateMessage(settingMessageHeader:settingMessageHeader);
            OriginalFullMessage = OriginalHeader + OriginalContent;
        }

        public void AssembleMessage()
        {
            FullMessageBytes = null;
            if (OriginalHeader == null || OriginalContent == null) return;
            ContentBytes = ContentBytes == null ? Encoding.UTF8.GetBytes(OriginalContent) : ContentBytes;
            HeaderBytes = Encoding.UTF8.GetBytes(OriginalHeader);
            FullMessageBytes = HeaderBytes.Concat(ContentBytes).ToArray();

        }

        private void SetHeader()
        {
            SetHeashedPassword();
            int transmissionIntervalSeconds = 60;

            OriginalHeader =
                $"CMD=1&MODULE={Unit!.ModuleName}&V=1.0&SN={Unit.SerialNumber}&NAME={Unit.Name}&INT={transmissionIntervalSeconds}&USR=\"{Unit.UserName}\"&PSW=\"{HashedPassword}\"";

            if (Unit.UseEncryption)
            {
                OriginalHeader += "&AES=128";
            }

            OriginalHeader += "\r\n";
        }

        private void GenerateMessage(bool settingMessageHeader = true)
        {
            string message = "";
            int parametersListLoopCounter = 0;
            foreach (KeyValuePair<string, List<ParameterBase>> entry in Unit!.ParametersList)
            {
                if (settingMessageHeader)
                {
                    message += SetMeasurementsHeader(parameters:entry.Value);
                }
            
                message += GenerateMeasurements(timeStamp:entry.Key, parameters:entry.Value);

                if (parametersListLoopCounter == Unit.ParametersList.Count - 1)
                {
                    message += "\r\n";
                }
            }
            
            OriginalContent = message + (char)13 + (char)10 + (char)26;
        }

        private string SetMeasurementsHeader(List<ParameterBase> parameters)
        {
            string messageHeader = "L";
            foreach (ParameterBase parameter in parameters)
            {
                messageHeader += $";{parameter.Abbreviation};{parameter.Name};{parameter.MeasurementUnit}";
            }

            messageHeader += "\r\n";

            return messageHeader;
        }

        private string GenerateMeasurements(string timeStamp, List<ParameterBase> parameters)
        {
            string measurementLine = $"D;{timeStamp}";

            foreach (ParameterBase parameter in parameters)
            {
                measurementLine += $";{parameter.Abbreviation};{((StringParameter)parameter).Value}";
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