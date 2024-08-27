using SpawnDev.EBML.Extensions;
using SpawnDev.PatchStreams;

namespace SpawnDev.EBML.Streams
{
    public partial class Element
    {
        public EBMLParser EBMLParser { get; protected set; }
        /// <summary>
        /// This elements index in its container
        /// </summary>
        public int Index { get; protected set; } = -1;
        /// <summary>
        /// The element's Id
        /// </summary>
        public ulong Id { get; protected set; }
        /// <summary>
        /// The element's hex id
        /// </summary>
        public string HexId => EBMLConverter.ElementIdToHexId(Id);
        /// <summary>
        /// The element's path
        /// </summary>
        public string Path { get; protected set; }
        /// <summary>
        /// The element's instance path
        /// </summary>
        public string InstancePath { get; protected set; }
        /// <summary>
        /// The element's EBML schema
        /// </summary>
        public SchemaElement SchemaElement { get; protected set; }
        /// <summary>
        /// The source stream
        /// </summary>
        public PatchStream Stream { get; protected set; }
        /// <summary>
        /// The position in the stream where the EBML document containing this element starts
        /// </summary>
        public long DocumentOffset { get; protected set; }
        /// <summary>
        /// The position in the stream of this element
        /// </summary>
        public long Offset { get; protected set; }
        /// <summary>
        /// The size of this element, if specified by header
        /// </summary>
        public ulong? Size { get; protected set; }
        /// <summary>
        /// The size of this element, if specified by header, else the size of data left in the stream
        /// </summary>
        public long MaxDataSize { get; protected set; }
        /// <summary>
        /// The total size of this element. Header size + data size.
        /// </summary>
        public long MaxTotalSize { get; protected set; }
        /// <summary>
        /// The position in the stream where this element's data starts
        /// </summary>
        public long DataOffset { get; protected set; }
        /// <summary>
        /// The patch id of the PatchStream when this element's metadata was last updated
        /// </summary>
        public string PatchId { get; protected set; }
        /// <summary>
        /// Returns true if this element's InstancePath is still found in the containing EBML document or if this element is an EBML document master element<br/>
        /// </summary>
        public bool Exists => (DocumentRoot && Stream != null) || Index > -1;
        /// <summary>
        /// Returns true if this element has an empty name and Offset == DocumentOffset
        /// </summary>
        public bool DocumentRoot => DocumentOffset == Offset && Depth == -1;
        /// <summary>
        /// The number of elements if this elements path - 1
        /// </summary>
        public int Depth { get; private set; } = -1;
        /// <summary>
        /// A Root Element is a mandatory, nonrepeating EBML Element that occurs at the top level of the path hierarchy within an EBML Body and contains all other EBML Elements of the EBML Body, excepting optional Void Elements.
        /// </summary>
        public bool Root => Depth == 0;
        /// <summary>
        /// A Top-Level Element is an EBML Element defined to only occur as a Child Element of the Root Element.
        /// </summary>
        public bool TopLevel => Depth == 1;
        /// <summary>
        /// Returns true if the the source stream has changed since Info was last updated
        /// </summary>
        public bool UpdateNeeded => PatchId != Stream.PatchId;

        //internal StreamElementInfo Info { get; set; }
        public Element(StreamElementInfo element)
        {
            UpdateFromInfo(element);
        }
        public Element(EBMLParser parser, PatchStream patchStream, string instancePath, string docType)
        {
            var path = EBMLConverter.PathFromInstancePath(instancePath);
            SchemaElement? schemaElement = null;
            var instanceName = path.Split(EBMLParser.PathDelimiters, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault();
            if (!string.IsNullOrEmpty(instanceName))
            {
                schemaElement = parser.GetElement(instanceName, docType);
            }
            Path = EBMLConverter.PathFromInstancePath(instancePath);
            InstancePath = EBMLConverter.PathToInstancePath(instancePath);
            Stream = patchStream;
            EBMLParser = parser;
            Id = schemaElement?.Id ?? 0;
            SchemaElement = schemaElement;
        }
        /// <summary>
        /// Removes the element from the stream
        /// </summary>
        /// <returns></returns>
        public bool Remove()
        {
            Update();
            if (!Exists || Size == null) return false;
            var pos = Stream.Position;
            Stream.Position = Offset;
            var sizeDiff = (long)Size!.Value;
            Stream.Delete(sizeDiff);
            Stream.Position = pos;
            ResizeAdd(-sizeDiff);
            return true;
        }
        /// <summary>
        /// Returns true if the stream has changed, and possibly this element's data
        /// </summary>
        /// <returns></returns>
        public bool Update()
        {
            if (PatchId == Stream.PatchId) return false;
            if (Depth == -1)
            {
                PatchId = Stream.PatchId;
                MaxDataSize = Stream.Length;
                return true;
            }
            var temp = FindInfo(InstancePath).FirstOrDefault();
            UpdateFromInfo(temp);
            return true;
        }
        public void UpdateFromInfo(StreamElementInfo? info)
        {
            if (info != null)
            {
                Path = info.Path;
                DocumentOffset = info.DocumentOffset;
                EBMLParser = info.EBMLParser;
                Id = info.Id;
                InstancePath = info.InstancePath;
                SchemaElement = info.SchemaElement;
                Stream = info.Stream;
                Index = info.Index;
                Offset = info.Offset;
                Size = info.Size;
                MaxDataSize = info.MaxDataSize;
                MaxTotalSize = info.MaxTotalSize;
                DataOffset = info.DataOffset;
                PatchId = info.PatchId;
                Depth = Path.Split(EBMLParser.PathDelimiters, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length - 1;
            }
            else
            {
                Index = -1;
            }
        }
        /// <summary>
        /// This method can be called be child nodes when they change size to notify all parents nodes so they can update the size info in their headers
        /// </summary>
        /// <param name="size"></param>
        public void ResizeAdd(long sizeDiff)
        {
            if (DocumentRoot)
            {
                return;
            }
            if (sizeDiff == 0)
            {
                return;
            }
            var newSize = (long)Size!.Value + sizeDiff;
            if (newSize < 0)
            {
                throw new Exception("Invalid size");
            }
            // create new header . compare new header size to old and add diff to size
            var elementHeaderStream = new MemoryStream();
            elementHeaderStream.WriteEBMLElementIdRaw(Id);
            elementHeaderStream.WriteEBMLElementSize((ulong)newSize);
            elementHeaderStream.Position = 0;
            // replace current header with new header
            var currentHeaderSize = DataOffset - Offset;
            var t = this;
            Stream.Position = Offset;
            Stream.Insert(elementHeaderStream, currentHeaderSize);
            var newHeaderSize = elementHeaderStream.Length;
            var headerSizeDiff = newHeaderSize - currentHeaderSize;
            sizeDiff += headerSizeDiff;
            if (sizeDiff == 0)
            {
                return;
            }
            // replace header
            // notify parent if 1
            var parentInstancePath = EBMLConverter.PathParent(InstancePath);
            if (!string.IsNullOrEmpty(parentInstancePath))
            {
                var parentEl = Find<MasterElement>(parentInstancePath).FirstOrDefault();
                if (parentEl != null)
                {
                    parentEl.ResizeAdd(sizeDiff);
                }
            }
        }
        public void ReplaceData(Stream replacementData)
        {
            var dataSize = (long)Size!.Value;
            var sizeDiff = replacementData.Length - dataSize;
            Stream.Position = DataOffset;
            Stream.Insert(replacementData, dataSize);
            ResizeAdd(sizeDiff);
        }
    }
}
