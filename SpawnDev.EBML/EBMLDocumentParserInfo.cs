namespace SpawnDev.EBML
{
    public class EBMLDocumentParserInfo
    {
        public Type ParserType { get; private set; }
        public IEnumerable<string> DocTypes { get; private set; }
        private Func<EBMLDocument, EBMLDocument> Factory { get; set; }
        public EBMLDocumentParserInfo(IEnumerable<string> docTypes, Type type, Func<EBMLDocument, EBMLDocument> factory)
        {
            DocTypes = docTypes;
            ParserType = type;
            Factory = factory;
        }
        public EBMLDocument Create(EBMLDocument doc)
        {
            return Factory(doc);
        }
    }
}
