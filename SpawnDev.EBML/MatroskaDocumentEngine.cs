using SpawnDev.EBML.Elements;
using SpawnDev.EBML.Extensions;

namespace SpawnDev.EBML
{
    /// <summary>
    /// Matroska EBML document engine
    /// </summary>
    public class MatroskaDocumentEngine : EBMLDocumentEngine
    {
        /// <summary>
        /// DocTypes this engine supports
        /// </summary>
        public override string[] DocTypes { get; } = new string[] { "matroska", "webm" };
        /// <summary>
        /// This constructor can take an existing EBMLDocument and parse it<br/>
        /// This is used by the generic Parser
        /// </summary>
        public MatroskaDocumentEngine(EBMLDocument document) : base(document)
        {
            Console.WriteLine($"MatroskaDocumentEngine(): {Document.DocType}");
            Document.OnElementAdded += Document_OnElementAdded;
            Document.OnElementRemoved += Document_OnElementRemoved;
            Document.OnChanged += Document_OnChanged;
        }

        /// <summary>
        /// Fires when any document element changes<br/>
        /// When an element changes, this event will fire for the element that changes and for every one of its parent elements up the chain
        /// </summary>
        /// <param name="elements">The element that changed</param>
        private void Document_OnChanged(IEnumerable<BaseElement> elements)
        {
            var element = elements.First();
            //Console.WriteLine($"MKVE: Document_OnChanged: {elements.Count()} {element.Depth} {element.Name} {element.Path}");
            // Verify SeekPosition element values if any
            UpdateSeekHead();
        }
        public bool UpdateSeekHeadOnChange { get; set; } = true;
        public bool VerifySeekHeadOnChange { get; set; } = true;
        bool UpdatingSeekHead = false;
        void UpdateSeekHead()
        {
            if (!DocTypeSupported) return;
            if (!VerifySeekHeadOnChange && !UpdateSeekHeadOnChange)
            {
                return;
            }
            if (UpdatingSeekHead)
            {
                Console.WriteLine("Not UpdateSeekHead due to already in progress");
                //return;
            }
            Console.WriteLine(">> UpdateSeekHead");
            UpdatingSeekHead = true;
            try
            {
                var segment = Document.GetContainer("Segment");
                if (segment == null) return;
                var segmentStart = segment.Offset + segment.HeaderSize;
                var seeks = Document.GetContainers(@"\Segment\SeekHead\Seek");
                if (seeks.Count() == 0) return;
                Console.WriteLine("Checking SeekHead");
                foreach (var seek in seeks)
                {
                    var seekIdEl = seek.GetElement<BinaryElement>("SeekID");
                    if (seekIdEl == null) continue;
                    var seekPositionEl = seek.GetElement<UintElement>("SeekPosition");
                    if (seekPositionEl == null) continue;
                    var seekId = EBMLConverter.ReadEBMLUInt(seekIdEl.Data);
                    var seekPosition = seekPositionEl.Data;
                    var segmentPosition = seekPosition + (ulong)segmentStart;
                    var targetElement = segment.Data.FirstOrDefault(o => o.Id == seekId);
                    if (targetElement == null)
                    {
                        Console.WriteLine("Warning: Seek target not found");
                        continue;
                    }
                    var targetCurrentPosition = targetElement.Offset;
                    if (segmentPosition != targetCurrentPosition)
                    {
                        var diff = (long)segmentPosition - (long)targetCurrentPosition;
                        Console.WriteLine($"Warning: Seek expected position {segmentPosition}, real position is {targetCurrentPosition}, diff: {diff}");
                        // update seek
                        // break due to modifying the document?
                        if (UpdateSeekHeadOnChange)
                        {
                            seekPositionEl.Data = targetCurrentPosition;
                            Console.WriteLine($"Notice: Seek updated");
                            break;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Target position confirmed");
                    }
                }
            }
            finally
            {
                Console.WriteLine("<< UpdateSeekHead");
                UpdatingSeekHead = false;
            }
        }
        private void Document_OnElementAdded(MasterElement masterElement, BaseElement element)
        {
            //Console.WriteLine($"MKVE: Document_OnElementAdded: {element.Depth} {element.Name} {element.Path}");
        }
        private void Document_OnElementRemoved(MasterElement masterElement, BaseElement element)
        {
            //Console.WriteLine($"MKVE: Document_OnElementRemoved: {element.Depth} {masterElement.Path}\\{element.Name}");
        }
    }
}

