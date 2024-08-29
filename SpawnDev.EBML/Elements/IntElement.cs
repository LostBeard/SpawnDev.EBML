using SpawnDev.EBML.Extensions;
using SpawnDev.PatchStreams;

namespace SpawnDev.EBML.Elements
{
    public class IntElement : Element
    {
        public long Data
        {
            get
            {
                Stream.LatestStable.Position = DataOffset;
                return Stream.LatestStable.ReadEBMLInt((int)MaxDataSize);
            }
            set
            {
                ReplaceData(EBMLConverter.ToIntBytes(value));
            }
        }
        public IntElement(Document document, ElementStreamInfo element) : base(document, element) { }
    }
}
