using SpawnDev.EBML.Elements;

namespace SpawnDev.EBML.Matroska
{
    [ElementName("webm", "SimpleBlock")]
    [ElementName("matroska", "SimpleBlock")]
    public class SimpleBlock : BinaryElement
    {
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="element"></param>
        public SimpleBlock(EBMLDocument document, ElementStreamInfo element) : base(document, element) { }
    }
}
