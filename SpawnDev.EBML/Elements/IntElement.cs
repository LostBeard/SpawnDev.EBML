using SpawnDev.EBML.Extensions;
using SpawnDev.PatchStreams;

namespace SpawnDev.EBML.Elements
{
    public class IntElement : BaseElement
    {
        /// <summary>
        /// The element type name
        /// </summary>
        public const string TypeName  = "integer";
        protected override string DataToDataString()
        {
            return Data.ToString();
        }
        protected override void DataFromDataString(string value)
        {
            if (long.TryParse(value, out var v))
            {
                Data = v;
            }
        }
        public long Data
        {
            get
            {
                Stream.Position = DataOffset;
                return Stream.ReadEBMLInt((int)DataSize);
            }
            set
            {
                ReplaceData(EBMLConverter.ToIntBytes(value));
            }
        }
        public IntElement(EBMLDocument document, ElementStreamInfo element) : base(document, element) { }
    }
}
