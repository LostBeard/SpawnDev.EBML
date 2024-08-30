using SpawnDev.EBML.Elements;

namespace SpawnDev.EBML.Matroska
{
    [ElementName("webm", "Cluster")]
    [ElementName("matroska", "Cluster")]
    public class Cluster : MasterElement
    {
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="element"></param>
        public Cluster(EBMLDocument document, ElementStreamInfo element) : base(document, element) { }
    }
}
