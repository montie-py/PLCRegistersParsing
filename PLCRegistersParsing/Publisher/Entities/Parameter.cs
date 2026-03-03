
namespace PLCRegistersParsing.Publisher.Entities
{
    public class Parameter
    {
        public string Name { get; set; }
        public string Abbreviation { get; set; }
        public string MeasurementUnit { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
    }
}
