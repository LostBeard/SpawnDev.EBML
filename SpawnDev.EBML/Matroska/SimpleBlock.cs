using SpawnDev.EBML.Elements;

namespace SpawnDev.EBML.Matroska
{
    [ElementName(nameof(SimpleBlock), "matroska", "webm")]
    public class SimpleBlock : BinaryElement
    {
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="element"></param>
        public SimpleBlock(EBMLDocument document, ElementStreamInfo element) : base(document, element) { }
    }
}
