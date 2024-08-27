using SpawnDev.EBML.Extensions;

namespace SpawnDev.EBML.Streams
{
    /// <summary>
    /// Basic read all, write all binary element<br/>
    /// </summary>
    public class BinaryElement : Element
    {
        public byte[] Value
        {
            get
            {
                Update();
                var pos = Stream.Position;
                if (!Exists) return default;
                var ret = new byte[Size!.Value];
                _ = Stream.Read(ret, 0, (int)Size!.Value);
                Stream.Position = pos;
                return ret;
            }
            set
            {
                if (Value == value) return;
                Update();
                if (!Exists) return;
                var pos = Stream.Position;
                var replacementData = new MemoryStream();
                replacementData.WriteEBMLElementIdRaw(Id);
                replacementData.WriteEBMLElementSize((ulong)value.Length);
                replacementData.Write(value);
                Stream.Position = Offset;
                Stream.Insert(replacementData, MaxTotalSize);
                Stream.Position = pos;
            }
        }
        public BinaryElement(StreamElementInfo element) : base(element) { }
    }
}
