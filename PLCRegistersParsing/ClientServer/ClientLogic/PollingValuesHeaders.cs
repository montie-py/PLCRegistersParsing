using System.Collections.Generic;

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
                    Abbreviation = "DATE",
                    MeasurementUnit = ""
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "TIME",
                    MeasurementUnit = ""
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "MILLIS",
                    MeasurementUnit = ""
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "WIC",
                    MeasurementUnit = "uS"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "WIT",
                    MeasurementUnit = "C"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "WPrePP",
                    MeasurementUnit = "PSI"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "WPosPP",
                    MeasurementUnit = "PSI"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "WPP",
                    MeasurementUnit = "PSI"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "WPF",
                    MeasurementUnit = "GPM"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "WPT",
                    MeasurementUnit = "C"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "WWF",
                    MeasurementUnit = "GPM"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "WRF",
                    MeasurementUnit = "GPM"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "WPC",
                    MeasurementUnit = "uS"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "PIC",
                    MeasurementUnit = "uS"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "PIT",
                    MeasurementUnit = "C"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "PPrePP",
                    MeasurementUnit = "PSI"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "PPosPP",
                    MeasurementUnit = "PSI"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "PPP",
                    MeasurementUnit = "PSI"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "PPF",
                    MeasurementUnit = "GPM"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "PPT",
                    MeasurementUnit = "C"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "PWF",
                    MeasurementUnit = "GPM"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "PRF",
                    MeasurementUnit = "GPM"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "PPC",
                    MeasurementUnit = "uS"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "SPC",
                    MeasurementUnit = "uS"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "RES1",
                    MeasurementUnit = ""
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "RES2",
                    MeasurementUnit = ""
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "RES3",
                    MeasurementUnit = ""
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "WIF",
                    MeasurementUnit = "GPM, calc"
                },
                new PollingValuesHeaders
                {
                    Abbreviation = "PIF",
                    MeasurementUnit = "GPM, calc"
                }
            );
        }
    }
}