using SpawnDev.EBML.Elements;

namespace SpawnDev.EBML.Matroska
{
    [ElementName(nameof(Block), "matroska", "webm")]
    public class Block : BaseElement
    {
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="element"></param>
        public Block(EBMLDocument document, ElementStreamInfo element) : base(document, element) { }
    }
}
