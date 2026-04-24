using SpawnDev.EBML.Elements;

namespace SpawnDev.EBML.Matroska
{
    [ElementName(nameof(Cluster), "matroska", "webm")]
    public class Cluster : MasterElement
    {
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="document"></param>
        /// <param name="element"></param>
        public Cluster(EBMLDocument document, ElementStreamInfo element) : base(document, element) { }
        /// <summary>
        /// Creates a new, detached instance
        /// </summary>
        public Cluster() : base() { }
    }
}
