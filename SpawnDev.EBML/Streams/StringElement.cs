using SpawnDev.EBML.Extensions;
using System.Text;

namespace SpawnDev.EBML.Streams
{
    public class StringElement : Element
    {
        public bool IsUTF8 => SchemaElement?.Type == "utf-8";
        public string? Value
        {
            get
            {
                Update();
                var pos = Stream.Position;
                if (!Exists) return default;
                Stream.Position = DataOffset;
                var ret = IsUTF8 ? Stream.ReadEBMLStringUTF8((int)Size!.Value) : Stream.ReadEBMLStringASCII((int)Size!.Value);
                Stream.Position = pos;
                return ret;
            }
            set
            {
                if (Value == value) return;
                Update();
                if (!Exists) return;
                var replacementData = new MemoryStream();
                replacementData.Write((IsUTF8 ? Encoding.UTF8 : Encoding.ASCII).GetBytes(value ?? ""));
                ReplaceData(replacementData);
            }
        }
        public StringElement(StreamElementInfo element) : base(element) { }
    }
}
