using SpawnDev.EBML.Extensions;

namespace SpawnDev.EBML.Elements
{
    public class UintElement : ElementBase
    {
        /// <summary>
        /// The element type name
        /// </summary>
        public const string TypeName = "uinteger";
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
