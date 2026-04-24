using SpawnDev.EBML.Elements;

namespace SpawnDev.EBML.Matroska
{
    [ElementName(nameof(Segment), "matroska", "webm")]
    public class Segment : MasterElement
    {
        /// <summary>
        /// Creates a Segment instance to represent one found in a Document
        /// </summary>
        public Segment(EBMLDocument document, ElementStreamInfo element) : base(document, element) { }
        /// <summary>
        /// Creates a new, detached instance
        /// </summary>
        public Segment()
        {
            Info.Id = 0;
            Info.Depth = -1;
            Info.Offset = 0;
            Info.Exists = true;
        }
    }
}
