using SpawnDev.PatchStreams;

namespace SpawnDev.EBML.Elements
{
    /// <summary>
    /// Basic read all, write all binary element<br/>
    /// </summary>
    public class BinaryElement : ElementBase
    {
        /// <summary>
        /// The element type name
        /// </summary>
        public const string TypeName = "binary";
        /// <summary>
        /// Returns the element's data as  PatchStream stream.<br/>
        /// Editing the returned stream will not modify the element
        /// </summary>
        public PatchStream Data
        {
            get => ElementStreamDataSlice();
            set
            {
                // Out of sync element values cannot be set
                ReplaceData(value);
            }
        }
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="element"></param>
        public BinaryElement(EBMLDocument document, ElementStreamInfo element) : base(document, element) { }
    }
}
