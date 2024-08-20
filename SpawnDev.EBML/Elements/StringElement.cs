using SpawnDev.EBML.Extensions;
using SpawnDev.EBML.Segments;
using System.Text;

namespace SpawnDev.EBML.Elements
{
    public class StringElement : BaseElement<string>
    {
        public const string TypeName = "string";
        public override string DataString
        {
            get => Data?.ToString() ?? "";
            set => Data = value ?? "";
        }
        public StringElement(EBMLSchemaElement schemaElement, SegmentSource source, ElementHeader? header = null) : base(schemaElement, source, header) { }
        public StringElement(EBMLSchemaElement schemaElement, string value) : base(schemaElement, value) { }
        public StringElement(EBMLSchemaElement schemaElement) : base(schemaElement, string.Empty) { }
        protected override string DataFromSegmentSource() => Encoding.UTF8.GetString(SegmentSource.ReadBytes(0, SegmentSource.Length, true));
        protected override SegmentSource DataToSegmentSource() => new ByteSegment(Encoding.ASCII.GetBytes(Data));
    }
}
