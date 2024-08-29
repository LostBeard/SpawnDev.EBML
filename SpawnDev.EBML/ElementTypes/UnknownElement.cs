using SpawnDev.EBML.Elements;

namespace SpawnDev.EBML.ElementTypes
{
    public class UnknownElement : Element
    {
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="element"></param>
        public UnknownElement(Document document, ElementStreamInfo element) : base(document, element) { }
    }
}
