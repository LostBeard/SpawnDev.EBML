using SpawnDev.EBML.Extensions;

namespace SpawnDev.EBML.Elements
{
    public class FloatElement : Element
    {
        public double Data
        {
            get
            {
                Stream.LatestStable.Position = DataOffset;
                return Stream.LatestStable.ReadEBMLFloat((int)MaxDataSize);
            }
            set
            {
                ReplaceData(EBMLConverter.ToFloatBytes(value));
            }
        }
        public FloatElement(Document document, ElementStreamInfo element) : base(document, element) { }
    }
}
