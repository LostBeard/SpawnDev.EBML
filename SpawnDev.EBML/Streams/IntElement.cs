using SpawnDev.EBML.Extensions;
using SpawnDev.PatchStreams;

namespace SpawnDev.EBML.Streams
{
    public class IntElement : Element
    {
        public long Value
        {
            get
            {
                Update();
                var pos = Stream.Position;
                if (!Exists) return default;
                Stream.Position = DataOffset;
                var ret = Stream.ReadEBMLInt((int)MaxDataSize);
                Stream.Position = pos;
                return ret;
            }
            set
            {
                if (Value == value) return;
                Update();
                if (!Exists) return;
                var pos = Stream.Position;
                var data = EBMLConverter.ToIntBytes(value);
                var replacementData = new MemoryStream();
                replacementData.WriteEBMLElementIdRaw(Id);
                replacementData.WriteEBMLElementSize((ulong)data.Length);
                replacementData.Write(data);
                Stream.Position = Offset;
                Stream.Insert(replacementData, MaxTotalSize);
                Stream.Position = pos;
            }
        }
        public IntElement(StreamElementInfo element) : base(element) { }
    }
}
