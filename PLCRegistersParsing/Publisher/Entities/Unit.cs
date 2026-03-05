
using PLCRegistersParsing.Publisher.Enums;

namespace PLCRegistersParsing.Publisher.Entities
{
    public class Unit
    {
        public string Name { get; private set; }
        public string UserName { get; private set; }
        public string Password { get; private set; }
        public bool UseEncryption { get; private set; }
        public UnitStatusEnum CurrentStatus { get; set; }
        public int MeasurementInterval { get; set; }
        public int TransmissionInterval { get; set; }
        public int ChallengeWaitTimeMode { get; set; }
        public int ACKWaitTimeMode { get; set; }

        public List<Parameter> Parameters { get; private set; }
        public List<UnitData> UnitData { get; private set; }


        public Unit(string name, Options options, List<Parameter> parameters, int transmissionInterval, int measurementInterval)
        {
            Name = name;
            UserName = options.Username;
            Password = options.Password;
            UseEncryption = options.UseEncryption;
            ChallengeWaitTimeMode = options.WaitChallenge;
            ACKWaitTimeMode = options.WaitAck;
            UnitData = new List<UnitData>();
            Parameters = parameters;
            TransmissionInterval = transmissionInterval;
            MeasurementInterval = measurementInterval > transmissionInterval ? transmissionInterval : measurementInterval;
        }

        public UnitData GetLatestData()
        {
            UnitData latestData = UnitData.OrderByDescending(x => x.LatestUpdate).FirstOrDefault();

            if (latestData == null)
            {
                latestData = new UnitData();
            }
            
            return latestData;
        }

        public UnitData NewUnitData()
        {
            UnitData unitData = new UnitData(this);

            UnitData.Add(unitData);

            return unitData;
        }
    }
}
