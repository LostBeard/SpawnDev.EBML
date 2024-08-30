using SpawnDev.EBML.Elements;

namespace SpawnDev.EBML.Matroska
{
    [ElementName("webm", "Block")]
    [ElementName("matroska", "Block")]
    public class Block : ElementBase
    {
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="element"></param>
        public Block(EBMLDocument document, ElementStreamInfo element) : base(document, element) { }
    }
}
