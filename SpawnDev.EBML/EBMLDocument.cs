using SpawnDev.EBML.Elements;
using SpawnDev.EBML.Segments;

namespace SpawnDev.EBML
{
    /// <summary>
    /// An EBML document
    /// </summary>
    public class EBMLDocument : MasterElement
    {
        /// <summary>
        /// Returns tru if this element is a document
        /// </summary>
        public override bool IsDocument { get; } = true;
        /// <summary>
        /// Element path
        /// </summary>
        public override string Path { get; } = "\\";
        /// <summary>
        /// Get or set the Filename. Used for informational purposes only.
        /// </summary>
        public string Filename { get; set; } = "";
        /// <summary>
        /// Returns the EBML header or null if not found
        /// </summary>
        public MasterElement? EBMLHeader => GetContainer("EBML");
        /// <summary>
        /// Returns \EBML\DocType or null
        /// </summary>
        public override string DocType => EBMLHeader?.ReadString("DocType") ?? EBMLSchemaSet.EBML;
        /// <summary>
        /// Returns the EBML body or null if not found
        /// </summary>
        public MasterElement? EBMLBody => Data.FirstOrDefault(o => o.Name != "EBML" && o is MasterElement) as MasterElement;
        public EBMLDocument(Stream stream, EBMLSchemaSet schemas, string? filename = null) : base(schemas, new StreamSegment(stream))
        {
            if (!string.IsNullOrEmpty(filename)) Filename = filename;
            LoadEngines();
        }
        public EBMLDocument(SegmentSource stream, EBMLSchemaSet schemas, string? filename = null) : base(schemas, stream)
        {
            if (!string.IsNullOrEmpty(filename)) Filename = filename;
            LoadEngines();
        }
        public EBMLDocument(string docType, EBMLSchemaSet schemas, string? filename = null) : base(schemas)
        {
            if (!string.IsNullOrEmpty(filename)) Filename = filename;
            CreateDocument(docType);
            OnChanged += EBMLDocument_OnChanged;
            ElementFound += EBMLDocument_ElementFound;
            ElementRemoved += EBMLDocument_ElementRemoved;
            LoadEngines();
        }
        public Dictionary<EBMLDocumentParserInfo, EBMLDocumentEngine> DocumentEngines { get; private set; }
        void LoadEngines()
        {
            var ret = new Dictionary<EBMLDocumentParserInfo, EBMLDocumentEngine>();
            DocumentEngines = ret;
            foreach (var engineInfo in SchemaSet.EBMLDocumentEngines)
            {
                var engine = engineInfo.Create(this);
                engine.Loaded(this);
                ret.Add(engineInfo, engine);
            }
        }
        private void EBMLDocument_ElementRemoved(BaseElement ret)
        {

        }
        private void EBMLDocument_ElementFound(BaseElement ret)
        {
            //if (ret.Path == @"\EBML")
            //{
            //    if (ret is MasterElement ebmlMaster && this is EBMLDocument thisDoc)
            //    {
            //        var newDocType = ebmlMaster.ReadString("DocType");
            //        if (!string.IsNullOrEmpty(newDocType))
            //        {
            //            _DocType = newDocType;
            //        }
            //    }
            //}
        }
        private void EBMLDocument_OnChanged(BaseElement ret)
        {
            //if (ret.Path == @"\EBML")
            //{
            //    if (ret is MasterElement ebmlMaster && this is EBMLDocument thisDoc)
            //    {
            //        var newDocType = ebmlMaster.ReadString("DocType");
            //        if (!string.IsNullOrEmpty(newDocType))
            //        {
            //            _DocType = newDocType;
            //        }
            //    }
            //}
        }
        /// <summary>
        /// This initializes a very minimal EBML document based on the current DocType
        /// </summary>
        void CreateDocument(string docType)
        {
            // - create EBML header based on DocType
            var ebmlHeader = AddContainer("EBML")!;
            var strEl = ebmlHeader.AddString("DocType", docType);
            if (SchemaSet.Schemas.TryGetValue(docType, out var schema))
            {
                var version = uint.TryParse(schema.Version, out var ver) ? ver : 1;
                ebmlHeader.AddUint("DocTypeVersion", version);
                // Adds any required root level containers
                AddMissingContainers();
            }
        }
    }
}
