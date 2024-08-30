using SpawnDev.EBML.Elements;

namespace SpawnDev.EBML.ElementTypes
{
    public class UnknownElement : ElementBase
    {
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="element"></param>
        public UnknownElement(EBMLDocument document, ElementStreamInfo element) : base(document, element) { }
    }
}
