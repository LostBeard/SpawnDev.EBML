using SpawnDev.EBML.Extensions;

namespace SpawnDev.EBML.Elements
{
    public class DateElement : Element
    {
        public DateTime Data
        {
            get
            {
                if (!Exists) return default;
                Stream.LatestStable.Position = DataOffset;
                return Stream.LatestStable.ReadEBMLDate((int)MaxDataSize);
            }
            set
            {
                ReplaceData(EBMLConverter.ToDateBytes(value));
            }
        }
        public DateElement(Document document, ElementStreamInfo element) : base(document, element) { }
    }
}
