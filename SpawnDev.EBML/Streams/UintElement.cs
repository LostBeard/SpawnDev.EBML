using SpawnDev.EBML.Extensions;

namespace SpawnDev.EBML.Streams
{
    public class UintElement : Element
    {
        public ulong Value
        {
            get
            {
                Update();
                var pos = Stream.Position;
                if (!Exists) return default;
                Stream.Position = DataOffset;
                var ret = Stream.ReadEBMLUInt((int)MaxDataSize);
                Stream.Position = pos;
                return ret;
            }
            set
            {
                if (Value == value) return;
                Update();
                if (!Exists) return;
                var pos = Stream.Position;
                var data = EBMLConverter.ToUIntBytes(value);
                var replacementData = new MemoryStream();
                replacementData.WriteEBMLElementIdRaw(Id);
                replacementData.WriteEBMLElementSize((ulong)data.Length);
                replacementData.Write(data);
                Stream.Position = Offset;
                Stream.Insert(replacementData, MaxTotalSize);
                Stream.Position = pos;
            }
        }
        public UintElement(StreamElementInfo element) : base(element) { }
    }
}
