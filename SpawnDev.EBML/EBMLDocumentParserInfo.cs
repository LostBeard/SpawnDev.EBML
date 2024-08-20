namespace SpawnDev.EBML
{
    public class EBMLDocumentParserInfo
    {
        public Type ParserType { get; private set; }
        private Func<EBMLDocument, IEBMLDocumentEngine>? Factory { get; set; }
        public EBMLDocumentParserInfo(Type type, Func<EBMLDocument, IEBMLDocumentEngine>? factory = null)
        {
            ParserType = type;
            Factory = factory;
        }
        public IEBMLDocumentEngine Create(EBMLDocument doc)
        {
            return Factory != null ? Factory(doc) : (IEBMLDocumentEngine)Activator.CreateInstance(ParserType)!;
        }
    }
}
