using SpawnDev.EBML.Extensions;

namespace SpawnDev.EBML.Elements
{
    public class FloatElement : BaseElement
    {
        /// <summary>
        /// The element type name
        /// </summary>
        public const string TypeName = "float";
        protected override string DataToDataString()
        {
            return Data.ToString();
        }
        protected override void DataFromDataString(string value)
        {
            if (double.TryParse(value, out var v))
            {
                Data = v;
            }
        }
        public double Data
        {
            get
            {
                Stream.Position = DataOffset;
                return Stream.ReadEBMLFloat((int)DataSize);
            }
            set
            {
                ReplaceData(EBMLConverter.ToFloatBytes(value));
            }
        }
        public FloatElement(EBMLDocument document, ElementStreamInfo element) : base(document, element) { }
    }
}
