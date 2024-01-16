namespace SpawnDev.EBML
{
    /// <summary>
    /// A .Net EBML library for reading and writing EBML streams<br />
    /// https://github.com/ietf-wg-cellar/ebml-specification/blob/master/specification.markdown<br />
    /// </summary>
    public class EBMLDocumentReader : MasterElement
    {
        public List<EBMLSchema> Schemas { get; set; } = new List<EBMLSchema> { };

        public string DocType => EBML == null ? "?" : EBML.DocType ?? "?";

        public EBMLSchema GetSchema(string docType)
        {
            return Schemas.FirstOrDefault(o => o.DocType == docType) ?? DefaultEBMLSchema;
        }

        protected override void EBMLElementFound(EBMLElement ebml)
        {
            var schema = Schemas.FirstOrDefault(o => o.DocType == ebml.DocType);
            if (schema != null)
            {
                _ActiveSchema = schema;
            }
        }

        public EBMLElement? EBML => GetElement<EBMLElement>(ElementId.EBML);

        public EBMLDocumentReader(Stream? stream = null, List<EBMLSchema>? schemas = null) : base(ElementId.EBMLSource)
        {
            if (schemas != null)
            {
                Schemas = schemas;
            }
            if (stream != null)
            {
                if (typeof(SegmentSource).IsAssignableFrom(stream.GetType()))
                {
                    Stream = (SegmentSource)stream;
                }
                else
                {
                    Stream = new StreamSegment(stream);
                }
            }
        }
    }
}
