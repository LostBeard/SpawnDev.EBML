using SpawnDev.EBML.Extensions;
using SpawnDev.EBML.Segments;

namespace SpawnDev.EBML.Elements
{
    public class FloatElement : BaseElement<double>
    {
        public const string TypeName  = "float";
        public override string DataString
        {
            get => Data.ToString();
            set
            {
                if (double.TryParse(value, out var v))
                {
                    Data = v;
                }
            }
        }
        public FloatElement(EBMLSchemaElement schemaElement, SegmentSource source, ElementHeader? header = null) : base(schemaElement, source, header) { }
        public FloatElement(EBMLSchemaElement schemaElement, double value) : base(schemaElement, value) { }
        public FloatElement(EBMLSchemaElement schemaElement) : base(schemaElement, default) { }
        protected override double DataFromSegmentSource() => EBMLConverter.ReadEBMLFloat(SegmentSource.ReadBytes(0, SegmentSource.Length, true));
        protected override SegmentSource DataToSegmentSource() => new ByteSegment(EBMLConverter.ToFloatBytes(Data));
    }
}
