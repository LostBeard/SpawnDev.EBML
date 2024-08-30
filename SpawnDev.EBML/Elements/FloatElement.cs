using SpawnDev.EBML.Extensions;

namespace SpawnDev.EBML.Elements
{
    public class FloatElement : Element
    {
        public double Data
        {
            get
            {
                Stream.Position = DataOffset;
                return Stream.ReadEBMLFloat((int)MaxDataSize);
            }
            set
            {
                ReplaceData(EBMLConverter.ToFloatBytes(value));
            }
        }
        public FloatElement(Document document, ElementStreamInfo element) : base(document, element) { }
    }
}
