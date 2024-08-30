using SpawnDev.EBML.Elements;

namespace SpawnDev.EBML.Matroska
{
    [ElementName("webm", "Segment")]
    [ElementName("matroska", "Segment")]
    public class Segment : MasterElement
    {
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="element"></param>
        public Segment(EBMLDocument document, ElementStreamInfo element) : base(document, element) { }
    }
}
