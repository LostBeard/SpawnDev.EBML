using SpawnDev.EBML.Elements;
using SpawnDev.EBML.Extensions;
using SpawnDev.EBML.StreamElements;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseElement = SpawnDev.EBML.StreamElements.BaseElement;

namespace SpawnDev.EBML.Schemas
{
    public partial class EBMLParser
    {
        static bool Verbose = true;
        class Ele
        {

        }
        public override MasterElement FindInfo(Stream stream)
        {
            var changed = false;
            var sw = Stopwatch.StartNew();
            var resultCount = 0;
            var docType = "";
            try
            {
                var parser = this;
                long documentOffset = stream.Position;
                var startPos = documentOffset;
                var startInstancePath = "";
                var currentIndex = -1;
                //ElementStreamInfo? elementStreamInfo = null;
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
                        Path = "",
                        InstancePath = startInstancePath,
                        MaxDataSize = endPos - documentOffset,
                        DataOffset = startPos,
                        Depth = startInstancePath.Split(EBMLParser.PathDelimiters, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length - 1,
                    }
                };
                var parent = stack.Last();
                //parent.MasterCacheItem = masterCacheItem;
                //IteratedElementInfo? targetParent = filterParentInstancePath == parent.InstancePath ? parent : null;
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
                    var schemaElement = parser.GetElement(id, docType);
                    while (parent.UnknownSize && schemaElement != null && !parser.CheckParent(parent!.Path, schemaElement))
                    {
                        var calculatedSize = parent.DataOffset - elementOffset;
                        parent.MaxDataSize = calculatedSize;
                        //parent.MasterCacheItem.Complete = true;
                        //if (parent == targetParent)
                        //{
                        //    yield break;
                        //}
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
                    //elementStreamInfo.Offset = elementOffset;
                    //elementStreamInfo.Id = id;
                    //elementStreamInfo.Name = elementName;
                    //elementStreamInfo.SchemaElement = schemaElement;
                    //elementStreamInfo.Index = index;
                    ////elementStreamInfo.Path = elementPath;
                    ////elementStreamInfo.InstancePath = elementInstancePath;
                    //elementStreamInfo.Size = size;
                    //elementStreamInfo.TypeIndex = elementTypeIndex;
                    //elementStreamInfo.DataOffset = dataPosition;
                    //elementStreamInfo.DataSize = maxDataSize;
                    //elementStreamInfo.TotalSize = maxDataSize + headerSize;
                    //elementStreamInfo.HeaderSize = headerSize;
                    ////elementStreamInfo.ParentInstancePath = elementParentInstancePath;
                    //elementStreamInfo.Depth = elementDepth;
                    ////elementStreamInfo.DocumentOffset = documentOffset;
                    //elementStreamInfo.Exists = true;
                    ////elementStreamInfo.Updated(stream);
                    //elementStreamInfo.Stream = stream;
                    if (Verbose) Console.WriteLine($"{new string(' ', 2 * elementDepth + 1)} {elementIndex} {elementOffset} {maxDataSize} {elementName} {iteratedInfo.InstancePath}");
                    if (iteratedInfo.InstancePath == "/EBML,0/DocType,0")
                    {
                        var dt = stream.ReadEBMLStringASCII((int)size!.Value);
                        if (!string.IsNullOrEmpty(dt) && dt != docType)
                        {
                            docType = dt;
                            if (Verbose) Console.WriteLine($"DocType found: {docType}");
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
                    if (skipElement && elementIsMaster && elementName == "EBML")
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
                        //if (!MasterCache.TryGetValue(parent.InstancePath, out masterCacheItem))
                        //{
                        //    masterCacheItem = new MasterCacheItem
                        //    {
                        //        InstancePath = parent.InstancePath,
                        //    };
                        //    MasterCache.Add(parent.InstancePath, masterCacheItem);
                        //}
                        //parent.MasterCacheItem = masterCacheItem;
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
                            //endedIteratedMaster.MasterCacheItem.Complete = true;
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
    }
}
