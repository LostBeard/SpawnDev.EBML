using SpawnDev.EBML;

namespace BlazorEBMLViewer.Services
{
    public class EBMLSchemaService
    {
        public EBMLParser SchemaSet { get; }
        public EBMLSchemaService()
        {
            SchemaSet = new EBMLParser();
            SchemaSet.LoadDefaultSchemas();
            SchemaSet.RegisterDocumentEngine<MatroskaDocumentEngine>();
        }
    }
}
