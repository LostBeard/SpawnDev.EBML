using SpawnDev.EBML.Extensions;
using System.Text;

namespace SpawnDev.EBML.Elements
{
    public class StringElement : ElementBase
    {
        /// <summary>
        /// The element type name
        /// </summary>
        public const string TypeNameString = "string";
        /// <summary>
        /// The element type name
        /// </summary>
        public const string TypeNameUTF8  = "utf-8";
        public string Data
        {
            get
            {
                Stream.Position = DataOffset;
                return IsUTF8 ? Stream.ReadEBMLStringUTF8((int)Size!.Value) : Stream.ReadEBMLStringASCII((int)Size!.Value);
            }
            set
            {
                ReplaceData((IsUTF8 ? Encoding.UTF8 : Encoding.ASCII).GetBytes(value ?? ""));
            }
        }
        public bool IsUTF8 => SchemaElement?.Type == "utf-8";
        public StringElement(EBMLDocument document, ElementStreamInfo element) : base(document, element) { }
    }
}
