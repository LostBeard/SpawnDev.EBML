using SpawnDev.EBML.Crc32;
using SpawnDev.EBML.Segments;
using System.Xml.Linq;

namespace SpawnDev.EBML.Elements
{
    public class MasterElement : BaseElement<IEnumerable<BaseElement>>
    {
        public EBMLSchemaSet SchemaSet { get; }
        public const string TypeName = "master";
        public override string DataString => $"";
        public event Action<BaseElement> ElementFound;
        public event Action<BaseElement> ElementRemoved;
        //public virtual ulong DataSize => CalculatedSize;
        public MasterElement(EBMLSchemaSet schemas, EBMLSchemaElement schemaElement, SegmentSource source, ElementHeader? header = null) : base(schemaElement, source, header)
        {
            SchemaSet = schemas;
        }
        public MasterElement(EBMLSchemaSet schemas, SegmentSource source, ElementHeader? header = null) : base(null, source, header)
        {
            SchemaSet = schemas;
        }
        public MasterElement(EBMLSchemaSet schemas, EBMLSchemaElement schemaElement) : base(schemaElement, new List<BaseElement>())
        {
            SchemaSet = schemas;
        }
        public MasterElement(EBMLSchemaSet schemas, ulong id) : base(id, new List<BaseElement>())
        {
            SchemaSet = schemas;
        }
        public MasterElement(EBMLSchemaSet schemas) : base(0, new List<BaseElement>())
        {
            SchemaSet = schemas;
        }
        /// <summary>
        /// Returns a list of EBMLSchemaElement that can be added to this MasterElement
        /// </summary>
        /// <param name="includeMaxCountItems"></param>
        /// <returns></returns>
        public IEnumerable<EBMLSchemaElement> GetAddableElementSchemas(bool includeMaxCountItems = false)
        {
            var ret = new List<EBMLSchemaElement>();
            var allSchemaElements = SchemaSet.GetElements(DocType);
            foreach (var addable in allSchemaElements.Values)
            {
                var parentAllowed = SchemaSet.CheckParent(this, addable);
                if (!parentAllowed) continue;
                if (!includeMaxCountItems)
                {
                    var atMaxCount = false;
                    if (addable.MaxOccurs > 0 || addable.MinOccurs > 0)
                    {
                        var count = Data.Count(o => o.Id == addable.Id);
                        atMaxCount = addable.MaxOccurs > 0 && count >= addable.MaxOccurs;
                    }
                    if (atMaxCount) continue;
                }
                ret.Add(addable);
            }
            return ret;
        }
        public int ChildIndex(BaseElement element)
        {
            var data = (List<BaseElement>)Data;
            return data.IndexOf(element);
        }
        /// <summary>
        /// Returns a list of EBMLSchemaElement for elements that do not occur in this MasterElement as many times as there EBML minOccurs value states it should
        /// </summary>
        /// <returns></returns>
        public IEnumerable<EBMLSchemaElement> GetMissingElementSchemas()
        {
            var ret = new List<EBMLSchemaElement>();
            var allSchemaElements = SchemaSet.GetElements(DocType);
            foreach (var addable in allSchemaElements.Values)
            {
                var parentAllowed = SchemaSet.CheckParent(this, addable);
                if (!parentAllowed) continue;
                var requiresAdd = false;
                if (addable.MinOccurs > 0)
                {
                    var count = Data.Count(o => o.Id == addable.Id);
                    requiresAdd = count < addable.MinOccurs;
                    if (requiresAdd) ret.Add(addable);
                }
            }
            return ret;
        }
        public IEnumerable<MasterElement> AddMissingContainers()
        {
            var missing = GetMissingElementSchemas();
            var masterEls = missing.Where(o => o.Type == MasterElement.TypeName).Select(o => AddElement(o)).Cast<MasterElement>().ToList();
            return masterEls;
        }
        public MasterElement? AddContainer(string name)
        {
            var schemaElement = SchemaSet.GetEBMLSchemaElement(name, DocType);
            if (schemaElement == null || schemaElement.Type != MasterElement.TypeName) throw new Exception("Invalid element type");
            var masterEl = new MasterElement(SchemaSet, schemaElement);
            AddElement(masterEl);
            return masterEl;
        }
        public UTF8Element? AddUTF8(string name, string data)
        {
            var schemaElement = SchemaSet.GetEBMLSchemaElement(name, DocType);
            if (schemaElement == null || schemaElement.Type != UTF8Element.TypeName) throw new Exception("Invalid element type");
            var element = new UTF8Element(schemaElement, data);
            AddElement(element);
            return element;
        }
        public StringElement? AddString(string name, string data)
        {
            var schemaElement = SchemaSet.GetEBMLSchemaElement(name, DocType);
            if (schemaElement == null || schemaElement.Type != StringElement.TypeName) throw new Exception("Invalid element type");
            var element = new StringElement(schemaElement, data);
            AddElement(element);
            return element;
        }
        public UintElement? AddUint(string name, ulong data)
        {
            var schemaElement = SchemaSet.GetEBMLSchemaElement(name, DocType);
            if (schemaElement == null || schemaElement.Type != UintElement.TypeName) throw new Exception("Invalid element type");
            var element = new UintElement(schemaElement, data);
            AddElement(element);
            return element;
        }
        public IntElement? AddInt(string name, long data)
        {
            var schemaElement = SchemaSet.GetEBMLSchemaElement(name, DocType);
            if (schemaElement == null || schemaElement.Type != IntElement.TypeName) throw new Exception("Invalid element type");
            var element = new IntElement(schemaElement, data);
            AddElement(element);
            return element;
        }
        public FloatElement? AddFloat(string name, double data)
        {
            var schemaElement = SchemaSet.GetEBMLSchemaElement(name, DocType);
            if (schemaElement == null || schemaElement.Type != FloatElement.TypeName) throw new Exception("Invalid element type");
            var element = new FloatElement(schemaElement, data);
            AddElement(element);
            return element;
        }
        public DateElement? AddDate(string name, DateTime data)
        {
            var schemaElement = SchemaSet.GetEBMLSchemaElement(name, DocType);
            if (schemaElement == null || schemaElement.Type != DateElement.TypeName) throw new Exception("Invalid element type");
            var element = new DateElement(schemaElement, data);
            AddElement(element);
            return element;
        }
        public BinaryElement? AddBinary(string name, byte[] data)
        {
            var schemaElement = SchemaSet.GetEBMLSchemaElement(name, DocType);
            if (schemaElement == null || schemaElement.Type != BinaryElement.TypeName) throw new Exception("Invalid element type");
            var element = new BinaryElement(schemaElement, data);
            AddElement(element);
            return element;
        }
        public TElement RemoveElement<TElement>(TElement element) where TElement : BaseElement
        {
            var data = (List<BaseElement>)Data;
            if (!data.Contains(element)) return element;
            data.Remove(element);
            element.OnChanged -= Child_OnChanged;
            element.SetParent(null);
            ElementRemoved?.Invoke(element);
            DataChanged();
            return element;
        }
        public TElement AddElement<TElement>(EBMLSchemaElement elementSchema) where TElement : BaseElement
        {
            var element = SchemaSet.Create<TElement>(elementSchema);
            if (element == null) return null;
            return AddElement(element);
        }
        public BaseElement? AddElement(EBMLSchemaElement elementSchema)
        {
            var element = SchemaSet.Create(elementSchema);
            if (element == null) return null;
            return AddElement(element);
        }
        static Crc32Algorithm CRC = new Crc32Algorithm(false);
        /// <summary>
        /// Updates the container's CRC-32 element if it has one
        /// </summary>
        public void UpdateCRC()
        {
            var crcEl = Data.FirstOrDefault(o => o is BinaryElement && o.Name == "CRC-32");
            if (crcEl is BinaryElement binaryElement)
            {
                var crc = CalculateCRC();
                if (crc != null && !binaryElement.Data.SequenceEqual(crc))
                {
                    binaryElement.Data = crc;
                }
            }
        }
        public byte[]? CalculateCRC()
        {
            var crcSchema = SchemaSet.GetEBMLSchemaElement("CRC-32");
            if (crcSchema == null) return null;
            var dataToCRC = Data.Where(o => o.Id != crcSchema.Id).Select(o => o.ToStream());
            using var stream = new MultiStreamSegment(dataToCRC);
            var hash = CRC.ComputeHash(stream);
            return hash;
        }
        public TElement AddElement<TElement>(TElement element) where TElement : BaseElement
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
            if (element.SchemaElement == null)
            {
                throw new ArgumentNullException(nameof(element.SchemaElement));
            }
            element.SetParent(this);
            element.OnChanged += Child_OnChanged;
            if (element.Path == @"\EBML" && element is MasterElement ebmlMaster && this is EBMLDocument thisDoc)
            {
                var docType1 = ebmlMaster.ReadString("DocType");
                if (!string.IsNullOrEmpty(docType1)) thisDoc._DocType = docType1;
            }
            var added = false;
            var data = (List<BaseElement>)Data;
            if (element.SchemaElement.Position != null)
            {
                if (element.SchemaElement.Position.Value == 0)
                {
                    // first
                    var index = 0;
                    for (var i = 0; i < data.Count; i++)
                    {
                        var item = data[i];
                        if (item.SchemaElement == null) break;
                        if (item.SchemaElement.Position == null) break;
                        var itemPos = item.SchemaElement.Position.Value;
                        if (itemPos > 0) break;
                        if (item.SchemaElement.PositionWeight < element.SchemaElement.PositionWeight) break;
                        index = i;
                    }
                    added = true;
                    data.Insert(index, element);
                }
                else if (element.SchemaElement.Position.Value == -1)
                {
                    // last
                    // TODO
                }
            }
            if (element.Name == "CRC-32" && element is BinaryElement binaryElement)
            {
                var crc = CalculateCRC();
                if (crc != null) binaryElement.Data = crc;
            }
            if (!added) data.Add(element);
            ElementFound?.Invoke(element);
            DataChanged();
            return element;
        }
        public TElement InsertElement<TElement>(TElement element, int index = 0) where TElement : BaseElement
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
            element.SetParent(this);
            element.OnChanged += Child_OnChanged;
            if (element.Path == @"\EBML" && element is MasterElement ebmlMaster && this is EBMLDocument thisDoc)
            {
                var docType1 = ebmlMaster.ReadString("DocType");
                if (!string.IsNullOrEmpty(docType1)) thisDoc._DocType = docType1;
            }
            var data = (List<BaseElement>)Data;
            data.Insert(0, element);
            ElementFound?.Invoke(element);
            DataChanged();
            return element;
        }
        private void Child_OnChanged(BaseElement obj)
        {
            if (obj.Name != "CRC-32")
            {
                UpdateCRC();
            }
            DataChanged();
        }
        public string? ReadUTF8(string path)
        {
            var els = GetElements<UTF8Element>(path).FirstOrDefault();
            return els?.Data;
        }
        public string? ReadASCII(string path)
        {
            var els = GetElements<StringElement>(path).FirstOrDefault();
            return els?.Data;
        }
        public string? ReadString(string path)
        {
            var stringElement = GetElements<BaseElement>(path).FirstOrDefault();
            if (stringElement == null) return null;
            if (stringElement is UTF8Element stringUTF8) return stringUTF8.Data;
            if (stringElement is StringElement stringASCII) return stringASCII.Data;
            throw new Exception("Unknown type");
        }
        //public ulong CalculatedSize
        //{
        //    get
        //    {
        //        ulong ret = 0;
        //        var children = Data;
        //        foreach (var child in children)
        //        {
        //            if (child is MasterElement masterElement)
        //            {
        //                ret += masterElement.CalculatedSize;
        //            }
        //            else
        //            {
        //                ret += child.TotalSize;
        //            }
        //        }
        //        return ret;
        //    }
        //}
        /// <summary>
        /// Returns all children recursively
        /// </summary>
        /// <returns></returns>
        public IEnumerable<BaseElement> GetDescendants()
        {
            var ret = new List<BaseElement>();
            var children = Data;
            ret.AddRange(children);
            foreach (var child in children)
            {
                if (child is MasterElement masterElement)
                {
                    ret.AddRange(masterElement.GetDescendants());
                }
            }
            return ret;
        }
        public TElement? GetElement<TElement>(string path) where TElement : BaseElement => GetElements<TElement>(path).FirstOrDefault();
        public IEnumerable<TElement> GetElements<TElement>(string path) where TElement : BaseElement
        {
            var parts = path.Split('\\', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return this is TElement ? new TElement[] { (this as TElement)! } : Enumerable.Empty<TElement>();
            }
            var masterEls = new List<MasterElement> { this };
            for (var i = 0; i < parts.Length - 1; i++)
            {
                var elementName = parts[i];
                masterEls = masterEls.SelectMany(o => o.Data.Where(o => o.Name == elementName && o is MasterElement).Cast<MasterElement>()).ToList();
                if (masterEls.Count == 0) return Enumerable.Empty<TElement>();
            }
            var elementNameFinal = parts.Last();
            var results = masterEls.SelectMany(o => o.Data.Where(o => o.Name == elementNameFinal && o is TElement)!).Cast<TElement>().ToList();
            return results;
        }
        public IEnumerable<BaseElement> GetElements(string path) => GetElements<BaseElement>(path);
        public IEnumerable<MasterElement> GetContainers(string path) => GetElements<MasterElement>(path);
        public MasterElement? GetContainer(string path) => GetElements<MasterElement>(path).FirstOrDefault();
        protected override IEnumerable<BaseElement> DataFromSegmentSource()
        {
            SegmentSource.Position = 0;
            var source = SegmentSource;
            var data = new List<BaseElement>();
            //var rootElement = GetDocumentElement();
            var isUnknownSize = _ElementHeader != null && _ElementHeader.Size == null;
            while (true)
            {
                BaseElement? ret = null;
                var elementHeaderOffset = source.Position;
                if (source.Position == source.Length)
                {
                    break;
                }
                ElementHeader elementHeader;
                try
                {
                    elementHeader = ElementHeader.Read(source);
                }
                catch (Exception ex)
                {
                    break;
                }
                var elementDataOffset = source.Position;
                var id = elementHeader.Id;
                var schemaElement = SchemaSet.GetEBMLSchemaElement(id, DocType);
                if (schemaElement == null)
                {
                    var nmttt = true;
                }
                var elementDataSize = elementHeader.Size;
                var elementName = schemaElement?.Name ?? $"{id}";
                // The end of an Unknown - Sized Element is determined by whichever comes first:
                // - Any EBML Element that is a valid Parent Element of the Unknown - Sized Element according to the EBML Schema, Global Elements excluded.
                // - Any valid EBML Element according to the EBML Schema, Global Elements excluded, that is not a Descendant Element of the Unknown-Sized Element but shares a common direct parent, such as a Top - Level Element.
                // - Any EBML Element that is a valid Root Element according to the EBML Schema, Global Elements excluded.
                // - The end of the Parent Element with a known size has been reached.
                // - The end of the EBML Document, either when reaching the end of the file or because a new EBML Header started.
                var elementDataSizeMax = elementDataSize ?? (ulong)(SegmentSource.Length - elementDataOffset);
                var elementSegmentSource = SegmentSource.Slice(elementDataOffset, (long)elementDataSizeMax);
                var canParent = SchemaSet.CheckParent(this, schemaElement);
                if (!canParent)
                {
                    source.Position = elementDataOffset;
                    break;
                }
                else
                {
                    ret = SchemaSet.Create(schemaElement, elementSegmentSource, elementHeader);
                    if (ret == null)
                    {
                        ret = new BaseElement(id, schemaElement, elementSegmentSource, elementHeader);
                    }
                }
                if (ret == null)
                {
                    break;
                }
                ret.SetParent(this);
                ret.OnChanged += Child_OnChanged;
                data.Add(ret);
                if (ret.Path == @"\EBML")
                {
                    if (ret is MasterElement ebmlMaster && this is EBMLDocument thisDoc)
                    {
                        var docType1 = ebmlMaster.ReadString("DocType");
                        if (!string.IsNullOrEmpty(docType1)) thisDoc._DocType = docType1;
                    }
                }
                ElementFound?.Invoke(ret);
                SegmentSource.Position = (long)elementDataSizeMax + elementDataOffset;
            }
            if (isUnknownSize)
            {
                // - create a new header with the actual size of the master element
                // - re-slice the SegmentSource so it is only as big as the element
                var last = data.LastOrDefault();
                var measuredSize = last == null ? 0 : (long)last.SegmentSource.Offset + (long)last.DataSize - (long)SegmentSource.Offset;
                if (last != null)
                {
                    if (ElementHeader != null) ElementHeader.Size = (ulong)measuredSize;
                    _SegmentSource = SegmentSource.Slice(0, measuredSize);
                }
            }
            return data;
        }
        protected override SegmentSource DataToSegmentSource()
        {
            return new MultiStreamSegment(Data.Select(o => o.ToStream()).ToArray());
        }
    }
}
