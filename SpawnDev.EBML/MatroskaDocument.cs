using SpawnDev.EBML.Segments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpawnDev.EBML
{
    public class MatroskaDocument : EBMLDocument
    {
        /// <summary>
        /// This constructor can take an existing EBMLDocument and parse it<br/>
        /// This is used by the generic Parser
        /// </summary>
        /// <param name="ebmlDocument"></param>
        public MatroskaDocument(EBMLDocument ebmlDocument) : base(ebmlDocument.SegmentSource, ebmlDocument.SchemaSet) { }
        public MatroskaDocument(Stream stream, EBMLSchemaSet schemas, string? filename = null) : base(stream, schemas, filename) { }
        public MatroskaDocument(SegmentSource stream, EBMLSchemaSet schemas, string? filename = null) : base(stream, schemas, filename) { }
        public MatroskaDocument(string docType, EBMLSchemaSet schemas, string? filename = null) : base(docType, schemas, filename) { }
    }
}
