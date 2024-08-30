using SpawnDev.EBML.Extensions;
using SpawnDev.PatchStreams;

namespace SpawnDev.EBML.Elements
{
    public class IntElement : ElementBase
    {
        /// <summary>
        /// The element type name
        /// </summary>
        public const string TypeName  = "integer";
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
