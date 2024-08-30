using SpawnDev.EBML.Extensions;

namespace SpawnDev.EBML.Elements
{
    public class FloatElement : ElementBase
    {
        /// <summary>
        /// The element type name
        /// </summary>
        public const  string TypeName  = "float";
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
