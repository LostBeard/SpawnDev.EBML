using SpawnDev.EBML.Elements;

namespace SpawnDev.EBML.Engines
{
    /// <summary>
    /// Base class for EBML document engines<br/>
    /// After changes are made to an EBML Document, and the Document is marked as being in a stable state, the DocumentCheck methods of registered DocumentEngines<br/>
    /// are invoked to give them a chance to 
    /// </summary>
    public abstract class DocumentEngine
    {
        /// <summary>
        /// The EBML document this engine is attached to
        /// </summary>
        public Document Document { get; private set; }
        /// <summary>
        /// Required EBMLDocumentEngine constructor
        /// </summary>
        /// <param name="document"></param>
        public DocumentEngine(Document document)
        {
            Document = document;
        }
        /// <summary>
        /// Called by the Document when the document has changed<br/>
        /// This gives the engine a chance to update the document if needed
        /// </summary>
        public abstract void DocumentCheck(List<Element> changedElements);
        /// <summary>
        /// Fired when a log entry has been added
        /// </summary>
        public event Action<string> OnLog = default!;
        /// <summary>
        /// Get or set whether this engine is enabled
        /// </summary>
        public virtual bool Enabled { get; set; } = true;
        /// <summary>
        /// Add a log entry
        /// </summary>
        /// <param name="msg"></param>
        protected void Log(string msg)
        {
            OnLog?.Invoke($"{GetType().Name} {msg}");
        }
        /// <summary>
        /// A list of issues this engine sees with the current document
        /// </summary>
        public IEnumerable<DocumentIssue> Issues { get; protected set; } = new List<DocumentIssue>();
    }
}
