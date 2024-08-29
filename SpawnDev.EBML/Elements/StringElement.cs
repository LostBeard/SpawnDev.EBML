using SpawnDev.EBML.Extensions;
using System.Text;

namespace SpawnDev.EBML.Elements
{
    public class StringElement : Element
    {
        public string Data
        {
            get
            {
                Stream.LatestStable.Position = DataOffset;
                return IsUTF8 ? Stream.LatestStable.ReadEBMLStringUTF8((int)Size!.Value) : Stream.LatestStable.ReadEBMLStringASCII((int)Size!.Value);
            }
            set
            {
                ReplaceData((IsUTF8 ? Encoding.UTF8 : Encoding.ASCII).GetBytes(value ?? ""));
            }
        }
        public bool IsUTF8 => SchemaElement?.Type == "utf-8";
        public StringElement(Document document, ElementStreamInfo element) : base(document, element) { }
    }
}
