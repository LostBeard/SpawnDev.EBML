using System.Text;

namespace SpawnDev.EBML
{
    public class StringElement : BaseElement<string>
    {
        public static explicit operator string?(StringElement? element) => element == null ? null : element.Data;
        public StringElement(Enum id) : base(id) { }
        public virtual Encoding Encoding { get; } = Encoding.ASCII;
        public StringElement(Enum id, string value) : base(id)
        {
            Data = value;
        }
        public override void UpdateBySource()
        {
            _DataValue = new Lazy<string>(() =>
            {
                Stream!.Position = 0;
                return Stream!.ReadEBMLString((int)Stream.Length, Encoding);
            });
        }
        public override void UpdateByData()
        {
            _DataStream = new Lazy<SegmentSource?>(() =>
            {;
                return new ByteSegment(Encoding.GetBytes(Data));
            });
        }
    }
    public class UTF8StringElement : StringElement
    {
        public static explicit operator string?(UTF8StringElement? element) => element == null ? null : element.Data;
        public UTF8StringElement(Enum id) : base(id) { }
        public override Encoding Encoding { get; } = Encoding.UTF8;
        public UTF8StringElement(Enum id, string value) : base(id, value) { }
    }
}
