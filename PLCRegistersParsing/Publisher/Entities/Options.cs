
namespace PLCRegistersParsing.Publisher.Entities
{
    public class Options
    {
        public string ServerAddress { get; set; }
        public int ServerPort { get; set; }
        public bool Encrypt { get; set; }
        public string UnitNamePrefix { get; set; }
        public string UnitNameSufix { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public Option UnitsQuantity { get; set; }
        public Option TransmissionDelay { get; set; }
        public Option UnitTransmissionInterval { get; set; }
        public Option NumberOfParameters { get; set; }
        public Option MeasurementTimeInterval { get; set; }
        public Option WaitForChallenge { get; set; }
        public Option WaitForACK { get; set; }

    }


}
