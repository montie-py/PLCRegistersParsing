namespace PLCRegistersParsing.Simulation.ClientLogic
{
    public sealed record PollingValuesHeaders()
    {
        public string? Abbreviation;
        public string? MeasurementUnit;

        public static readonly List<PollingValuesHeaders> PollingValuesHeadersArray = new();

        static PollingValuesHeaders()
        {
            PollingValuesHeadersArray.AddRange(
                new PollingValuesHeaders
                {
                    Abbreviation = "Date",
                    MeasurementUnit = ""
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "Time",
                    MeasurementUnit = ""
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "Milliseconds",
                    MeasurementUnit = ""
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "Worker Inlet Conductivity",
                    MeasurementUnit = "uS"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "Worker Inlet Temperature",
                    MeasurementUnit = "C"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "Worker Pre Pump Pressure",
                    MeasurementUnit = "PSI"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "Worker Post Pump Pressure",
                    MeasurementUnit = "PSI"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "Worker Product Pressure",
                    MeasurementUnit = "PSI"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "Worker Product Flow",
                    MeasurementUnit = "GPM"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "Worker Product Temperature",
                    MeasurementUnit = "C"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "Worker Waste Flow",
                    MeasurementUnit = "GPM"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "Worker Recycle Flow",
                    MeasurementUnit = "GPM"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "Worker Product Conductivity",
                    MeasurementUnit = "uS"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "Polisher Inlet Conductivity",
                    MeasurementUnit = "uS"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "Polisher Inlet Temperature",
                    MeasurementUnit = "C"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "Polisher Pre Pump Pressure",
                    MeasurementUnit = "PSI"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "Polisher Post Pump Pressure",
                    MeasurementUnit = "PSI"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "Polisher Product Pressure",
                    MeasurementUnit = "PSI"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "Polisher Product Flow",
                    MeasurementUnit = "GPM"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "Polisher Product Temperature",
                    MeasurementUnit = "C"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "Polisher Waste Flow",
                    MeasurementUnit = "GPM"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "Polisher Recycle Flow",
                    MeasurementUnit = "GPM"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "Polisher Product Conductivity",
                    MeasurementUnit = "uS"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "System Product Conductivity",
                    MeasurementUnit = "uS"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "-Reserved-",
                    MeasurementUnit = ""
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "-Reserved-",
                    MeasurementUnit = ""
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "-Reserved-",
                    MeasurementUnit = ""
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "Worker Inlet Flow",
                    MeasurementUnit = "GPM, calc"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "Polisher Inlet Flow",
                    MeasurementUnit = "GPM, calc"
                }
            );
        }
    }
}