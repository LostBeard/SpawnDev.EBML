using SpawnDev.EBML.Extensions;

namespace SpawnDev.EBML.Elements
{
    public class UintElement : Element
    {
        public ulong Data
        {
            get
            {
                Stream.LatestStable.Position = DataOffset;
                return Stream.LatestStable.ReadEBMLUInt((int)MaxDataSize);
            }
            set
            {
                ReplaceData(EBMLConverter.ToUIntBytes(value));
            }
        }
        public UintElement(Document document, ElementStreamInfo element) : base(document, element) { }
    }
}
