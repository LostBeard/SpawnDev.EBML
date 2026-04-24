using SpawnDev.EBML.Extensions;

namespace SpawnDev.EBML.Elements
{
    public class DateElement : BaseElement
    {
        /// <summary>
        /// The element type name
        /// </summary>
        public const string TypeName  = "date";
        protected override string DataToDataString()
        {
            return Data.ToString();
        }
        protected override void DataFromDataString(string value)
        {
            if (DateTime.TryParse(value, out var v))
            {
                Data = v;
            }
        }
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
