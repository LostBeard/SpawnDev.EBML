using SpawnDev.EBML.Elements;
using SpawnDev.EBML.Engines;
using SpawnDev.EBML.Extensions;
using SpawnDev.EBML.Schemas;
using SpawnDev.PatchStreams;
using System.Diagnostics;

namespace SpawnDev.EBML
{
    /// <summary>
    /// An EBML document
    /// </summary>
    public partial class Document : MasterElement, IDisposable
    {
        private EBMLParser _Parser { get; set; }
        private PatchStream _Stream { get; set; }
        /// <summary>
        /// EBML Parser<br/>
        /// May be shared between Documents
        /// </summary>
        public override EBMLParser Parser => _Parser;
        /// <summary>
        /// The underlying PatchStream
        /// </summary>
        public override PatchStream Stream => _Stream;
        /// <summary>
        /// Get or set the Filename. Used for informational purposes only.
        /// </summary>
        public string Filename { get; set; } = "";
        /// <summary>
        /// Returns the EBML header or null if not found
        /// </summary>
        public MasterElement? Header => First<MasterElement>("EBML");
        /// <summary>
        /// Returns the EBML body or null if not found<br/>
        /// EBML body refers to the first element that is not the EBML element, usually right after the EBML element
        /// </summary>
        public IEnumerable<Element> Body => Children.Skip(1);
        /// <summary>
        /// Log message event
        /// </summary>
        public event Action<string> OnLog = default!;
        /// <summary>
        /// Creates a new EBML document with the specified type<br/>
        /// Only the /EBML and /EBML/DocType are created
        /// </summary>
        public Document(string docType = "ebml", EBMLParser? parser = null, string? filename = null) : base()
        {
            if (filename != null) Filename = filename;
            parser ??= new EBMLParser();
            Info.Path = "/";
            Info.InstancePath = "/";
            Info.Id = 0;
            Info.Depth = -1;
            Info.DocumentOffset = 0;
            Info.Exists = true;
            _Parser = parser;
            Document = this;
            // once Stream is set, Elements will stay up to date with stream changes, automatically
            // switching to the most recent (to the current patch level) stable stream version
            _Stream = new PatchStream(new MemoryStream());
            _Stream.OnChanged += _Stream_OnChanged;
            _Stream.OnRestorePointsChanged += _Stream_OnRestorePointsChanged;
            LoadEngines();
            Stream.RestorePoint = true;
            if (!string.IsNullOrEmpty(docType))
            {
                CreateDocument(docType);
            }
            Console.WriteLine($"DocType: {DocType}");
        }
        /// <summary>
        /// Creates a new instance
        /// </summary>
        public Document(Stream stream, EBMLParser? parser = null, string? filename = null) : base()
        {
            if (filename != null) Filename = filename;
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            parser ??= new EBMLParser();
            Info.Path = "/";
            Info.InstancePath = "/";
            Info.Id = 0;
            Info.Depth = -1;
            Info.DocumentOffset = stream.Position;
            Info.Exists = true;
            Document = this;
            _Parser = parser;
            _Stream = stream is PatchStream patchStream ? patchStream : new PatchStream(stream);
            _Stream.OnChanged += _Stream_OnChanged;
            _Stream.OnRestorePointsChanged += _Stream_OnRestorePointsChanged;
            LoadEngines();
            Stream.RestorePoint = true;
            FindInfo("/EBML/DocTpe").FirstOrDefault();
            Console.WriteLine($"DocType: {DocType}");
        }
        private void _Stream_OnChanged(PatchStream sender, IEnumerable<Patch> overwrittenPatches, IEnumerable<ByteRange> affectedRegions)
        {
            if (VerboseStream) Console.WriteLine($"Stream changed: {sender.PatchId} {CanUpdate}");
            if (CanUpdate)
            {
                FindInfo("/EBML/DocTpe").FirstOrDefault();
                RunDocumentEngines();
                if (CanUpdate)
                {
                    Stream.RestorePoint = true;
                    FindInfo("/EBML/DocTpe").FirstOrDefault();
                }
            }
        }
        private void _Stream_OnRestorePointsChanged(PatchStream sender)
        {
            if (VerboseStream) Console.WriteLine($"Restore point set: {sender.PatchId}");
            if (!sender.RestorePoint) return;
            unhandledChanges++;
            if (CanUpdate)
            {
                FindInfo("/EBML/DocTpe").FirstOrDefault();
                RunDocumentEngines();
                if (CanUpdate)
                {
                    FindInfo("/EBML/DocTpe").FirstOrDefault();
                }
            }
        }
        void UpdateHeader(Element element, ref long sizeDiff)
        {
            if (sizeDiff != 0)
            {
                var newSize = element.MaxDataSize + sizeDiff;
                if (newSize < 0)
                {
                    throw new Exception("Invalid size");
                }
                // create new header . compare new header size to old and add diff to size
                var elementHeaderStream = EBMLStreamExtensions.CreateEBMLHeader(element.Id, (ulong)newSize);
                // replace current header with new header
                Stream.Position = element.Offset;
                if (Verbose) Console.WriteLine($"-- Replacing header: {InstancePath} {element.MaxDataSize} > {newSize}");
                Stream.Insert(elementHeaderStream, element.HeaderSize);
                var headerSizeDiff = elementHeaderStream.Length - element.HeaderSize;
                sizeDiff += headerSizeDiff;
            }
        }
        /// <summary>
        /// Called by elements after they are modified<br/>
        /// </summary>
        /// <param name="changedElement"></param>
        /// <param name="newDataSize"></param>
        protected internal override void DataChanged(Element changedElement, long newDataSize)
        {
            // Fix headers starting with the changed element and going up the ancestor list
            if (changedElement != this)
            {
                if (newDataSize < 0)
                {
                    // the element is requesting deletion
                    // new data size is -header size
                    newDataSize = -changedElement.HeaderSize;
                    // delete the entire element
                    Stream.Position = changedElement.Offset;
                    Stream.Delete(changedElement.MaxTotalSize);
                    // notify parent so it can adjust the size value in its header
                    // newDataSize = -1 to signify deletion
                }
                var sizeDiff = newDataSize - changedElement.MaxDataSize;
                if (sizeDiff != 0)
                {
                    if (newDataSize > 0)
                    {
                        UpdateHeader(changedElement, ref sizeDiff);
                    }
                    var ancestors = changedElement.GetAncestors(true);
                    foreach (var ancestor in ancestors)
                    {
                        UpdateHeader(ancestor, ref sizeDiff);
                        if (sizeDiff == 0)
                        {
                            break;
                        }
                    }
                }
            }
            // the stream can be marked as stable again now that headers have been verified
            if (Verbose) Console.WriteLine($"----------------------------------------------------------: {Stream.RestorePoint}");
            ChangeEventBackLog.Add(changedElement);
            unhandledChanges++;
            Stream.RestorePoint = true;
            FindInfo("/EBML/DocTpe").FirstOrDefault();
            RunDocumentEngines();
            Stream.RestorePoint = true;
            if (CanUpdate) FindInfo("/EBML/DocTpe").FirstOrDefault();
        }
        protected Dictionary<string, ElementStreamInfo> CachedItems = new Dictionary<string, ElementStreamInfo>();
        /// <summary>
        /// List of loaded document engines
        /// </summary>
        public Dictionary<EngineInfo, DocumentEngine> DocumentEngines { get; private set; } = new Dictionary<EngineInfo, DocumentEngine>();

        public IEnumerable<TDocumentEngine> GetEngines<TDocumentEngine>() where TDocumentEngine : DocumentEngine
        {
            return DocumentEngines.Values.Where(o => o is TDocumentEngine).Cast<TDocumentEngine>().ToList();
        }
        public TDocumentEngine? GetEngine<TDocumentEngine>() where TDocumentEngine : DocumentEngine => GetEngines<TDocumentEngine>().FirstOrDefault();

        void LoadEngines()
        {
            var ret = new Dictionary<EngineInfo, DocumentEngine>();
            DocumentEngines = ret;
            foreach (var engineInfo in Parser.DocumentEngines)
            {
                var engine = engineInfo.Create(this);
                engine.OnLog += Engine_OnLog;
                ret.Add(engineInfo, engine);
            }
        }
        public bool Undo()
        {
            return Stream.RestorePointUndo();
        }
        public bool Redo()
        {
            return Stream.RestorePointRedo();
        }
        private void Engine_OnLog(string msg)
        {
            Log(msg);
        }
        /// <summary>
        /// Log some info
        /// </summary>
        /// <param name="msg"></param>
        protected void Log(string msg)
        {
            OnLog?.Invoke(msg);
#if DEBUG && false
            Console.WriteLine(msg);
#endif
        }
        /// <summary>
        /// Disable the document engines<br/>
        /// Useful when making multiple changes to the document or if multiple changes need to be made without the engines changing anything<br/>
        /// </summary>
        public void DisableDocumentEngines()
        {
            DocumentEnginesEnabled = false;
        }
        /// <summary>
        /// Enable the document engines<br/>
        /// </summary>
        /// <param name="runIfChanged"></param>
        public void EnableDocumentEngines(bool runIfChanged = true)
        {
            DocumentEnginesEnabled = true;
            if (runIfChanged && unhandledChanges > 0)
            {
                if (VerboseEngines) Console.WriteLine($"Document engines enabled. Catching up on: {unhandledChanges} unhandled changes");
                RunDocumentEngines();
            }
        }
        /// <summary>
        /// True if the document engines should run when the document changes
        /// </summary>
        public bool DocumentEnginesEnabled { get; private set; } = true;
        /// <summary>
        /// Returns true if the document engines are running
        /// </summary>
        public bool DocumentEnginesRunning { get; private set; } = false;
        int unhandledChanges = 0;
        List<Element> ChangeEventBackLog = new List<Element>();
        public bool VerboseEngines { get; set; } = false;
        public bool VerboseStream { get; set; } = false;
        /// <summary>
        /// Run the document engines.<br/>
        /// If the document has not changed, the engines will not run unless forceRun == true
        /// </summary>
        /// <param name="forceRun">Run even if the document has not changed</param>
        /// <exception cref="Exception"></exception>
        public void RunDocumentEngines(bool forceRun = false)
        {
            if ((!DocumentEnginesEnabled && !forceRun) || DocumentEnginesRunning || !Stream.RestorePoint)
            {
                if (!DocumentEnginesEnabled)
                {
                    if (VerboseEngines) Console.WriteLine($"Document engines disabled. Ignoring changes: {unhandledChanges}");
                }
                return;
            }
            DocumentEnginesRunning = true;
            if (VerboseEngines) Console.WriteLine("Running document engines");
            var sw = Stopwatch.StartNew();
            var iterations = 0;
            try
            {
                if (forceRun) unhandledChanges++;
                while (unhandledChanges > 0)
                {
                    unhandledChanges = 0;
                    var changeEventBackLog = ChangeEventBackLog;
                    ChangeEventBackLog = new List<Element>();
                    foreach (var documentEngine in DocumentEngines.Values)
                    {
                        if (!documentEngine.Enabled)
                        {
                            // skip disabled engines
                            // they miss all changes and therefore should check the document when re-enabled
                            continue;
                        }
                        var swe = Stopwatch.StartNew();
                        var unhandledChangesThis = unhandledChanges;
                        // this tells the document engine that the document has been modified nad gives it a chance to makes updates
                        // if it does make changes all document engines will get another chance to run until none of them makes any changes
                        documentEngine.DocumentCheck(changeEventBackLog);
                        var changeCount = unhandledChanges - unhandledChangesThis;
                        if (VerboseEngines) Console.WriteLine($"Engine {iterations} iteration {documentEngine.GetType().Name}: {changeCount} changes {sw.Elapsed.Milliseconds} run time");
                    }
                    iterations++;
                    if (iterations > 16)
                    {
                        Log("The document engines have iterated more than 16 times. Exiting run.");
                        break;
                    }
                }
            }
            finally
            {
                if (VerboseEngines) Console.WriteLine($"Engines took: {iterations} iterations {sw.Elapsed.Milliseconds} run time");
                DocumentEnginesRunning = false;
            }
        }
        /// <summary>
        /// This initializes a very minimal EBML document based on the current DocType
        /// </summary>
        public void CreateDocument(string docType)
        {
            AddMaster("EBML", 0);
            DocType = docType;
        }
        /// <summary>
        /// Release resources
        /// </summary>
        public void Dispose()
        {
            var engines = DocumentEngines;
            DocumentEngines.Clear();
            foreach (var engine in engines.Values)
            {
                if (engine is IDisposable disposable) disposable.Dispose();
            }
        }
        /// <summary>
        /// Returns the current DocType or null if it is does not exist
        /// </summary>
        public string? DocType
        {
            get => ReadString("/EBML,0/DocType,0");
            set => WriteString("/EBML/DocType", value ?? "");
        }
        string _DocType = "";
        bool SkipUnneededData = true;
        public override IEnumerable<ElementStreamInfo> FindInfo(string path) => FindInfo(path, false);
        protected internal IEnumerable<ElementStreamInfo> FindInfo(string path, bool useLiveStream)
        {
            if (Verbose) Console.WriteLine($"? {path}");
            var stream = Stream.LatestStable;
            var patchId = stream.PatchId;
            if (Info.PatchId != patchId)
            {
                // patch changed
                Info.PatchId = patchId;
                if (Verbose) Console.WriteLine($"Changed to patch: {patchId}");
            }
            if (!path.StartsWith("/")) path = $"/{path}";
            if (CachedItems.TryGetValue(path, out var cacheMatch) && cacheMatch.PatchId == patchId)
            {
                // save processing not reiterating parents to get to the cached result
                yield return cacheMatch;
                yield break;
            }
            var resultCount = 0;
            var parser = Parser;
            long documentOffset = DocumentOffset;
            var startPos = documentOffset;
            var startInstancePath = "/";
            EBMLConverter.PathToParentInstancePathNameIndex(path, out var filterParentInstancePath, out var iname, out var index, true);
            if (string.IsNullOrEmpty(filterParentInstancePath)) filterParentInstancePath = "/";
            if (CachedItems.TryGetValue(filterParentInstancePath, out var parentInfo))
            {
                // save processing not reiterating parents to get to parent path
                var nmt = true;
            }
            iname = iname.Trim('.');
            var partType = iname.StartsWith("@") ? iname.Substring(1) : "";
            var partId = !iname.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? 0 : EBMLConverter.ElementIdFromHexId(iname);
            // verify document is EBML
            var pos = stream.Position;
            stream.Position = documentOffset;
            if (!parser.IsEBML(stream))
            {
                stream.Position = pos;
                yield break;
            }
            try
            {
                stream.Position = startPos;
                var currentIndex = -1;
                var endPos = stream.Length;
                var stack = new List<IteratedElementInfo>
                {
                    new IteratedElementInfo
                    {
                        Path = EBMLConverter.PathFromInstancePath(startInstancePath),
                        InstancePath = startInstancePath,
                        MaxDataSize = endPos - documentOffset,
                    }
                };
                var parent = stack.Last();
                IteratedElementInfo? targetParent = filterParentInstancePath == parent.InstancePath ? parent : null;
                while (stream.CanRead && stream.Position < endPos)
                {
                    var elementOffset = stream.Position;
                    ulong id;
                    ulong? size;
                    try
                    {
                        id = stream.ReadEBMLElementIdRaw();
                        size = stream.ReadEBMLElementSizeN();
                    }
                    catch
                    {
                        yield break;
                    }
                    var dataPosition = stream.Position;
                    var streamBytesLeft = stream.Length - dataPosition;
                    var headerSize = dataPosition - elementOffset;
                    var maxDataSize = size != null ? (long)size.Value : streamBytesLeft;
                    var schemaElement = parser.GetElement(id, _DocType);
                    while (parent.UnknownSize && schemaElement != null && !parser.CheckParent(parent!.Path, schemaElement))
                    {
                        if (parent == targetParent)
                        {
                            yield break;
                        }
                        if (stack.Count == 1)
                        {
                            yield break;
                        }
                        stack.Remove(parent);
                        parent = stack.Last();
                    }
                    var skipIfMaster = SkipUnneededData;
                    var elementType = schemaElement?.Type;
                    var elementIndex = parent.ChildCount;
                    var elementTypeIndex = parent.Seen(id);
                    var elementName = schemaElement?.Name ?? EBMLConverter.ElementIdToHexId(id);
                    var elementPath = $"{parent.Path.TrimEnd(EBMLParser.PathDelimiters)}{EBMLParser.PathDelimiter}{elementName}";
                    var elementInstancePath = $"{parent.InstancePath.TrimEnd(EBMLParser.PathDelimiters)}{EBMLParser.PathDelimiter}{elementName},{elementTypeIndex}";
                    var elementDepth = elementPath.Split(EBMLParser.PathDelimiters, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length - 1;
                    var elementParentInstancePath = EBMLConverter.PathParent(elementInstancePath);
                    var iteratedInfo = new IteratedElementInfo
                    {
                        Path = elementPath,
                        InstancePath = elementInstancePath,
                        MaxDataSize = maxDataSize,
                        UnknownSize = size == null,
                        DataOffset = dataPosition,
                        Depth = elementDepth,
                    };
                    if (!CachedItems.TryGetValue(elementInstancePath, out var elementStreamInfo))
                    {
                        elementStreamInfo = new ElementStreamInfo();
                        CachedItems.Add(elementInstancePath, elementStreamInfo);
                    }
                    elementStreamInfo.Offset = elementOffset;
                    elementStreamInfo.Id = id;
                    elementStreamInfo.Name = elementName;
                    elementStreamInfo.SchemaElement = schemaElement;
                    elementStreamInfo.Index = parent.ChildCount - 1;
                    elementStreamInfo.Path = elementPath;
                    elementStreamInfo.InstancePath = elementInstancePath;
                    elementStreamInfo.Size = size;
                    elementStreamInfo.TypeIndex = elementTypeIndex;
                    elementStreamInfo.DataOffset = dataPosition;
                    elementStreamInfo.PatchId = patchId;
                    elementStreamInfo.MaxDataSize = maxDataSize;
                    elementStreamInfo.MaxTotalSize = maxDataSize + headerSize;
                    elementStreamInfo.HeaderSize = headerSize;
                    elementStreamInfo.ParentInstancePath = elementParentInstancePath;
                    elementStreamInfo.Depth = elementDepth;
                    elementStreamInfo.DocumentOffset = documentOffset;
                    elementStreamInfo.Exists = true;
                    if (Verbose) Console.WriteLine($"{new string(' ', 2 * elementDepth + 1)} {elementIndex} {elementOffset} {maxDataSize} {elementName} {iteratedInfo.InstancePath}");
                    if (iteratedInfo.InstancePath == "/EBML,0/DocType,0")
                    {
                        var dt = stream.ReadEBMLStringASCII((int)size!.Value);
                        if (!string.IsNullOrEmpty(dt) && dt != _DocType)
                        {
                            _DocType = dt;
                            if (Verbose) Console.WriteLine($"DocType found: {_DocType}");
                        }
                        stream.Position = dataPosition;
                    }
                    if (filterParentInstancePath == iteratedInfo.InstancePath && targetParent == null)
                    {
                        // target parent found
                        skipIfMaster = false;
                        targetParent = iteratedInfo;
                    }
                    else if (targetParent == parent || (targetParent == null && filterParentInstancePath == parent.InstancePath))
                    {
                        targetParent = parent;
                        var partTypeMatches = (partType == elementType) || partType == "strings" && (elementType == "string" || elementType == "utf-8");
                        // inside target parent. check if element filters match
                        if (
                            (partId > 0 && partId == id)
                            || partTypeMatches
                            || iname == elementName
                            || iname == "")
                        {
                            // match found for at 1 except index
                            currentIndex++;
                            if (index == currentIndex || index == -1)
                            {
                                // TODO - if this element is unknown size, defer all further yields (to maintain yield order) until size is determined
                                // need test media with unknown size element. will create using browser (they create webm videos with elements of unknown size)
                                // ... or, add a method to MasterElement GetSize() that will the master element's size
                                resultCount++;
                                if (Verbose) Console.WriteLine($"Match: ^^^^^^^^^^^^^^^");
                                yield return elementStreamInfo!;
                                stream.Position = dataPosition;
                                if (index == currentIndex)
                                {
                                    yield break;
                                }
                            }
                        }
                    }
                    // The end of an Unknown - Sized Element is determined by whichever comes first:
                    // - Any EBML Element that is a valid Parent Element of the Unknown - Sized Element according to the EBML Schema, Global Elements excluded.
                    // - Any valid EBML Element according to the EBML Schema, Global Elements excluded, that is not a Descendant Element of the Unknown-Sized Element but shares a common direct parent, such as a Top - Level Element.
                    // - Any EBML Element that is a valid Root Element according to the EBML Schema, Global Elements excluded.
                    // - The end of the Parent Element with a known size has been reached.
                    // - The end of the EBML Document, either when reaching the end of the file or because a new EBML Header started.
                    if (schemaElement?.Type == "master")
                    {
                        if (elementName == "EBML" && (!CachedItems.TryGetValue("/EBML,0/DocType,0", out var docTypeInfo) || docTypeInfo.PatchId != patchId))
                        {
                            // iterate EBML to try and get DocType
                            skipIfMaster = false;
                        }
                        if (skipIfMaster && size != null)
                        {
                            // we can only skip this if it does not match our filter
                            stream.Position = dataPosition + (long)size.Value;
                        }
                        else
                        {
                            // has to be iterated
                            stack.Add(iteratedInfo);
                            parent = iteratedInfo;
                            if (Verbose) Console.WriteLine($">> {parent.InstancePath}");
                        }
                    }
                    else
                    {
                        // skip data (only master elements are allowed to be unknown size according the EBML spec
                        stream.Position = dataPosition + (long)size!.Value;
                    }
                    // all elements after and including the first element that has ended have ended
                    var ended = stack.Where(o => o.MaxDataSize + o.DataOffset <= stream.Position).FirstOrDefault();
                    if (ended != null)
                    {
                        var endedIndex = stack.IndexOf(ended);
                        var allEnded = stack.Skip(endedIndex).ToList();
                        stack = stack.Take(endedIndex).ToList();
                        // remove starting with the last
                        // not needed ATM but will be if there are unreported results due to unknown sized elements
                        allEnded.Reverse();
                        foreach (var i in allEnded)
                        {
                            if (i.UnknownSize)
                            {
                                // can no yield queued results
                                // TODO
                            }
                            if (Verbose) Console.WriteLine($"<< {i.InstancePath}");
                        }
                        if (stack.Count == 0)
                        {
                            yield break;
                        }
                        parent = stack.Last();
                    }
                }
            }
            finally
            {
                if (Verbose) Console.WriteLine($"! {resultCount}");
            }
        }
    }
}
