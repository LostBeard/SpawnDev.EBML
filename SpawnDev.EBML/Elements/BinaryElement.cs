using SpawnDev.PatchStreams;

namespace SpawnDev.EBML.Elements
{
    /// <summary>
    /// Basic read all, write all binary element<br/>
    /// </summary>
    public class BinaryElement : BaseElement
    {
        protected override string DataToDataString()
        {
            var chunkSize = DataSize <= 8 ? DataSize : 8;
            var chunk = new byte[chunkSize];
            _ = Stream.Read(chunk);
            return DataSize <= 8 ? "0x" + Convert.ToHexString(chunk) : "0x" + Convert.ToHexString(chunk) + "...";
        }
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
            get => ElementToDataSlice();
            set
            {
                // Out of sync element values cannot be set
                ReplaceData(value);
            }
        }
        VolatileData<string> _DataString;
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="element"></param>
        public BinaryElement(EBMLDocument document, ElementStreamInfo element) : base(document, element)
        {

        }
    }
}
