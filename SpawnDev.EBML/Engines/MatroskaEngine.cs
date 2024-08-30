using SpawnDev.EBML.Elements;
using SpawnDev.EBML.Extensions;

namespace SpawnDev.EBML.Engines
{
    /// <summary>
    /// Matroska EBML document engine
    /// </summary>
    public class MatroskaEngine : DocumentEngine
    {
        /// <summary>
        /// This constructor can take an existing EBMLDocument and parse it<br/>
        /// This is used by the generic Parser
        /// </summary>
        public MatroskaEngine(EBMLDocument document) : base(document)
        {

        }
        public List<string> DefaultSeekHeadTargets = new List<string> { "Info", "Tracks", "Chapters", "Cues", "Attachments" };
        public bool AutoPopulateSeekDefaultTargets { get; set; } = true;
        string[] DocTypes = new[] { "matroska", "webm" };
        public override void DocumentCheck(List<ElementBase> changedElements)
        {
            if (!DocTypes.Contains(Document.DocType)) return;
            //var issues = new List<DocumentIssue>();
            //var foundSeekTargetElementNames = new List<string>();
            //// verify seek data
            //var segmentElement = Document.GetContainer("Segment");
            //if (segmentElement == null) return;
            //var segmentStart = segmentElement.DataOffset;
            //var seekHeadElements = segmentElement.GetContainers("SeekHead");
            //foreach (var seekHeadElement in seekHeadElements)
            //{
            //    var seekElements = seekHeadElement.GetContainers("Seek");
            //    foreach (var seekElement in seekElements)
            //    {
            //        var seekIdBytes = seekElement.ReadBinary("SeekID");
            //        if (seekIdBytes != null)
            //        {
            //            var seekId = EBMLConverter.ToUInt(seekIdBytes);
            //            var targetElement = segmentElement.Data.FirstOrDefault(e => e.Id == seekId);
            //            if (targetElement != null)
            //            {
            //                foundSeekTargetElementNames.Add(targetElement.Name);
            //                var seekPosition = seekElement.ReadUint("SeekPosition");
            //                var targetPosition = targetElement.Offset;
            //                if (seekPosition == null || seekPosition.Value + segmentStart != targetPosition)
            //                {
            //                    var correctSeekPosition = targetPosition - segmentStart;
            //                    var diff = correctSeekPosition - targetPosition;
            //                    Log($"Seek position is off by {diff}. Fixing seek position.");
            //                    seekElement.UpdateUint("SeekPosition", correctSeekPosition);
            //                }
            //                else
            //                {
            //                    // correct. nothing to do
            //                    Log($"Seek position verified for {targetElement.Name}");
            //                }
            //            }
            //            else
            //            {
            //                // Target is missing!
            //                // remove seek?
            //                Log("Seek target is missing. Removing seek element.");
            //                seekElement.Remove();
            //            }
            //        }
            //    }
            //}
            //// if auto-populate verify Seek elements exist for "Info", "Tracks", "Chapters", "Cues", "Attachments"
            //if (AutoPopulateSeekDefaultTargets)
            //{
            //    if (seekHeadElements.Count() > 0)
            //    {
            //        var targetNamesToAdd = DefaultSeekHeadTargets.Except(foundSeekTargetElementNames).ToList();
            //        var seekHeadElement = seekHeadElements.First();
            //        foreach (var targetName in targetNamesToAdd)
            //        {
            //            var targetElement = segmentElement.GetContainer(targetName);
            //            if (targetElement != null)
            //            {
            //                var seekPosition = targetElement.Offset - segmentStart;
            //                var seekElement = seekHeadElement.CreateContainer("Seek");
            //                var seekIdBytes = EBMLConverter.ToUIntBytes(targetElement.Id);
            //                seekElement.AddBinary("SeekID", seekIdBytes);
            //                seekElement.AddUint("SeekPosition", seekPosition);
            //                seekHeadElement.AddElement(seekElement);
            //                Log($"Seek added for {targetElement.Name}");
            //            }
            //        }
            //    }
            //}
        }
    }
}

