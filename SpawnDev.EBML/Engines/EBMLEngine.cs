using SpawnDev.EBML.Elements;
using SpawnDev.EBML.ElementTypes;

namespace SpawnDev.EBML.Engines
{
    /// <summary>
    /// Handles EBML specific document verification and CRC-32 updating
    /// </summary>
    class EBMLEngine : DocumentEngine
    {
        public EBMLEngine(Document document) : base(document) { }

        public override void DocumentCheck(List<Element> changedElements)
        {
            if (changedElements.Count > 0)
            {
                var updatedCount = 0;
                var count = 0;
                foreach (var element in changedElements)
                {
                    if (element.SchemaElement?.Type == "master")
                    {
                        var elMaster = element.As<MasterElement>();
                        var crcEl = elMaster.First<CRC32Element>();
                        if (crcEl != null)
                        {
                            count++;
                            updatedCount += crcEl.UpdateCRC() ? 1 : 0;
                        }
                    }
                    var ancestors = element.GetAncestors(true);
                    foreach (var ancestor in ancestors)
                    {
                        var crcEl = ancestor.First<CRC32Element>();
                        if (crcEl != null)
                        {
                            count++;
                            updatedCount += crcEl.UpdateCRC() ? 1 : 0;
                        }
                    }
                }
                if (count > 0)
                {
                    Log($"Updated {updatedCount} and Verified {count - updatedCount} CRC-32 values");
                }
            }
        }
    }
}
