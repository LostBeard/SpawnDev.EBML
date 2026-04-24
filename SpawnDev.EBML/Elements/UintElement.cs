using SpawnDev.EBML.Extensions;

namespace SpawnDev.EBML.Elements
{
    public class UintElement : BaseElement
    {
        /// <summary>
        /// The element type name
        /// </summary>
        public const string TypeName = "uinteger";
        protected override string DataToDataString()
        {
            return Data.ToString();
        }
        protected override void DataFromDataString(string value)
        {
            if (ulong.TryParse(value, out var v))
            {
                Data = v;
            }
        }
        public ulong Data
        {
            get
            {
                Stream.Position = DataOffset;
                return Stream.ReadEBMLUInt((int)DataSize);
            }
            set
            {
                ReplaceData(EBMLConverter.ToUIntBytes(value));
            }
        }
        public UintElement(EBMLDocument document, ElementStreamInfo element) : base(document, element) { }
    }
}
