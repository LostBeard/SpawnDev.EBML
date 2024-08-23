using SpawnDev.EBML.Elements;
using SpawnDev.EBML.Segments;
using System.Diagnostics;

namespace SpawnDev.EBML
{
    /// <summary>
    /// An EBML document
    /// </summary>
    public class Document : MasterElement, IDisposable
    {
        /// <summary>
        /// Element Instance name
        /// </summary>
        public override string InstanceName => Name;
        /// <summary>
        /// Returns tru if this element is a document
        /// </summary>
        public override bool IsDocument { get; } = true;
        /// <summary>
        /// Element path
        /// </summary>
        public override string Path { get; } = EBMLParser.PathDelimiter.ToString();
        /// <summary>
        /// Get or set the Filename. Used for informational purposes only.
        /// </summary>
        public string Filename { get; set; } = "";
        /// <summary>
        /// Returns the EBML header or null if not found
        /// </summary>
        public MasterElement? Header => GetContainer("EBML");
        /// <summary>
        /// Log message event
        /// </summary>
        public event Action<string> OnLog;
        /// <summary>
        /// Returns EBML DocType or null
        /// </summary>
        public override string DocType => Header?.ReadString("DocType") ?? EBMLParser.EBML;
        /// <summary>
        /// Returns the EBML body or null if not found<br/>
        /// EBML body refers to the first element that is not the EBML element, usually right after the EBML element
        /// </summary>
        public MasterElement? Body => Data.FirstOrDefault(o => o.Name != "EBML" && o is MasterElement) as MasterElement;
        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="schemas"></param>
        /// <param name="filename"></param>
        public Document(EBMLParser schemas, Stream stream, string? filename = null) : base(schemas, new StreamSegment(stream))
        {
            if (!string.IsNullOrEmpty(filename)) Filename = filename;
            OnChanged += Document_OnChanged;
            LoadEngines();
        }
        /// <summary>
        /// Creates a new instance
        /// </summary>
        public Document(EBMLParser schemas, SegmentSource segmentSource, string? filename = null) : base(schemas, segmentSource)
        {
            if (!string.IsNullOrEmpty(filename)) Filename = filename;
            OnChanged += Document_OnChanged;
            LoadEngines();
        }
        /// <summary>
        /// Creates a new instance
        /// </summary>
        public Document(EBMLParser schemas, string docType, string? filename = null) : base(schemas)
        {
            if (!string.IsNullOrEmpty(filename)) Filename = filename;
            CreateDocument(docType);
            OnChanged += Document_OnChanged;
            LoadEngines();
        }
        /// <summary>
        /// List of loaded document engines
        /// </summary>
        public Dictionary<DocumentEngineInfo, DocumentEngine> DocumentEngines { get; private set; } = new Dictionary<DocumentEngineInfo, DocumentEngine>();
        void LoadEngines()
        {
            var ret = new Dictionary<DocumentEngineInfo, DocumentEngine>();
            DocumentEngines = ret;
            foreach (var engineInfo in Parser.DocumentEngines)
            {
                var engine = engineInfo.Create(this);
                engine.OnLog += Engine_OnLog;
                ret.Add(engineInfo, engine);
            }
        }

        private void Engine_OnLog(string msg)
        {
            Log(msg);
        }

        protected void Log(string msg)
        {
            OnLog?.Invoke($"{this.GetType().Name} {msg}");
        }
        /// <summary>
        /// Disable the document engines<br/>
        /// Useful when making multiple changes to the document
        /// </summary>
        public void DisableDocumentEngines()
        {
            DocumentEnginesEnabled = false;
        }
        /// <summary>
        /// Enable the document engines<br/>
        /// </summary>
        /// <param name="runIfChanged"></param>
        public void EnabledDocumentEngines(bool runIfChanged = true)
        {
            DocumentEnginesEnabled = true;
            if (runIfChanged && unhandledChanges > 0) RunDocumentEngines();
        }
        /// <summary>
        /// True if the document engines should run when the document changes
        /// </summary>
        public bool DocumentEnginesEnabled { get; private set; } = true;
        /// <summary>
        /// Returns true if the document engines are running
        /// </summary>
        public bool DocumentEnginesRunning { get; private set; } = false;
        int unhandledChanges = 0;
        List<IEnumerable<BaseElement>> ChangeEventBackLog = new List<IEnumerable<BaseElement>>();
        private void Document_OnChanged(IEnumerable<BaseElement> elements)
        {
            unhandledChanges++;
            ChangeEventBackLog.Add(elements);
            if (DocumentEnginesEnabled)
            {
                RunDocumentEngines();
            }
        }
        /// <summary>
        /// Run the document engines.<br/>
        /// If the document has not changed, the engines will not run unless forceRun == true
        /// </summary>
        /// <param name="forceRun">Run even if the document has not changed</param>
        /// <exception cref="Exception"></exception>
        public void RunDocumentEngines(bool forceRun = false)
        {
            if (!DocumentEnginesRunning)
            {
                DocumentEnginesRunning = true;
                var sw = Stopwatch.StartNew();
                var iterations = 0;
                try
                {
                    if (forceRun) unhandledChanges++;
                    while (unhandledChanges > 0)
                    {
                        unhandledChanges = 0;
                        var changeEventBackLog = ChangeEventBackLog;
                        ChangeEventBackLog = new List<IEnumerable<BaseElement>>();
                        foreach (var documentEngine in DocumentEngines.Values)
                        {
                            var swe = Stopwatch.StartNew();
                            var unhandledChangesThis = unhandledChanges;
                            // this tells the document engine that the document has been modified nad gives it a chance to makes updates
                            // if it does make changes all document engines will get another chance to run until none of them makes any changes
                            documentEngine.DocumentCheck(changeEventBackLog);
                            var changeCount = unhandledChanges - unhandledChangesThis;
                            Console.WriteLine($"Engine {iterations} iteration {documentEngine.GetType().Name}: {changeCount} changes {sw.Elapsed.Milliseconds} run time");
                        }
                        iterations++;
                        if (iterations > 16)
                        {
                            Log("The document engines have iterated more than 16 times. Exiting run.");
                            break;
                        }
                    }
                }
                finally
                {
                    Console.WriteLine($"Engines took: {iterations} iterations {sw.Elapsed.Milliseconds} run time");
                    DocumentEnginesRunning = false;
                }
            }
        }
        /// <summary>
        /// This initializes a very minimal EBML document based on the current DocType
        /// </summary>
        void CreateDocument(string docType)
        {
            // - create EBML header based on DocType
            var ebmlHeader = AddContainer("EBML")!;
            var strEl = ebmlHeader.AddASCII("DocType", docType);
            if (Parser.Schemas.TryGetValue(docType, out var schema))
            {
                var version = uint.TryParse(schema.Version, out var ver) ? ver : 1;
                ebmlHeader.AddUint("DocTypeVersion", version);
                // Adds any required root level containers
                AddMissingContainers();
            }
        }
        /// <summary>
        /// Release resources
        /// </summary>
        public void Dispose()
        {
            var engines = DocumentEngines;
            DocumentEngines.Clear();
            foreach (var engine in engines.Values)
            {
                if (engine is IDisposable disposable) disposable.Dispose();
            }
        }
    }
}
