using SpawnDev.EBML.Extensions;
using SpawnDev.EBML.Segments;
using System.Text;

namespace SpawnDev.EBML.Elements
{
    public class UTF8Element : BaseElement<string>
    {
        public const string TypeName  = "utf-8";
        public override string DataString
        {
            get => Data?.ToString() ?? "";
            set => Data = value ?? "";
        }
        public UTF8Element(EBMLSchemaElement schemaElement, SegmentSource source, ElementHeader? header = null) : base(schemaElement, source, header) { }
        public UTF8Element(EBMLSchemaElement schemaElement, string value) : base(schemaElement, value) { }
        public UTF8Element(EBMLSchemaElement schemaElement) : base(schemaElement, string.Empty) { }
        protected override string DataFromSegmentSource() => Encoding.UTF8.GetString(SegmentSource.ReadBytes(0, SegmentSource.Length, true));
        protected override SegmentSource DataToSegmentSource() => new ByteSegment(Encoding.UTF8.GetBytes(Data));
    }
}
