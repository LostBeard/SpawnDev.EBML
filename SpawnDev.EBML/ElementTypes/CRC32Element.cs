using SpawnDev.EBML.Elements;
using SpawnDev.PatchStreams;

namespace SpawnDev.EBML.ElementTypes
{
    /// <summary>
    /// CRC-32 element<br/>
    /// </summary>
    [ElementName("CRC-32")]
    public class CRC32Element : BaseElement
    {
        protected override string DataToDataString()
        {
            var data = Data;
            var ret = data.Length == 0 ? "0x00000000" : "0x" + Convert.ToHexString(data);
            Console.WriteLine($"DataToDataString: {ret}");
            return ret;
        }
        /// <summary>
        /// Returns the element's data as  PatchStream stream.<br/>
        /// Editing the returned stream will not modify the element
        /// </summary>
        public byte[] Data
        {
            get
            {
                var hash = new byte[DataSize];
                Stream.Position = DataOffset;
                _ = Stream.Read(hash);
                return hash;
            }
            set
            {
                Console.WriteLine($">> Data._set: {InstancePath}");
                ReplaceData(value);
                Console.WriteLine($"<< Data._set: {InstancePath}");
            }
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
            var dataToCRC = allButCrc.Select(o => o.ElementToSlice()).ToList();
            using var stream = new PatchStream(dataToCRC);
            var hash = CRC.ComputeHash(stream);
            var ret = "0x" + Convert.ToHexString(hash);
            Console.WriteLine($"CalculateCRC: {ret}");
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
            var hashStrOld = "0x" + Convert.ToHexString(currentCRC);
            var hashStrNew = "0x" + Convert.ToHexString(crc);
            Console.WriteLine($"VerifyCRC: current {hashStrOld} calculated: {hashStrNew}");
            return currentCRC.SequenceEqual(crc);
        }
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="element"></param>
        public CRC32Element(EBMLDocument document, ElementStreamInfo element) : base(document, element) { }
    }
}
