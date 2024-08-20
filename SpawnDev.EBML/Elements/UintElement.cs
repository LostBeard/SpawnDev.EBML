using SpawnDev.EBML.Extensions;
using SpawnDev.EBML.Segments;

namespace SpawnDev.EBML.Elements
{
    public class UintElement : BaseElement<ulong>
    {
        public const string TypeName = "uinteger";

        public override string DataString
        {
            get => Data.ToString();
            set
            {
                if (ulong.TryParse(value, out var v))
                {
                    Data = v;
                }
            }
        }
        public UintElement(EBMLSchemaElement schemaElement, SegmentSource source, ElementHeader? header = null) : base(schemaElement, source, header) { }
        public UintElement(EBMLSchemaElement schemaElement, ulong value) : base(schemaElement, value) { }
        public UintElement(EBMLSchemaElement schemaElement) : base(schemaElement, default) { }
        protected override ulong DataFromSegmentSource() => EBMLConverter.ReadEBMLUInt(SegmentSource.ReadBytes(0, SegmentSource.Length, true));
        protected override SegmentSource DataToSegmentSource() => new ByteSegment(EBMLConverter.ToUIntBytes(Data));
    }
}
