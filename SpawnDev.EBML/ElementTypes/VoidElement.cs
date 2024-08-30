using SpawnDev.EBML.Elements;

namespace SpawnDev.EBML.ElementTypes
{
    [ElementName("ebml", "Void")]
    public class VoidElement : BinaryElement
    {
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="element"></param>
        public VoidElement(EBMLDocument document, ElementStreamInfo element) : base(document, element) { }
    }
}
