using SpawnDev.EBML.Elements;

namespace SpawnDev.EBML.ElementTypes
{
    [ElementName("ebml", "EBML")]
    public class EBMLHeader : MasterElement
    {
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="element"></param>
        public EBMLHeader(EBMLDocument document, ElementStreamInfo element) : base(document, element) { }
    }
}
