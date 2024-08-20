using SpawnDev.EBML.Elements;
using SpawnDev.EBML.Segments;

namespace SpawnDev.EBML
{
    /// <summary>
    /// An EBML document
    /// </summary>
    public class EBMLDocument : MasterElement
    {
        public override bool ElementHeaderRequired { get; } = false;
        public override string Path { get; } = "\\";
        internal string _DocType = EBMLSchemaSet.EBML;
        public override string DocType => _DocType;
        /// <summary>
        /// 
        /// </summary>
        //public EBMLSchemaSet SchemaSet { get; private set; }
        /// <summary>
        /// Get or set the Filename. Used for informational purposes only.
        /// </summary>
        public string Filename { get; set; } = "";
        /// <summary>
        /// Returns the EBML header or null if not found
        /// </summary>
        public MasterElement? EBMLHeader => GetContainer("EBML");
        /// <summary>
        /// Returns the EBML body or null if not found
        /// </summary>
        public MasterElement? EBMLBody => Data.Where(o => o.Name != "EBML" && o is MasterElement).Cast<MasterElement>().FirstOrDefault();
        public EBMLDocument(Stream stream, EBMLSchemaSet schemas) : base(schemas, new StreamSegment(stream))
        {
            
        }
        public EBMLDocument(SegmentSource stream, EBMLSchemaSet schemas) : base(schemas, stream)
        {

        }
        public EBMLDocument(string docType, EBMLSchemaSet schemas) : base(schemas)
        {
            _DocType = docType;
            CreateDocument();
        }
        public EBMLDocument(string filename, Stream stream, EBMLSchemaSet schemas) : base(schemas, new StreamSegment(stream))
        {
            Filename = filename;
        }
        public EBMLDocument(string filename, SegmentSource stream, EBMLSchemaSet schemas) : base(schemas, stream)
        {
            Filename = filename;
        }
        public EBMLDocument(string filename, string docType, EBMLSchemaSet schemas) : base(schemas)
        {
            Filename = filename;
            _DocType = docType;
            CreateDocument();
        }
        /// <summary>
        /// This initializes a very minimal EBML document based on the current DocType
        /// </summary>
        void CreateDocument()
        {
            var schema = SchemaSet.Schemas[_DocType];
            // - create EBML header based on DocType
            var ebmlHeader = AddContainer("EBML")!;
            var strEl = ebmlHeader.AddString("DocType", DocType);
            var version = uint.TryParse(schema.Version, out var ver) ? ver : 1;
            var uintEl = ebmlHeader.AddUint("DocTypeVersion", version);
            var headerDataSize = ebmlHeader.ElementHeader!.Size;
            var headerSize = ebmlHeader.ElementHeader!.HeaderSize;
            var nmt = true;
        }
        //public override void CopyTo(Stream stream)
        //{
        //    SegmentSource.Position = 0;
        //    SegmentSource.CopyTo(stream);
        //}
        //public override Task CopyToAsync(Stream stream)
        //{
        //    SegmentSource.Position = 0;
        //    return SegmentSource.CopyToAsync(stream);
        //}
    }
}
