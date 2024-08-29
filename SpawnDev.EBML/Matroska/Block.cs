using SpawnDev.EBML.Elements;

namespace SpawnDev.EBML.Matroska
{
    [ElementName("webm", "Block")]
    [ElementName("matroska", "Block")]
    public class Block : Element
    {
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="element"></param>
        public Block(Document document, ElementStreamInfo element) : base(document, element) { }
    }
}
