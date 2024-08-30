using SpawnDev.EBML.Elements;
using SpawnDev.PatchStreams;

namespace SpawnDev.EBML.ElementTypes
{
    /// <summary>
    /// CRC-32 element<br/>
    /// </summary>
    [ElementName("ebml", "CRC-32")]
    public class CRC32Element : ElementBase
    {
        /// <summary>
        /// Returns the element's data as  PatchStream stream.<br/>
        /// Editing the returned stream will not modify the element
        /// </summary>
        public byte[] Data
        {
            get
            {
                var bytes = new byte[DataSize];
                Stream.LatestStable.Position = DataOffset;
                _ = Stream.LatestStable.Read(bytes);
                return bytes;
            }
            set
            {
                ReplaceData(value);
            }
        }
        public override string ToString()
        {
            return "0x" + Convert.ToHexString(Data);
        }
        /// <summary>
        /// Calculates a CRC for this element's data
        /// </summary>
        /// <returns></returns>
        public byte[] CalculateCRC()
        {
            var parent = Parent;
            if (parent == null) throw new Exception("No parent");
            var allButCrc = parent.Children.Where(o => o.Name != "CRC-32").ToList();
            var dataToCRC = allButCrc.Select(o => o.ElementStreamSlice()).ToList();
            using var stream = new PatchStream(dataToCRC);
            var hash = CRC.ComputeHash(stream);
            return hash;
        }
        /// <summary>
        /// Returns true if the CRC-32 value was updated
        /// </summary>
        /// <returns></returns>
        public bool UpdateCRC()
        {
            var crc = CalculateCRC();
            var currentCRC = Data;
            if (!currentCRC.SequenceEqual(crc))
            {
                Data = crc;
                return true;
            }
            return false;
        }
        public bool VerifyCRC()
        {
            var crc = CalculateCRC();
            var currentCRC = Data;
            return currentCRC.SequenceEqual(crc);
        }
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="element"></param>
        public CRC32Element(EBMLDocument document, ElementStreamInfo element) : base(document, element) { }
    }
}
