using SpawnDev.EBML;

namespace BlazorEBMLViewer.Services
{
    public class EBMLSchemaService
    {
        public EBMLSchemaSet SchemaSet { get; }
        public EBMLSchemaService()
        {
            SchemaSet = new EBMLSchemaSet();
            SchemaSet.LoadExecutingAssemblyEmbeddedSchemaXMLs();
        }
    }
}
