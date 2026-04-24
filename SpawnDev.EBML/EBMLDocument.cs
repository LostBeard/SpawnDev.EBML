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
    public partial class EBMLDocument : MasterElement, IDisposable
    {
        Dictionary<string, MasterCacheItem> MasterCache = new Dictionary<string, MasterCacheItem>();
        private EBMLParser _Parser { get; set; }
        /// <summary>
        /// The underlying PatchStream
        /// </summary>
        //public PatchStream Stream { get; private set; }
        /// <summary>
        /// EBML Parser<br/>
        /// May be shared between Documents
        /// </summary>
        public override EBMLParser Parser => _Parser;
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
        public IEnumerable<BaseElement> Body => Children.Skip(1);
        /// <summary>
        /// 
        /// </summary>
        public event Action OnChanged = default!;
        /// <summary>
        /// Trigger the OnChanged event
        /// </summary>
        protected void Changed() => OnChanged?.Invoke();
        /// <summary>
        /// Log message event
        /// </summary>
        public event Action<string> OnLog = default!;
        /// <summary>
        /// Creates a new EBML document with the specified type<br/>
        /// Only the /EBML and /EBML/DocType are created
        /// </summary>
        public EBMLDocument(string docType = "ebml", EBMLParser? parser = null, string? filename = null) : base()
        {
            if (filename != null) Filename = filename;
            parser ??= new EBMLParser();
            //Info.Path = "";
            //Info.InstancePath = ",0";
            Info.Id = 0;
            Info.Depth = -1;
            Info.Offset = 0;
            Info.Exists = true;
            _Parser = parser;
            Document = this;
            // once Stream is set, Elements will stay up to date with stream changes, automatically
            // switching to the most recent (to the current patch level) stable stream version
            LoadEngines();
            Info.Stream = new PatchStream(new MemoryStream());
            // Keep a permanent reference to the LIVE PatchStream. FindInfo may
            // reassign Info.Stream to a stable snapshot for reads, but LatestStable
            // checks MUST always go against the live stream - otherwise a snapshot
            // taken mid-edit (before the new patch is marked RestorePoint) becomes
            // a permanent dead end and the document never sees later restore points.
            _LiveStream = Info.Stream;
            Stream.RestorePoint = true;
            Stream.OnChanged += _Stream_OnChanged;
            Stream.OnRestorePointsChanged += _Stream_OnRestorePointsChanged;
            if (!string.IsNullOrEmpty(docType))
            {
                CreateDocument(docType);
            }
            Console.WriteLine($"DocType: {DocType}");
        }
        /// <summary>
        /// The original, LIVE PatchStream backing this document. Unlike Info.Stream
        /// which may temporarily be swapped to a stable snapshot during FindInfo,
        /// _LiveStream always refers to the real stream that receives writes.
        /// </summary>
        private PatchStream _LiveStream = default!;
        /// <summary>
        /// Creates a new instance
        /// </summary>
        public EBMLDocument(Stream stream, EBMLParser? parser = null, string? filename = null, int typeIndex = 0, ulong? size = null) : base()
        {
            if (filename != null) Filename = filename;
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            parser ??= new EBMLParser();
            Info.Name = "";
            //Info.Path = "";
            //Info.InstancePath = $",{typeIndex}";
            Info.Id = 0;
            Info.TypeIndex = typeIndex;
            Info.Depth = -1;
            Info.Offset = stream.Position;
            Info.Exists = true;
            Info.Size = size;
            Info.DataSize = size != null ? (long)size.Value : stream.Length - stream.Position;
            Document = this;
            _Parser = parser;
            Info.Stream = stream is PatchStream patchStream ? patchStream : new PatchStream(stream);
            _LiveStream = Info.Stream;
            LoadEngines();
            Stream.RestorePoint = true;
            Stream.OnChanged += _Stream_OnChanged;
            Stream.OnRestorePointsChanged += _Stream_OnRestorePointsChanged;
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
                    _LiveStream.RestorePoint = true;
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
        void UpdateHeader(BaseElement element, ref long sizeDiff)
        {
            if (sizeDiff != 0)
            {
                var newSize = element.DataSize + sizeDiff;
                if (newSize < 0)
                {
                    throw new Exception("Invalid size");
                }
                // create new header . compare new header size to old and add diff to size
                var elementHeaderStream = EBMLStreamExtensions.CreateEBMLHeader(element.Id, (ulong)newSize);
                // replace current header with new header
                _LiveStream.Position = element.Offset;
                if (Verbose) Console.WriteLine($"-- Replacing header: {InstancePath} {element.DataSize} > {newSize}");
                _LiveStream.Insert(elementHeaderStream, element.HeaderSize);
                var headerSizeDiff = elementHeaderStream.Length - element.HeaderSize;
                sizeDiff += headerSizeDiff;
            }
        }
        /// <summary>
        /// Called by elements after they are modified<br/>
        /// </summary>
        /// <param name="changedElement"></param>
        /// <param name="newDataSize"></param>
        protected internal override void DataChanged(BaseElement changedElement, long newDataSize)
        {
            var changedElements = new List<BaseElement> { changedElement };
            // Fix headers starting with the changed element and going up the ancestor list
            if (changedElement != this)
            {
                if (newDataSize < 0)
                {
                    // the element is requesting deletion
                    // new data size is -header size
                    newDataSize = -changedElement.HeaderSize;
                    // delete the entire element
                    _LiveStream.Position = changedElement.Offset;
                    _LiveStream.Delete(changedElement.TotalSize);
                    // notify parent so it can adjust the size value in its header
                    // newDataSize = -1 to signify deletion
                }
                var sizeDiff = newDataSize - changedElement.DataSize;
                if (sizeDiff != 0)
                {
                    if (newDataSize > 0)
                    {
                        UpdateHeader(changedElement, ref sizeDiff);
                    }
                    var ancestors = changedElement.GetAncestors(true);
                    changedElements.AddRange(ancestors);
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
            if (InChange) return;
            InChange = true;
            try
            {
                // the stream can be marked as stable again now that headers have been verified
                if (Verbose) Console.WriteLine($"----------------------------------------------------------: {_LiveStream.RestorePoint}");
                ChangeEventBackLog.Add(changedElement);
                unhandledChanges++;
                _LiveStream.RestorePoint = true;
                FindInfo("/EBML/DocTpe").FirstOrDefault();
                RunDocumentEngines();
                _LiveStream.RestorePoint = true;
                foreach (var elem in changedElements)
                {
                    First(elem.InstancePath);
                }
            }
            finally
            {
                InChange = false;
            }
        }
        bool InChange = false;
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
        public bool CanUndo => _LiveStream?.CanRestorePointUndo ?? false;
        public bool CanRedo => _LiveStream?.CanRestorePointRedo ?? false;
        public bool Undo()
        {
            return _LiveStream.RestorePointUndo();
        }
        public bool Redo()
        {
            return _LiveStream.RestorePointRedo();
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
        public bool DocumentEnginesEnabled { get; private set; } = false;
        public bool DocumentEnginesEnabledGlobal { get; private set; } = false;
        /// <summary>
        /// Returns true if the document engines are running
        /// </summary>
        public bool DocumentEnginesRunning { get; private set; } = false;
        int unhandledChanges = 0;
        List<BaseElement> ChangeEventBackLog = new List<BaseElement>();
        public bool VerboseEngines { get; set; } = true;
        public bool VerboseStream { get; set; } = true;
        /// <summary>
        /// Run the document engines.<br/>
        /// If the document has not changed, the engines will not run unless forceRun == true
        /// </summary>
        /// <param name="forceRun">Run even if the document has not changed</param>
        /// <exception cref="Exception"></exception>
        public void RunDocumentEngines(bool forceRun = false)
        {
            if (!DocumentEnginesEnabledGlobal) return;
            if ((!DocumentEnginesEnabled && !forceRun) || DocumentEnginesRunning || !_LiveStream.RestorePoint)
            {
                if (!DocumentEnginesEnabled)
                {
                    if (VerboseEngines) Console.WriteLine($"Document engines disabled. Ignoring changes: {unhandledChanges}");
                }
                return;
            }
            DocumentEnginesRunning = true;
            if (VerboseEngines) Console.WriteLine($"Running document engines: {DocumentEngines.Values.Count}");
            var sw = Stopwatch.StartNew();
            var iterations = 0;
            try
            {
                if (forceRun) unhandledChanges++;
                while (unhandledChanges > 0)
                {
                    unhandledChanges = 0;
                    var changeEventBackLog = ChangeEventBackLog;
                    ChangeEventBackLog = new List<BaseElement>();
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
        public override string? DocType
        {
            get
            {
                if (base.Info.PatchId != _LiveStream.LatestStable.PatchId)
                {
                    ReadString("/EBML,0/DocType,0");
                }
                return _DocType;
            }
            set => WriteString("/EBML,0/DocType,0", value ?? "");
        }
        /// <summary>
        /// Cached doctype value
        /// </summary>
        string _DocType = "";
        bool SkipUnneededData = true;
        public override IEnumerable<ElementStreamInfo> FindInfo(string path)
        {
            var changed = false;
            var stream = Info.Stream;
            var patchId = stream.PatchId;
            // The latest stable snapshot must be computed on the LIVE stream,
            // NOT on Info.Stream. If we used Info.Stream and it had already been
            // swapped to a prior snapshot, `snapshot.LatestStable` would just be
            // the snapshot itself and the document could never observe later
            // restore points committed on the live stream.
            if (Info.PatchId != _LiveStream.LatestStable.PatchId)
            {
                stream = _LiveStream.LatestStable;
                patchId = stream.PatchId;
                changed = true;
                MasterCache.Clear();
                if (Verbose) Console.WriteLine($"*********** Changed to patch: {patchId}");
                Info.Stream = stream;
                Info.DataSize = stream.Length - DocumentOffset;
                Info.Size = (ulong)Info.DataSize;
            }
            // path must be a full path element 
            if (!path.StartsWith("/")) path = $"/{path}";
            if (CachedItems.TryGetValue(path, out var cacheMatch) && cacheMatch.PatchId == patchId)
            {
                // save processing not reiterating parents to get to the cached result
                yield return cacheMatch;
                yield break;
            }
            var sw = Stopwatch.StartNew();
            var resultCount = 0;
            try
            {
                if (Verbose) Console.WriteLine($"? {path}");
                var parser = Parser;
                long documentOffset = DocumentOffset;
                var startPos = documentOffset;
                var startInstancePath = "/";
                EBMLConverter.PathToParentInstancePathNameIndex(path, out var filterParentInstancePath, out var iname, out var index, true);
                if (string.IsNullOrEmpty(filterParentInstancePath)) filterParentInstancePath = "/";
                var target = $"{filterParentInstancePath}/{iname},{index}";
                var singleTarget = index < 0;
                var partType = iname.StartsWith("@") ? iname.Substring(1) : "";
                var partId = !iname.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? 0 : EBMLConverter.ElementIdFromHexId(iname);
                var currentIndex = -1;
                ElementStreamInfo? elementStreamInfo = null;
                // check cache for filter parent
                if (MasterCache.TryGetValue(filterParentInstancePath, out var cachedResults))
                {
                    if (cachedResults.Complete)
                    {
                        // the master element has been fully iterated
                        if (iname == "" && index == -1)
                        {
                            // no filter
                            foreach (var i in cachedResults.Children.Values)
                            {
                                yield return i;
                            }
                        }
                        else
                        {
                            // filter used
                            foreach (var i in cachedResults.Children.Values)
                            {
                                elementStreamInfo = i;
                                var partTypeMatches = (partType == elementStreamInfo.SchemaElement?.Type)
                                    || partType == "strings" && (elementStreamInfo.SchemaElement?.Type == "string" || elementStreamInfo.SchemaElement?.Type == "utf-8");
                                // inside target parent. check if element filters match
                                if (
                                    (partId > 0 && partId == elementStreamInfo.Id)
                                    || partTypeMatches
                                    || iname == elementStreamInfo.Name
                                    || iname == "")
                                {
                                    // match found for at 1 except index
                                    currentIndex++;
                                    if (index == currentIndex || index == -1)
                                    {
                                        yield return elementStreamInfo;
                                    }
                                }
                            }
                        }
                        resultCount = currentIndex;
                        yield break;
                    }
                }
                //if(CachedItems.TryGetValue(filterParentInstancePath, out var cachedParentMasterElement))
                //{
                //    startPos = cachedParentMasterElement
                //}
                // verify document is EBML
                var pos = stream.Position;
                stream.Position = documentOffset;
                if (!parser.IsEBML(stream))
                {
                    stream.Position = pos;
                    yield break;
                }
                stream.Position = startPos;
                var endPos = stream.Length;
                var stack = new List<IteratedElementInfo>
                {
                    new IteratedElementInfo
                    {
                        Path = EBMLConverter.PathFromInstancePath(startInstancePath),
                        InstancePath = startInstancePath,
                        MaxDataSize = endPos - documentOffset,
                        DataOffset = startPos,
                        Depth = startInstancePath.Split(EBMLParser.PathDelimiters, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length - 1,
                    }
                };
                var parent = stack.Last();
                if (!MasterCache.TryGetValue(parent.InstancePath, out var masterCacheItem))
                {
                    masterCacheItem = new MasterCacheItem
                    {
                        InstancePath = parent.InstancePath,
                    };
                    MasterCache.Add(parent.InstancePath, masterCacheItem);
                }
                parent.MasterCacheItem = masterCacheItem;
                IteratedElementInfo? targetParent = filterParentInstancePath == parent.InstancePath ? parent : null;
                while (stream.CanRead && stream.Position < endPos)
                {
                    var elementOffset = stream.Position;
                    ulong id;
                    ulong? size;
                    try
                    {
                        var header = ElementHeader.Read(stream);
                        size = header.Size;
                        id = header.Id;
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
                        var calculatedSize = parent.DataOffset - elementOffset;
                        parent.MaxDataSize = calculatedSize;
                        parent.MasterCacheItem.Complete = true;
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
                    var elementType = schemaElement?.Type;
                    var elementIndex = parent.ChildCount;
                    var elementTypeIndex = parent.Seen(id);
                    var elementName = schemaElement?.Name ?? EBMLConverter.ElementIdToHexId(id);
                    var elementPath = $"{parent.Path.TrimEnd(EBMLParser.PathDelimiters)}{EBMLParser.PathDelimiter}{elementName}";
                    var elementInstancePath = $"{parent.InstancePath.TrimEnd(EBMLParser.PathDelimiters)}{EBMLParser.PathDelimiter}{elementName},{elementTypeIndex}";
                    var elementDepth = elementPath.Split(EBMLParser.PathDelimiters, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length - 1;
                    var elementParentInstancePath = EBMLConverter.PathParent(elementInstancePath);
                    var elementIsMaster = elementType == "master";
                    var iteratedInfo = new IteratedElementInfo
                    {
                        Path = elementPath,
                        InstancePath = elementInstancePath,
                        MaxDataSize = maxDataSize,
                        UnknownSize = size == null,
                        DataOffset = dataPosition,
                        Depth = elementDepth,
                    };
                    if (!CachedItems.TryGetValue(elementInstancePath, out elementStreamInfo))
                    {
                        elementStreamInfo = new ElementStreamInfo();
                        CachedItems.Add(elementInstancePath, elementStreamInfo);
                    }
                    if (!parent.MasterCacheItem.Children.TryGetValue(elementInstancePath, out var child) || child != elementStreamInfo)
                    {
                        parent.MasterCacheItem.Children[elementInstancePath] = elementStreamInfo;
                    }
                    elementStreamInfo.Offset = elementOffset;
                    elementStreamInfo.Id = id;
                    elementStreamInfo.Name = elementName;
                    elementStreamInfo.SchemaElement = schemaElement;
                    elementStreamInfo.Index = parent.ChildCount - 1;
                    //elementStreamInfo.Path = elementPath;
                    //elementStreamInfo.InstancePath = elementInstancePath;
                    elementStreamInfo.Size = size;
                    elementStreamInfo.TypeIndex = elementTypeIndex;
                    elementStreamInfo.DataOffset = dataPosition;
                    elementStreamInfo.DataSize = maxDataSize;
                    elementStreamInfo.TotalSize = maxDataSize + headerSize;
                    elementStreamInfo.HeaderSize = headerSize;
                    //elementStreamInfo.ParentInstancePath = elementParentInstancePath;
                    elementStreamInfo.Depth = elementDepth;
                    //elementStreamInfo.DocumentOffset = documentOffset;
                    elementStreamInfo.Exists = true;
                    //elementStreamInfo.Updated(stream);
                    elementStreamInfo.Stream = stream;
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
                    // by default, all elements with a known size AND not an ancestor of the target get skipped
                    var skipElement = size != null && (!elementIsMaster || !EBMLConverter.IsAncestor(target, elementInstancePath));
                    if (filterParentInstancePath == iteratedInfo.InstancePath && targetParent == null)
                    {
                        // target parent found
                        skipElement = false;
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
                    if (skipElement && elementIsMaster && elementName == "EBML" && (!CachedItems.TryGetValue("/EBML,0/DocType,0", out var docTypeInfo) || docTypeInfo.PatchId != patchId))
                    {
                        // UpDocType not found in cache
                        // iterate EBML to try and get DocType
                        skipElement = false;
                    }
                    if (skipElement)
                    {
                        // we can only skip this if it does not match our filter (and size is know, checked earlier)
                        // or is not a master element
                        stream.Position = dataPosition + (long)size!.Value;
                        if (elementIsMaster)
                        {
                            if (Verbose) Console.WriteLine($"*** Skipping: {elementInstancePath}");
                        }
                    }
                    else
                    {
                        // has to be iterated
                        // stream.Position is left at elementOffset for next iteration
                        stack.Add(iteratedInfo);
                        parent = iteratedInfo;
                        if (!MasterCache.TryGetValue(parent.InstancePath, out masterCacheItem))
                        {
                            masterCacheItem = new MasterCacheItem
                            {
                                InstancePath = parent.InstancePath,
                            };
                            MasterCache.Add(parent.InstancePath, masterCacheItem);
                        }
                        parent.MasterCacheItem = masterCacheItem;
                        if (Verbose) Console.WriteLine($">> {parent.InstancePath}");
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
                        foreach (var endedIteratedMaster in allEnded)
                        {
                            // this instance has been fully iterated and can now be used instead of iterating file if possible
                            endedIteratedMaster.MasterCacheItem.Complete = true;
                            // size of ending master element is master element stream.Position - master.DataOffset
                            var calculatedSize = stream.Position - endedIteratedMaster.DataOffset;
                            Console.WriteLine($"calculatedSize: {calculatedSize} declared: {endedIteratedMaster.MaxDataSize}");
                            if (endedIteratedMaster.UnknownSize)
                            {
                                endedIteratedMaster.MaxDataSize = calculatedSize;
                                // can now yield queued results
                                // TODO
                            }
                            if (Verbose) Console.WriteLine($"<< {endedIteratedMaster.InstancePath}");
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
                if (Verbose) Console.WriteLine($"! {resultCount} {sw.Elapsed.TotalMilliseconds} {path} ");
                if (changed)
                {
                    Changed();
                }
            }
        }
        public override async IAsyncEnumerable<ElementStreamInfo> FindInfoAsync(string path, CancellationToken cancellationToken)
        {
            // TODO
            yield break;
        }
    }
}
