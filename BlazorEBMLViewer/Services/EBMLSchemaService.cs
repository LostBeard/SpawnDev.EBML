using SpawnDev.EBML;

namespace BlazorEBMLViewer.Services
{
    public class EBMLSchemaService
    {
        public EBMLParser Parser { get; }
        public EBMLSchemaService()
        {
            Parser = new EBMLParser();
            Parser.LoadDefaultSchemas();
            Parser.RegisterDocumentEngine<MatroskaDocumentEngine>();
        }
    }
}
