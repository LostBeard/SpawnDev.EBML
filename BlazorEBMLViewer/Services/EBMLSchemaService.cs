using SpawnDev.EBML.Engines;
using SpawnDev.EBML.Schemas;

namespace BlazorEBMLViewer.Services
{
    public class EBMLSchemaService
    {
        public EBMLParser Parser { get; }
        public EBMLSchemaService()
        {
            Parser = new EBMLParser();
            Parser.LoadDefaultSchemas();
            Parser.RegisterDocumentEngine<MatroskaEngine>();
        }
    }
}
