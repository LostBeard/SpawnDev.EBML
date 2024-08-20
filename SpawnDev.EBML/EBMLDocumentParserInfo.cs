namespace SpawnDev.EBML
{
    public class EBMLDocumentParserInfo
    {
        public Type ParserType { get; private set; }
        private Func<EBMLDocument, EBMLDocumentEngine>? Factory { get; set; }
        public EBMLDocumentParserInfo(Type type, Func<EBMLDocument, EBMLDocumentEngine>? factory = null)
        {
            ParserType = type;
            Factory = factory;
        }
        public EBMLDocumentEngine Create(EBMLDocument doc)
        {
            return Factory != null ? Factory(doc) : (EBMLDocumentEngine)Activator.CreateInstance(ParserType)!;
        }
    }
}
