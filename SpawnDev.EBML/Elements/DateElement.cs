using SpawnDev.EBML.Extensions;

namespace SpawnDev.EBML.Elements
{
    public class DateElement : ElementBase
    {
        /// <summary>
        /// The element type name
        /// </summary>
        public const string TypeName  = "date";
        public DateTime Data
        {
            get
            {
                if (!Exists) return default;
                Stream.Position = DataOffset;
                return Stream.ReadEBMLDate((int)DataSize);
            }
            set
            {
                ReplaceData(EBMLConverter.ToDateBytes(value));
            }
        }
        public DateElement(EBMLDocument document, ElementStreamInfo element) : base(document, element) { }
    }
}
