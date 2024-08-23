using SpawnDev.EBML.Elements;

namespace SpawnDev.EBML
{
    /// <summary>
    /// Base class for EBML document engine
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
        public abstract void DocumentCheck(List<IEnumerable<BaseElement>> changeLogs);

        public event Action<string> OnLog;

        protected void Log(string msg)
        {
            OnLog?.Invoke($"{this.GetType().Name} {msg}");
        }
        /// <summary>
        /// A list of issues this engine is reporting for this document
        /// </summary>
        public IEnumerable<DocumentIssue> Issues { get; protected set; } = new List<DocumentIssue>();
    }
    public class DocumentIssue
    {
        public string Description { get; set; }
        public string Container { get; set; }
    }
}
