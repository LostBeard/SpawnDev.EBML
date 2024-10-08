﻿using SpawnDev.EBML.ElementTypes;
using SpawnDev.EBML.Extensions;
using SpawnDev.EBML.Schemas;
using SpawnDev.PatchStreams;

namespace SpawnDev.EBML.Elements
{
    /// <summary>
    /// The EBML container type<br/>
    /// The only EBML type that can contain other EBML elements
    /// </summary>
    public partial class MasterElement : ElementBase
    {
        /// <summary>
        /// The element type name
        /// </summary>
        public const string TypeName= "master";
        /// <summary>
        /// Create a new instance<br/>
        /// This constructor is used internally
        /// </summary>
        /// <param name="element"></param>
        public MasterElement(EBMLDocument document, ElementStreamInfo element) : base(document, element) { }
        /// <summary>
        /// Constructor for derived classes
        /// </summary>
        protected MasterElement():base() { }
        /// <summary>
        /// Remove the first element matching the specified name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Remove(string name)
        {
            var ret = Find(name).FirstOrDefault();
            return ret?.Remove() ?? false;
        }
        /// <summary>
        /// Remove all elements with the specified name filter
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int RemoveAll(string name)
        {
            var ret = 0;
            var el = Find(name).FirstOrDefault();
            while (el != null && el.Remove())
            {
                ret++;
                el = Find(name).FirstOrDefault();
            }
            return ret;
        }
        /// <summary>
        /// Remove all children
        /// </summary>
        /// <returns></returns>
        public int RemoveAll()
        {
            var count = Children.Count();
            DeleteData();
            return count;
        }
        /// <summary>
        /// Remove an element at the specified index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="count">the number of elements to remove</param>
        /// <returns></returns>
        public bool RemoveAt(int index, int count = 1)
        {
            var toRemove = Children.Skip(index).Take(count).ToList();
            if (!toRemove.Any()) return false;
            var start = toRemove.First().Offset;
            var end = toRemove.Last().Offset + TotalSize;
            var length = end - start;
            Stream.Delete(start, length);
            return true;
        }
        /// <summary>
        /// Read the value from the first matching element
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string? ReadString(string name) => Find<StringElement>(name).FirstOrDefault()?.Data;
        /// <summary>
        /// Write an existing element, optionally adding if not found
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="allowCreate"></param>
        /// <returns></returns>
        public StringElement? WriteString(string name, string value, bool allowCreate = true)
        {
            var ret = Find<StringElement>(name).FirstOrDefault();
            if (ret != null) ret.Data = value;
            else if (allowCreate) ret = AddString(name, value);
            return ret;
        }
        /// <summary>
        /// Move [count] elements starting at index [start] to index [destination]<br/>
        /// If [destination] > the number of remaining elements after the selected range is removed, [destination] will be the number of elements left
        /// </summary>
        /// <param name="start"></param>
        /// <param name="destination">The desired index</param>
        /// <param name="length"></param>
        public int MoveElements(int start, int destination, int length = 1)
        {
            var children = Children.ToList();
            var currentCount = children.Count;
            if (start < 0) throw new ArgumentOutOfRangeException(nameof(start));
            if (length == 0) return 0;
            if (start == destination) return 0;
            if (destination < 0) destination = children.Count - length;
            if (length == -1)
            {
                length = currentCount - start;
            }
            if (destination > currentCount - length)
            {
                throw new ArgumentOutOfRangeException(nameof(destination));
            }
            var toMove = children.Skip(start).Take(length).ToList();
            if (!toMove.Any()) return 0;
            var left = children.Except(toMove).ToList();
            var startPos = toMove.First().Offset;
            var endPos = toMove.Last().Offset + TotalSize;
            var byteLength = endPos - startPos;
            var newPos = destination <= 0 ? DataOffset : (destination >= left.Count ? left.Last().EndPos : left[destination].Offset);
            Stream.Move(startPos, newPos, byteLength);
            return toMove.Count;
        }
        /// <summary>
        /// Pastes a opy of the specified element into this MasterElement at the specified index
        /// </summary>
        /// <param name="element"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public int PasteCopy(ElementBase element, int destination = -1) => PasteCopies(new[] { element }, destination);
        /// <summary>
        /// Pastes a opy of the specified elements into this MasterElement at the specified index
        /// </summary>
        /// <param name="elements">
        /// Elements to paste.<br/>
        /// NOTE: No validation is done on the pasted elements.
        /// </param>
        /// <param name="destination">The index position to paste the elements at. -1 will aster at the end.</param>
        /// <returns></returns>
        public int PasteCopies(IEnumerable<ElementBase> elements, int destination = -1)
        {
            // TODO - return the newly pasted elements (not the originals)
            var streams = elements.Select(o => o.ElementStreamSlice()).ToList();
            var totalSize = streams.Sum(o => o.Length);
            if (totalSize == 0) return 0;
            var children = Children.ToList();
            var newPos = destination <= 0 || children.Count == 0 ? DataOffset : (destination >= children.Count ? children.Last().EndPos : children[destination].Offset);
            var newDataSize = DataSize + totalSize;
            Stream.Position = newPos;
            Stream.Insert(streams, 0);
            DataChanged(this, newDataSize);
            return streams.Count;
        }
        #region Add element
        /// <summary>
        /// Add a binary element
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="atChildIndex"></param>
        /// <returns></returns>
        public BinaryElement AddBinary(string name, PatchStream? value = null, int atChildIndex = -1)
        {
            var ret = Add<BinaryElement>(name, atChildIndex);
            if (value != null)
            {
                ret.Data = value;
            }
            return ret;
        }
        /// <summary>
        /// Add a binary element
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="atChildIndex"></param>
        /// <returns></returns>
        public BinaryElement AddBinary(string name, Stream? value = null, int atChildIndex = -1)
        {
            var ret = Add<BinaryElement>(name, atChildIndex);
            if (value != null)
            {
                ret.Data = new PatchStream(value);
            }
            return ret;
        }
        /// <summary>
        /// Add a binary element
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="atChildIndex"></param>
        /// <returns></returns>
        public BinaryElement AddBinary(string name, byte[]? value = null, int atChildIndex = -1)
        {
            var ret = Add<BinaryElement>(name, atChildIndex);
            if (value != null)
            {
                ret.Data = new PatchStream(value);
            }
            return ret;
        }
        /// <summary>
        /// AddDate element
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="atChildIndex"></param>
        /// <returns></returns>
        public DateElement AddDate(string name, DateTime? value = null, int atChildIndex = -1)
        {
            var ret = Add<DateElement>(name, atChildIndex);
            if (value != null)
            {
                ret.Data = value.Value;
            }
            return ret;
        }
        /// <summary>
        /// Add a float element
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="atChildIndex"></param>
        /// <returns></returns>
        public FloatElement AddFloat(string name, double? value = null, int atChildIndex = -1)
        {
            var ret = Add<FloatElement>(name, atChildIndex);
            if (value != null)
            {
                ret.Data = value.Value;
            }
            return ret;
        }
        /// <summary>
        /// Add a uinteger element
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="atChildIndex"></param>
        /// <returns></returns>
        public UintElement AddUint(string name, ulong? value = null, int atChildIndex = -1)
        {
            var ret = Add<UintElement>(name, atChildIndex);
            if (value != null)
            {
                ret.Data = value.Value;
            }
            return ret;
        }
        /// <summary>
        /// Add a integer element
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="atChildIndex"></param>
        /// <returns></returns>
        public IntElement AddInt(string name, long? value = null, int atChildIndex = -1)
        {
            var ret = Add<IntElement>(name, atChildIndex);
            if (value != null)
            {
                ret.Data = value.Value;
            }
            return ret;
        }
        /// <summary>
        /// Add a string or utf-8 element
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="atChildIndex"></param>
        /// <returns></returns>
        public StringElement AddString(string name, string? value = null, int atChildIndex = -1)
        {
            var ret = Add<StringElement>(name, atChildIndex);
            if (value != null)
            {
                ret.Data = value;
            }
            return ret;
        }
        /// <summary>
        /// Add a master element
        /// </summary>
        /// <param name="name"></param>
        /// <param name="atChildIndex"></param>
        /// <returns></returns>
        public MasterElement AddMaster(string name, int atChildIndex = -1) => Add<MasterElement>(name, atChildIndex);
        /// <summary>
        /// Add an element. The specified return type will be used to determine the element name.
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="destination"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public TElement Add<TElement>(int destination = -1) where TElement : ElementBase
        {
            var elementNameFromType = Parser.GetElementNameFromType<TElement>(DocType);
            if (elementNameFromType == null) throw new Exception($"Could not determine element name from type: {typeof(TElement).Name}");
            return Add<TElement>(elementNameFromType, destination);
        }
        /// <summary>
        /// Add an element to this element
        /// </summary>
        /// <param name="name"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public ElementBase Add(string name, int destination = -1) => Add<ElementBase>(name, destination);
        /// <summary>
        /// Returns a list of EBMLSchemaElement that can be added to this MasterElement
        /// </summary>
        /// <param name="includeMaxCountItems"></param>
        /// <returns></returns>
        public IEnumerable<SchemaElement> GetAddableElementSchemas(bool includeMaxCountItems = false)
        {
            var ret = new List<SchemaElement>();
            var allSchemaElements = Parser.GetElements(DocType);
            foreach (var addable in allSchemaElements.Values)
            {
                var parentAllowed = Parser.CheckParent(this, addable);
                if (!parentAllowed) continue;
                if (!includeMaxCountItems)
                {
                    var atMaxCount = false;
                    if (addable.MaxOccurs > 0 || addable.MinOccurs > 0)
                    {
                        var count = Children.Count(o => o.Id == addable.Id);
                        atMaxCount = addable.MaxOccurs > 0 && count >= addable.MaxOccurs;
                    }
                    if (atMaxCount) continue;
                }
                ret.Add(addable);
            }
            return ret;
        }
        /// <summary>
        /// Add an element using element schema
        /// </summary>
        /// <param name="schemaElement"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public ElementBase AddElement(SchemaElement schemaElement, int destination = -1) => Add<ElementBase>(schemaElement.Name, destination);
        /// <summary>
        /// Add an element
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="name"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public TElement Add<TElement>(string name, int destination = -1) where TElement : ElementBase
        {
            ThrowIfCannotEdit();
            // here, index in name is ignored
            if (name.Contains('/'))
            {
                if (!name.StartsWith('/'))
                {
                    name = $"{InstancePath.TrimEnd(EBMLParser.PathDelimiters)}/{name}";
                }
                //var parentInstancePath = EBMLConverter.PathParent(name);
                //name = EBMLConverter.PathName(name);
                //EBMLConverter.NameToNameIndex();
                EBMLConverter.PathToParentInstancePathNameIndex(name, out var parentInstancePath, out var namei, out var index, true);
                if (index > -1 && destination == -1) destination = index;
                if (parentInstancePath != InstancePath)
                {
                    var parent = parentInstancePath == "/" ? Document : First<MasterElement>(parentInstancePath);
                    if (parent == null)
                    {
                        throw new Exception("Parent not found");
                    }
                    return parent.Add<TElement>(namei, destination);
                }
                name = namei;
            }
            EBMLConverter.NameToNameIndex(name, out var namen, out var indexn);
            if (indexn > -1 && destination == -1) destination = indexn;
            name = namen;
            var children = Children.ToList();
            if (destination < 0) destination = children.Count;
            else destination = Math.Clamp(destination, 0, children.Count);
            if (DocumentRoot && destination == 0 && name != "EBML")
            {
                if (children.FirstOrDefault()?.Name != "EBML")
                {
                    Add<EBMLHeader>();
                    children = Children.ToList();
                }
                destination = 1;
            }
            if (name == "CRC-32")
            {
                destination = 0;
                if (children.Any(o => o.Name == "CRC-32"))
                {
                    // CRC-32 is prevented from being added more than once to a master element
                    throw new Exception($"{name} can only occur 1 time in a container");
                }
            }
            var childrenBefore = children.Take(destination).ToList();
            var newPos = childrenBefore.LastOrDefault()?.EndPos ?? DataOffset;
            if (!Exists) throw new Exception("This element no longer exists");
            var schemaElement = Parser.GetElement(name, SchemaElement?.DocType ?? EBMLParser.EBML);
            if (schemaElement == null)
            {
                var docType = Document.DocType;
                if (!string.IsNullOrEmpty(docType))
                {
                    schemaElement = Parser.GetElement(name, docType);
                }
            }
            var typeIndex = childrenBefore.Count(o => o.Name == name);
            Document.DisableDocumentEngines();
            if (schemaElement == null) throw new Exception("Element type not found. Verify /EBML/DocType element is set correctly");
            var newElementStream = EBMLStreamExtensions.CreateEBMLHeader(schemaElement.Id, 0);
            if (Verbose) Console.WriteLine($"AddingElement: {name},{typeIndex} {destination} {newPos} {newElementStream.Length} {InstancePath}");
            var newDataSize = DataSize + newElementStream.Length;
            Stream.Position = newPos;
            Stream.Insert(newElementStream);
            // now update all parents headers to reflect the added data
            DataChanged(this, newDataSize);
            if (Verbose) Console.WriteLine($"AddedElement: {name},{typeIndex} {destination} {InstancePath}");
            var ret = Find<TElement>($",{destination}").First();
            if (Verbose) Console.WriteLine($"Added verified: {name},{typeIndex} {destination} {InstancePath}");
            ret.AfterAdded();
            Document.EnableDocumentEngines();
            return ret;
        }
        /// <summary>
        /// Get this element's CRC-32 element, optionally creating it if it does not exist<br/>
        /// The CRC-32 element will automatically be kept up to date by the EBMLEngine
        /// </summary>
        /// <param name="allowCreate"></param>
        /// <returns></returns>
        public CRC32Element? GetCRC32(bool allowCreate = false)
        {
            var ret = First<CRC32Element>();
            return allowCreate && ret == null ? AddCRC32() : ret;
        }
        /// <summary>
        /// Adds a CRC-32 element if it does not already exist
        /// </summary>
        /// <returns></returns>
        public CRC32Element AddCRC32()
        {
            var crc32 = First<CRC32Element>();
            if (crc32 != null) return crc32;
            return Add<CRC32Element>();
        }
        #endregion
        #region Find
        /// <summary>
        /// Find
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public IEnumerable<ElementBase> this[string path] => Find(Path);
        /// <summary>
        /// Returns all child elements of this element
        /// </summary>
        public IEnumerable<ElementBase> Children => Find();
        /// <summary>
        /// Returns all child elements of this element
        /// </summary>
        public IAsyncEnumerable<ElementBase> ChildrenAsync => FindAsync<ElementBase>("", CancellationToken.None);
        public IEnumerable<MasterElement> Masters => Find<MasterElement>("@master");
        /// <summary>
        /// Returns all the string and utf-8 elements in this element
        /// @strings - signifies `utf-8` and `string` strings. @string = `string` strings, and @utf-8 = `utf-8` strings
        /// </summary>
        public IEnumerable<StringElement> Strings => Find<StringElement>("@strings");
        /// <summary>
        /// Returns all the uinteger elements in this element
        /// </summary>
        public IEnumerable<UintElement> Uints => Find<UintElement>("@uinteger");
        /// <summary>
        /// Returns all the integer elements in this element
        /// </summary>
        public IEnumerable<IntElement> Ints => Find<IntElement>("@integer");
        /// <summary>
        /// Returns all the binary elements in this element
        /// </summary>
        public IEnumerable<BinaryElement> Binaries => Find<BinaryElement>("@binary");
        /// <summary>
        /// Returns all the float elements in this element
        /// </summary>
        public IEnumerable<FloatElement> Floats => Find<FloatElement>("@float");
        /// <summary>
        /// Returns all the date elements in this element
        /// </summary>
        public IEnumerable<DateElement> Dates => Find<DateElement>("@date");
        /// <summary>
        /// Returns all the master elements that match the path filter
        /// </summary>
        public MasterElement? FindMaster(string path) => First<MasterElement>(path);
        /// <summary>
        /// Returns all the master elements that match the path filter
        /// </summary>
        public IEnumerable<MasterElement> FindMasters(string path) => Find<MasterElement>(path);
        /// <summary>
        /// Returns the first element that matches the path filter
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public ElementBase? First(string path) => Find<ElementBase>(path).FirstOrDefault();
        /// <summary>
        /// Returns the first element that matches the path filter
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public TElement? First<TElement>(string path) where TElement : ElementBase => Find<TElement>(path).FirstOrDefault();
        /// <summary>
        /// Search this element's children for elements with the name determined by the TElement type
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public TElement? First<TElement>() where TElement : ElementBase
        {
            var elementNameFromType = Parser.GetElementNameFromType<TElement>(DocType);
            if (elementNameFromType == null) throw new Exception($"Could not determine element name from type: {typeof(TElement).Name}");
            return First<TElement>(elementNameFromType);
        }
        public IEnumerable<ElementBase> Find(string path = "") => Find<ElementBase>(path);
        public IEnumerable<TElement> Find<TElement>() where TElement : ElementBase
        {
            var elementNameFromType = Parser.GetElementNameFromType<TElement>(DocType);
            if (elementNameFromType == null) throw new Exception($"Could not determine element name from type: {typeof(TElement).Name}");
            return Find<TElement>(elementNameFromType);
        }
        public IEnumerable<TElement> Find<TElement>(string path) where TElement : ElementBase
        {
            return FindInfo(path).Select(elementSource =>
            {
                TElement element;
                if (typeof(TElement) == typeof(ElementBase))
                {
                    // default type requested so we use the best default representation of the element available
                    var type = Parser.GetElementType(elementSource.SchemaElement!.Type, elementSource.SchemaElement!.Name, DocType);
                    element = (TElement)Activator.CreateInstance(type, Document, elementSource)!;
                }
                else
                {
                    element = (TElement)Activator.CreateInstance(typeof(TElement), Document, elementSource)!;
                }
                return element;
            });
        }
        public virtual IEnumerable<ElementStreamInfo> FindInfo(string path)
        {
            if (!path.StartsWith("/"))
            {
                path = $"{InstancePath.TrimEnd('/')}/{path}";
            }
            return Document!.FindInfo(path);
        }
        // Async Find
        public IAsyncEnumerable<ElementBase> FindAsync(CancellationToken cancellationToken) => FindAsync<ElementBase>("", cancellationToken);
        public IAsyncEnumerable<ElementBase> FindAsync(string path, CancellationToken cancellationToken) => FindAsync<ElementBase>(path, cancellationToken);
        public IAsyncEnumerable<TElement> FindAsync<TElement>(CancellationToken cancellationToken) where TElement : ElementBase
        {
            var elementNameFromType = Parser.GetElementNameFromType<TElement>(DocType);
            if (elementNameFromType == null) throw new Exception($"Could not determine element name from type: {typeof(TElement).Name}");
            return FindAsync<TElement>(elementNameFromType, cancellationToken);
        }
        public async IAsyncEnumerable<TElement> FindAsync<TElement>(string path, CancellationToken cancellationToken) where TElement : ElementBase
        {
            var iterator = FindInfoAsync(path, cancellationToken);
            await foreach (var elementSource in iterator)
            {
                TElement element;
                if (typeof(TElement) == typeof(ElementBase))
                {
                    // default type requested so we use the best default representation of the element available
                    var type = Parser.GetElementType(elementSource.SchemaElement?.Type, elementSource.Name, DocType);
                    element = (TElement)Activator.CreateInstance(type, Document, elementSource)!;
                }
                else
                {
                    element = (TElement)Activator.CreateInstance(typeof(TElement), Document, elementSource)!;
                }
                yield return element;
            }
        }
        public virtual IAsyncEnumerable<ElementStreamInfo> FindInfoAsync(string path, CancellationToken cancellationToken)
        {
            if (!path.StartsWith("/"))
            {
                path = $"{InstancePath.TrimEnd('/')}/{path}";
            }
            return Document!.FindInfoAsync(path, cancellationToken);
        }
        #endregion
    }
}
