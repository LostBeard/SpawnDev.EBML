using SpawnDev.EBML.Segments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpawnDev.EBML
{
    public class MatroskaDocumentEngine : EBMLDocumentEngine
    {
        public readonly static IEnumerable<string> DocTypes = new string[] { "matroska", "webm" };
        public EBMLDocument Document { get; private set; }
        /// <summary>
        /// This constructor can take an existing EBMLDocument and parse it<br/>
        /// This is used by the generic Parser
        /// </summary>
        public MatroskaDocumentEngine()
        {
            
        }
        public void Loaded(EBMLDocument document)
        {
            Document = document;
        }
    }
}
