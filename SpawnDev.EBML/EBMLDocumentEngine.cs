using SpawnDev.EBML.Elements;

namespace SpawnDev.EBML
{
    class EBMLDocumentEngine : DocumentEngine
    {
        public EBMLDocumentEngine(Document document) : base(document) { }

        public override void DocumentCheck(List<IEnumerable<BaseElement>> changeLogs)
        {
            foreach(var changeLog in changeLogs)
            {
                foreach(var element in changeLog)
                {
                    if (element is MasterElement masterElement)
                    {
                        masterElement.UpdateCRC();
                    }
                }
            }
        }
    }
}
