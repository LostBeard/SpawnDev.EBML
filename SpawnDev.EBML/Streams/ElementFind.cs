using SpawnDev.EBML.Extensions;

namespace SpawnDev.EBML.Streams
{
    public partial class Element
    {
        /// <summary>
        /// Returns all child elements of this element
        /// </summary>
        public IEnumerable<Element> Children
        {
            get
            {
                Update();
                if (!Exists) return Enumerable.Empty<Element>();
                return Find("");
            }
        }
        public Element? FindFirst(string path) => Find<Element>(path).FirstOrDefault();
        public TElement? FindFirst<TElement>(string path) where TElement : Element => Find<TElement>(path).FirstOrDefault();
        string? DocTypEl = null;
        string? DocTypeElPID = null;
        public string? DocType
        {
            get
            {
                if (DocTypeElPID == Stream.PatchId) return DocTypEl;
                DocTypeElPID = Stream.PatchId;
                var element = Find<StringElement>("/EBML/DocType").FirstOrDefault();
                DocTypEl = element?.Value;
                return DocTypEl;
            }
        }
        public IEnumerable<Element> Find(string path) => Find<Element>(path);
        public IEnumerable<TElement> Find<TElement>(string path) where TElement : Element
        {
            return FindInfo(path).Select(elementSource =>
            {
                TElement element;
                if (typeof(TElement) == typeof(Element))
                {
                    // default type requested so we use the best default representation of the element available
                    element = elementSource.SchemaElement.Type switch
                    {
                        "master" => (TElement)(Element)new MasterElement(elementSource),
                        "string" => (TElement)(Element)new StringElement(elementSource),
                        "utf-8" => (TElement)(Element)new StringElement(elementSource),
                        "uinteger" => (TElement)(Element)new UintElement(elementSource),
                        "integer" => (TElement)(Element)new IntElement(elementSource),
                        "date" => (TElement)(Element)new DateElement(elementSource),
                        "float" => (TElement)(Element)new FloatElement(elementSource),
                        "binary" => (TElement)(Element)new BinaryElement(elementSource),
                        _ => (TElement)Activator.CreateInstance(typeof(TElement), elementSource)!,
                    };
                }
                else
                {
                    element = (TElement)Activator.CreateInstance(typeof(TElement), elementSource)!;
                }
                return element;
            });
        }
        public IEnumerable<StreamElementInfo> FindInfo(string path)
        {
            string startInstancePath = InstancePath;
            long? startPos = DataOffset;
            long documentOffset = DocumentOffset;
            if (path.StartsWith("/"))
            {
                startPos = documentOffset;
                startInstancePath = "/";
            }
            else if (SchemaElement != null && SchemaElement.Type != "master")
            {
                yield break;
            }
            long? origPos = null;
            string docType = EBMLParser.EBML;
            try
            {
                var parts = path.TrimStart('/').Split('/', StringSplitOptions.TrimEntries);
                if (!parts.Any()) yield break;
                if (string.IsNullOrEmpty(startInstancePath)) startInstancePath = "/";
                origPos = Stream.Position;
                if (startPos != null) Stream.Position = startPos.Value;
                var currentIndex = -1;
                var i = 0;
                var part = parts[i];
                var isLastPart = i == parts.Length - 1;
                var partIndex = 0;
                var partParts = part.Split(EBMLParser.IndexDelimiter);
                var parentPath = EBMLConverter.PathParent(path);
                var parentInstancePath = EBMLConverter.PathToInstancePath(parentPath);
                if (partParts.Length > 1)
                {
                    part = partParts[0];
                    partIndex = int.Parse(partParts[1]);
                }
                else if (isLastPart)
                {
                    partIndex = -1;
                }
                var partId = !part.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? 0 : EBMLConverter.ElementIdFromHexId(part);
                var partType = part.StartsWith("@") ? part.Substring(1) : "";
                var endPos = Stream.Length;
                var patchId = Stream.PatchId;
                var stack = new List<IteratedElementInfo>
                {
                    new IteratedElementInfo
                    {
                        Path = EBMLConverter.PathFromInstancePath(startInstancePath),
                        InstancePath = startInstancePath,
                        MaxSize = endPos - DocumentOffset,
                    }
                };
                IteratedElementInfo? targetParent = null;
                var parent = stack.Last();
                var currentParentInstancePath = parent.InstancePath;
                while (Stream.CanRead && Stream.Position < endPos)
                {
                    var position = Stream.Position;
                    ulong id;
                    ulong? size;
                    try
                    {
                        id = Stream.ReadEBMLElementIdRaw();
                        size = Stream.ReadEBMLElementSizeN();
                    }
                    catch
                    {
                        yield break;
                    }
                    var dataPosition = Stream.Position;
                    var streamBytesLeft = Stream.Length - dataPosition;
                    var headerSize = dataPosition - position;
                    var maxDataSize = size != null ? (long)size.Value : streamBytesLeft;
                    var schemaElement = EBMLParser.GetElement(id, docType);
                    if (schemaElement == null && docType == EBMLParser.EBML && path != "/EBML/DocType")
                    {
                        var dt = DocType;
                        if (!string.IsNullOrEmpty(dt) && dt != docType)
                        {
                            docType = dt;
                            schemaElement = EBMLParser.GetElement(id, docType);
                        }
                    }
                    if (schemaElement == null)
                    {
                        yield break;
                    }
                    if (docType == EBMLParser.EBML && schemaElement.Name == "DocType")
                    {
                        var dt = Stream.ReadEBMLStringASCII((int)size!.Value);
                        if (!string.IsNullOrEmpty(dt) && dt != docType)
                        {
                            docType = dt;
                        }
                        Stream.Position = dataPosition;
                    }
                    while (!EBMLParser.CheckParent(parent!.Path, schemaElement) && stack.Count > 0)
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
                        currentParentInstancePath = parent.InstancePath;
                    }
                    var name = schemaElement.Name;
                    var typeIndex = parent.Seen(id);
                    var iteratedInfo = new IteratedElementInfo
                    {
                        Path = $"{parent.Path.TrimEnd(EBMLParser.PathDelimiters)}{EBMLParser.PathDelimiter}{name}",
                        InstancePath = $"{parent.InstancePath.TrimEnd(EBMLParser.PathDelimiters)}{EBMLParser.PathDelimiter}{name},{typeIndex}",
                        MaxSize = maxDataSize,
                        DataOffset = dataPosition,
                    };
                    // The end of an Unknown - Sized Element is determined by whichever comes first:
                    // - Any EBML Element that is a valid Parent Element of the Unknown - Sized Element according to the EBML Schema, Global Elements excluded.
                    // - Any valid EBML Element according to the EBML Schema, Global Elements excluded, that is not a Descendant Element of the Unknown-Sized Element but shares a common direct parent, such as a Top - Level Element.
                    // - Any EBML Element that is a valid Root Element according to the EBML Schema, Global Elements excluded.
                    // - The end of the Parent Element with a known size has been reached.
                    // - The end of the EBML Document, either when reaching the end of the file or because a new EBML Header started.
                    var skipMaster = true;
                    if ((partId > 0 && partId == id) || (!string.IsNullOrWhiteSpace(partType) && partType == schemaElement.Type) || part == name || part == "")
                    {
                        currentIndex++;
                        if (partIndex == currentIndex || partIndex == -1)
                        {
                            if (isLastPart)
                            {
                                if ((targetParent != null && parent == targetParent) || stack.Count == parts.Length)
                                {
                                    var elementSource = new StreamElementInfo
                                    {
                                        Offset = position,
                                        Id = id,
                                        DocumentOffset = documentOffset,
                                        Index = parent.ChildCount - 1,
                                        Path = iteratedInfo.Path,
                                        InstancePath = iteratedInfo.InstancePath,
                                        Size = size,
                                        DataOffset = dataPosition,
                                        Stream = Stream,
                                        PatchId = patchId,
                                        MaxDataSize = maxDataSize,
                                        MaxTotalSize = maxDataSize + headerSize,
                                        SchemaElement = schemaElement,
                                        EBMLParser = EBMLParser,
                                        Exists = true,
                                    };
                                    // TODO - if this element is unknown size, defer the yield until size is determined
                                    // need test media with unknown size element. will create using browser (they create webm videos with elements of unknown size)
                                    // ... or, add a method to MasterElement GetSize() that will the master element's size
                                    yield return elementSource;
                                    Stream.Position = dataPosition;
                                }
                            }
                            else if (schemaElement.Type == "master")
                            {
                                skipMaster = false;
                                currentIndex = -1;
                                i++;
                                isLastPart = i == parts.Length - 1;
                                if (isLastPart)
                                {
                                    targetParent = iteratedInfo;
                                }
                                part = parts[i];
                                partIndex = 0;
                                partParts = part.Split(EBMLParser.IndexDelimiter);
                                if (partParts.Length > 1)
                                {
                                    part = partParts[0];
                                    partIndex = int.Parse(partParts[1]);
                                }
                                else if (isLastPart)
                                {
                                    // no index specified
                                    partIndex = -1;
                                }
                                partId = !part.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? 0 : EBMLConverter.ElementIdFromHexId(part);
                                partType = part.StartsWith("@") ? part.Substring(1) : "";
                            }
                        }
                    }
                    if (schemaElement.Type == "master")
                    {
                        if (skipMaster && size != null)
                        {
                            // we can only skip this if it does not match our filter
                            Stream.Position = dataPosition + (long)size.Value;
                        }
                        else
                        {
                            // has to be iterated
                            stack.Add(iteratedInfo);
                            parent = iteratedInfo;
                            currentParentInstancePath = parent.InstancePath;
                        }
                    }
                    else
                    {
                        // skip data (only master elements are allowed to be unknown size according the EBML spec
                        Stream.Position = dataPosition + (long)size!.Value;
                    }
                    while (parent.MaxSize + parent.DataOffset <= Stream.Position)
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
                        currentParentInstancePath = parent.InstancePath;
                    }
                }
            }
            finally
            {
                //Console.WriteLine("Exited iterator");
                if (origPos != null)
                {
                    try
                    {
                        Stream.Position = origPos.Value;
                    }
                    catch { }
                }
            }
        }
        //public IEnumerable<Element> Find(string path) => Find<Element>(path); 

        //public IEnumerable<TElement> Find<TElement>(string path) where TElement : Element
        //{
        //    string startInstancePath = InstancePath;
        //    long? startPos = DataOffset;
        //    long documentOffset = DocumentOffset;
        //    if (path.StartsWith("/"))
        //    {
        //        startPos = documentOffset;
        //        startInstancePath = "/";
        //    }
        //    long? origPos = null;
        //    string docType = EBMLParser.EBML;
        //    try
        //    {
        //        var parts = path.TrimStart('/').Split('/', StringSplitOptions.TrimEntries);
        //        if (!parts.Any()) yield break;
        //        if (string.IsNullOrEmpty(startInstancePath)) startInstancePath = "/";
        //        origPos = Stream.Position;
        //        if (startPos != null) Stream.Position = startPos.Value;
        //        var currentIndex = -1;
        //        var i = 0;
        //        var part = parts[i];
        //        var isLastPart = i == parts.Length - 1;
        //        var partIndex = 0;
        //        var partParts = part.Split(EBMLParser.IndexDelimiter);
        //        var parentPath = EBMLConverter.PathParent(path);
        //        var parentInstancePath = EBMLConverter.PathToInstancePath(parentPath);
        //        if (partParts.Length > 1)
        //        {
        //            part = partParts[0];
        //            partIndex = int.Parse(partParts[1]);
        //        }
        //        else if (isLastPart)
        //        {
        //            partIndex = -1;
        //        }
        //        var partId = !part.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? 0 : EBMLConverter.ElementIdFromHexId(part);
        //        var partType = part.StartsWith("@") ? part.Substring(1) : "";
        //        var endPos = Stream.Length;
        //        var patchId = Stream.PatchId;
        //        var stack = new List<IteratedElementInfo>
        //    {
        //        new IteratedElementInfo
        //        {
        //            Path = EBMLConverter.PathFromInstancePath(startInstancePath),
        //            InstancePath = startInstancePath,
        //            MaxSize = Stream.Length,
        //        }
        //    };
        //        IteratedElementInfo? targetParent = null;
        //        var parent = stack.Last();
        //        var currentParentInstancePath = parent.InstancePath;
        //        while (Stream.CanRead && Stream.Position < endPos)
        //        {
        //            var position = Stream.Position;
        //            ulong id;
        //            ulong? size;
        //            try
        //            {
        //                id = Stream.ReadEBMLElementIdRaw();
        //                size = Stream.ReadEBMLElementSizeN();
        //            }
        //            catch
        //            {
        //                yield break;
        //            }
        //            var dataPosition = Stream.Position;
        //            var streamBytesLeft = Stream.Length - dataPosition;
        //            var headerSize = dataPosition - position;
        //            var maxDataSize = size != null ? (long)size.Value : streamBytesLeft;
        //            var schemaElement = EBMLParser.GetElement(id, docType);
        //            if (schemaElement == null && docType == EBMLParser.EBML)
        //            {
        //                var dt = GetEBMLDocType();
        //                if (!string.IsNullOrEmpty(dt) && dt != docType)
        //                {
        //                    docType = dt;
        //                    schemaElement = EBMLParser.GetElement(id, docType);
        //                }
        //            }
        //            if (schemaElement == null)
        //            {
        //                yield break;
        //            }
        //            if (docType == EBMLParser.EBML && schemaElement.Name == "DocType")
        //            {
        //                docType = Stream.ReadEBMLStringASCII((int)size!.Value);
        //                Stream.Position = dataPosition;
        //            }
        //            while (!EBMLParser.CheckParent(parent!.Path, schemaElement) && stack.Count > 0)
        //            {
        //                if (parent == targetParent)
        //                {
        //                    yield break;
        //                }
        //                if (stack.Count == 1)
        //                {
        //                    yield break;
        //                }
        //                stack.Remove(parent);
        //                parent = stack.Last();
        //                currentParentInstancePath = parent.InstancePath;
        //            }
        //            var name = schemaElement.Name;
        //            var typeIndex = parent.Seen(id);
        //            var iteratedInfo = new IteratedElementInfo
        //            {
        //                Path = $"{parent.Path.TrimEnd(EBMLParser.PathDelimiters)}{EBMLParser.PathDelimiter}{name}",
        //                InstancePath = $"{parent.InstancePath.TrimEnd(EBMLParser.PathDelimiters)}{EBMLParser.PathDelimiter}{name},{typeIndex}",
        //                MaxSize = maxDataSize,
        //                DataOffset = dataPosition,
        //            };
        //            // The end of an Unknown - Sized Element is determined by whichever comes first:
        //            // - Any EBML Element that is a valid Parent Element of the Unknown - Sized Element according to the EBML Schema, Global Elements excluded.
        //            // - Any valid EBML Element according to the EBML Schema, Global Elements excluded, that is not a Descendant Element of the Unknown-Sized Element but shares a common direct parent, such as a Top - Level Element.
        //            // - Any EBML Element that is a valid Root Element according to the EBML Schema, Global Elements excluded.
        //            // - The end of the Parent Element with a known size has been reached.
        //            // - The end of the EBML Document, either when reaching the end of the file or because a new EBML Header started.
        //            var skipMaster = true;
        //            if ((partId > 0 && partId == id) || (!string.IsNullOrWhiteSpace(partType) && partType == schemaElement.Type) || part == name || part == "")
        //            {
        //                currentIndex++;
        //                if (partIndex == currentIndex || partIndex == -1)
        //                {
        //                    if (isLastPart)
        //                    {
        //                        if ((targetParent != null && parent == targetParent) || stack.Count == parts.Length)
        //                        {
        //                            var elementSource = new StreamElementInfo
        //                            {
        //                                Offset = position,
        //                                Id = id,
        //                                DocumentOffset = documentOffset,
        //                                Index = parent.ChildCount - 1,
        //                                Path = iteratedInfo.Path,
        //                                InstancePath = iteratedInfo.InstancePath,
        //                                Size = size,
        //                                DataOffset = dataPosition,
        //                                Stream = Stream,
        //                                PatchId = patchId,
        //                                MaxDataSize = maxDataSize,
        //                                MaxTotalSize = maxDataSize + headerSize,
        //                                SchemaElement = schemaElement,
        //                                EBMLParser = EBMLParser,
        //                                Exists = true,
        //                            };
        //                            TElement element;
        //                            if (typeof(TElement) == typeof(Element))
        //                            {
        //                                // default type requested so we use the best default representation of the element available
        //                                element = schemaElement.Type switch
        //                                {
        //                                    "master" => (TElement)(Element)new MasterElement(elementSource),
        //                                    "string" => (TElement)(Element)new StringElement(elementSource),
        //                                    "utf-8" => (TElement)(Element)new StringElement(elementSource),
        //                                    "uinteger" => (TElement)(Element)new UintElement(elementSource),
        //                                    "integer" => (TElement)(Element)new IntElement(elementSource),
        //                                    "date" => (TElement)(Element)new DateElement(elementSource),
        //                                    "float" => (TElement)(Element)new FloatElement(elementSource),
        //                                    "binary" => (TElement)(Element)new BinaryElement(elementSource),
        //                                    _ => (TElement)Activator.CreateInstance(typeof(TElement), elementSource)!,
        //                                };
        //                            }
        //                            else
        //                            {
        //                                element = (TElement)Activator.CreateInstance(typeof(TElement), elementSource)!;
        //                            }
        //                            // TODO - if this element is unknown size, defer the yield until size is determined
        //                            // need test media with unknown size element. will create using browser (they create webm videos with elements of unknown size)
        //                            // ... or, add a method to MasterElement GetSize() that will the master element's size
        //                            yield return element;
        //                            Stream.Position = dataPosition;
        //                        }
        //                    }
        //                    else if (schemaElement.Type == "master")
        //                    {
        //                        skipMaster = false;
        //                        currentIndex = -1;
        //                        i++;
        //                        isLastPart = i == parts.Length - 1;
        //                        if (isLastPart)
        //                        {
        //                            targetParent = iteratedInfo;
        //                        }
        //                        part = parts[i];
        //                        partIndex = 0;
        //                        partParts = part.Split(EBMLParser.IndexDelimiter);
        //                        if (partParts.Length > 1)
        //                        {
        //                            part = partParts[0];
        //                            partIndex = int.Parse(partParts[1]);
        //                        }
        //                        else if (isLastPart)
        //                        {
        //                            // no index specified
        //                            partIndex = -1;
        //                        }
        //                        partId = !part.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? 0 : EBMLConverter.ElementIdFromHexId(part);
        //                        partType = part.StartsWith("@") ? part.Substring(1) : "";
        //                    }
        //                }
        //            }
        //            if (schemaElement.Type == "master")
        //            {
        //                if (name != "EBML" && skipMaster && size != null)
        //                {
        //                    // we can only skip this if it does not match our filter
        //                    Stream.Position = dataPosition + (long)size.Value;
        //                }
        //                else
        //                {
        //                    // has to be iterated
        //                    stack.Add(iteratedInfo);
        //                    parent = iteratedInfo;
        //                    currentParentInstancePath = parent.InstancePath;
        //                }
        //            }
        //            else
        //            {
        //                // skip data (only master elements are allowed to be unknown size according the EBML spec
        //                Stream.Position = dataPosition + (long)size!.Value;
        //            }
        //            while (parent.MaxSize + parent.DataOffset <= Stream.Position)
        //            {
        //                if (parent == targetParent)
        //                {
        //                    yield break;
        //                }
        //                if (stack.Count == 1)
        //                {
        //                    yield break;
        //                }
        //                stack.Remove(parent);
        //                parent = stack.Last();
        //                currentParentInstancePath = parent.InstancePath;
        //            }
        //        }
        //    }
        //    finally
        //    {
        //        Console.WriteLine("Exited iterator");
        //        if (origPos != null)
        //        {
        //            try
        //            {
        //                Stream.Position = origPos.Value;
        //            }
        //            catch { }
        //        }
        //    }
        //}
    }
}
