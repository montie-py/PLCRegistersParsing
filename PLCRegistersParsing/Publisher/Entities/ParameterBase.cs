
namespace PLCRegistersParsing.Publisher.Entities
{
    public abstract class ParameterBase
    {
        public string Name { get; set; }
        public string Abbreviation { get; set; }
        public string MeasurementUnit { get; set; }
        public object Value { get; set; }
    }
}
