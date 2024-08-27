using SpawnDev.EBML.Extensions;
using SpawnDev.PatchStreams;

namespace SpawnDev.EBML.Streams
{
    public class DateElement : Element
    {
        public DateTime Value
        {
            get
            {
                Update();
                if (!Exists) return default;
                var pos = Stream.Position;
                Stream.Position = DataOffset;
                var ret = Stream.ReadEBMLDate((int)MaxDataSize);
                Stream.Position = pos;
                return ret;
            }
            set
            {
                if (Value == value) return;
                Update();
                if (!Exists) return;
                var pos = Stream.Position;
                var data = EBMLConverter.ToDateBytes(value);
                var replacementData = new MemoryStream();
                replacementData.WriteEBMLElementIdRaw(Id);
                replacementData.WriteEBMLElementSize((ulong)data.Length);
                replacementData.Write(data);
                Stream.Position = Offset;
                Stream.Insert(replacementData, MaxTotalSize);
                Stream.Position = pos;
            }
        }
        public DateElement(StreamElementInfo element) : base(element) { }
    }
}
