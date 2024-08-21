using SpawnDev.EBML;

namespace BlazorEBMLViewer.Services
{
    public class EBMLSchemaService
    {
        public SchemaSet SchemaSet { get; }
        public EBMLSchemaService()
        {
            SchemaSet = new SchemaSet();
            SchemaSet.LoadDefaultSchemas();
            SchemaSet.RegisterDocumentEngine<MatroskaDocumentEngine>();
        }
    }
}
