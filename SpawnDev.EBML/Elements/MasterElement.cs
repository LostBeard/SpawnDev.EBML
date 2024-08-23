using SpawnDev.EBML.Crc32;
using SpawnDev.EBML.Segments;

namespace SpawnDev.EBML.Elements
{
    public class MasterElement : BaseElement<IEnumerable<BaseElement>>
    {
        static Crc32Algorithm CRC = new Crc32Algorithm(false);
        public EBMLParser Parser { get; }
        public const string TypeName = "master";
        public override string DataString => $"";
        public event Action<MasterElement, BaseElement> OnElementAdded;
        public event Action<MasterElement, BaseElement> OnElementRemoved;
        /// <summary>
        /// Child elements of this element
        /// </summary>
        public override IEnumerable<BaseElement> Children => Data;
        public MasterElement(EBMLParser schemas, SchemaElement schemaElement, SegmentSource source, ElementHeader? header = null) : base(schemaElement, source, header)
        {
            Parser = schemas;
        }
        public MasterElement(EBMLParser schemas, SegmentSource source, ElementHeader? header = null) : base(null, source, header)
        {
            Parser = schemas;
        }
        public MasterElement(EBMLParser schemas, SchemaElement schemaElement, IEnumerable<BaseElement>? data = null) : base(schemaElement, data != null ? data : new List<BaseElement>())
        {
            Parser = schemas;
        }
        public MasterElement(EBMLParser schemas, ulong id, IEnumerable<BaseElement>? data = null) : base(id, data != null ? data : new List<BaseElement>())
        {
            Parser = schemas;
        }
        public MasterElement(EBMLParser schemas, IEnumerable<BaseElement>? data = null) : base(0, data != null ? data : new List<BaseElement>())
        {
            Parser = schemas;
        }
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
                        var count = Data.Count(o => o.Id == addable.Id);
                        atMaxCount = addable.MaxOccurs > 0 && count >= addable.MaxOccurs;
                    }
                    if (atMaxCount) continue;
                }
                ret.Add(addable);
            }
            return ret;
        }
        /// <summary>
        /// Returns the index of the specified element
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public int GetChildIndex(BaseElement element)
        {
            var data = (List<BaseElement>)Data;
            return data.IndexOf(element);
        }
        /// <summary>
        /// Returns the index of the specified element among the children with the same element Name
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public int GetTypeIndex(BaseElement element)
        {
            var data = (List<BaseElement>)Data;
            var ofType = data.Where(o => o.Name == element.Name).ToList();
            return ofType.IndexOf(element);
        }
        /// <summary>
        /// Returns a list of EBMLSchemaElement for elements that do not occur in this MasterElement as many times as there EBML minOccurs value states it should
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SchemaElement> GetMissingElementSchemas()
        {
            var ret = new List<SchemaElement>();
            var allSchemaElements = Parser.GetElements(DocType);
            foreach (var addable in allSchemaElements.Values)
            {
                var parentAllowed = Parser.CheckParent(this, addable);
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
        /// <summary>
        /// Adds missing container elements to this element
        /// </summary>
        /// <returns></returns>
        public IEnumerable<MasterElement> AddMissingContainers()
        {
            var missing = GetMissingElementSchemas();
            var masterEls = missing.Where(o => o.Type == MasterElement.TypeName).Select(o => AddElement(o)).Cast<MasterElement>().ToList();
            return masterEls;
        }
        #region Create, but do not add
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is not added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="data">Optional child elements</param>
        /// <returns>The new element</returns>
        /// <exception cref="Exception">Thrown if the element name is not found in the schema</exception>
        public MasterElement CreateContainer(string name, IEnumerable<BaseElement>? data = null)
        {
            var schemaElement = Parser.GetElement(name, DocType);
            if (schemaElement == null || schemaElement.Type != MasterElement.TypeName) throw new Exception("Invalid element type");
            var masterEl = new MasterElement(Parser, schemaElement, data);
            return masterEl;
        }
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is not added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="data">Element data value</param>
        /// <returns>The new element</returns>
        /// <exception cref="Exception">Thrown if the element name is not found in the schema</exception>
        public UTF8Element CreateUTF8(string name, string data)
        {
            var schemaElement = Parser.GetElement(name, DocType);
            if (schemaElement == null || schemaElement.Type != UTF8Element.TypeName) throw new Exception("Invalid element type");
            var element = new UTF8Element(schemaElement, data);
            return element;
        }
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is not added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="data">Element data value</param>
        /// <returns>The new element</returns>
        /// <exception cref="Exception">Thrown if the element name is not found in the schema</exception>
        public StringElement CreateASCII(string name, string data)
        {
            var schemaElement = Parser.GetElement(name, DocType);
            if (schemaElement == null || schemaElement.Type != StringElement.TypeName) throw new Exception("Invalid element type");
            var element = new StringElement(schemaElement, data);
            return element;
        }
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is not added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="data">Element data value</param>
        /// <returns>The new element</returns>
        /// <exception cref="Exception">Thrown if the element name is not found in the schema</exception>
        public UintElement CreateUint(string name, ulong data)
        {
            var schemaElement = Parser.GetElement(name, DocType);
            if (schemaElement == null || schemaElement.Type != UintElement.TypeName) throw new Exception("Invalid element type");
            var element = new UintElement(schemaElement, data);
            return element;
        }
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is not added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="data">Element data value</param>
        /// <returns>The new element</returns>
        /// <exception cref="Exception">Thrown if the element name is not found in the schema</exception>
        public IntElement CreateInt(string name, long data)
        {
            var schemaElement = Parser.GetElement(name, DocType);
            if (schemaElement == null || schemaElement.Type != IntElement.TypeName) throw new Exception("Invalid element type");
            var element = new IntElement(schemaElement, data);
            return element;
        }
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is not added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="data">Element data value</param>
        /// <returns>The new element</returns>
        /// <exception cref="Exception">Thrown if the element name is not found in the schema</exception>
        public FloatElement CreateFloat(string name, double data)
        {
            var schemaElement = Parser.GetElement(name, DocType);
            if (schemaElement == null || schemaElement.Type != FloatElement.TypeName) throw new Exception("Invalid element type");
            var element = new FloatElement(schemaElement, data);
            return element;
        }
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is not added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="data">Element data value</param>
        /// <returns>The new element</returns>
        /// <exception cref="Exception">Thrown if the element name is not found in the schema</exception>
        public DateElement CreateDate(string name, DateTime data)
        {
            var schemaElement = Parser.GetElement(name, DocType);
            if (schemaElement == null || schemaElement.Type != DateElement.TypeName) throw new Exception("Invalid element type");
            var element = new DateElement(schemaElement, data);
            return element;
        }
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is not added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="data">Element data value</param>
        /// <returns>The new element</returns>
        /// <exception cref="Exception">Thrown if the element name is not found in the schema</exception>
        public BinaryElement CreateBinary(string name, byte[] data)
        {
            var schemaElement = Parser.GetElement(name, DocType);
            if (schemaElement == null || schemaElement.Type != BinaryElement.TypeName) throw new Exception("Invalid element type");
            var element = new BinaryElement(schemaElement, data);
            return element;
        }
        /// <summary>
        /// Create a new element
        /// </summary>
        /// <param name="schemaElement">Element schema</param>
        /// <param name="source">Element binary data source as a SegmentSource</param>
        /// <param name="header">Header element if available</param>
        /// <returns>The new element</returns>
        public BaseElement Create(SchemaElement schemaElement, SegmentSource source, ElementHeader? header = null)
        {
            if (schemaElement == null) throw new NullReferenceException(nameof(schemaElement));
            var type = Parser.GetElementType(schemaElement.Type);
            BaseElement? ret = schemaElement.Type switch
            {
                MasterElement.TypeName => new MasterElement(Parser, schemaElement, source, header),
                UintElement.TypeName => new UintElement(schemaElement, source, header),
                IntElement.TypeName => new IntElement(schemaElement, source, header),
                FloatElement.TypeName => new FloatElement(schemaElement, source, header),
                StringElement.TypeName => new StringElement(schemaElement, source, header),
                UTF8Element.TypeName => new UTF8Element(schemaElement, source, header),
                BinaryElement.TypeName => new BinaryElement(schemaElement, source, header),
                DateElement.TypeName => new DateElement(schemaElement, source, header),
                _ => throw new Exception("Invalid schema element")
            };
            return ret;
        }
        /// <summary>
        /// Create a new element
        /// </summary>
        /// <param name="schemaElement">Element schema</param>
        /// <returns>The new element</returns>
        public TElement CreateElement<TElement>(SchemaElement schemaElement) where TElement : BaseElement
        {
            if (schemaElement == null) throw new NullReferenceException(nameof(schemaElement));
            var type = Parser.GetElementType(schemaElement.Type);
            if (!typeof(TElement).IsAssignableFrom(type)) throw new Exception("Create type mismatch");
            BaseElement? ret = schemaElement.Type switch
            {
                MasterElement.TypeName => new MasterElement(Parser, schemaElement),
                UintElement.TypeName => new UintElement(schemaElement),
                IntElement.TypeName => new IntElement(schemaElement),
                FloatElement.TypeName => new FloatElement(schemaElement),
                StringElement.TypeName => new StringElement(schemaElement),
                UTF8Element.TypeName => new UTF8Element(schemaElement),
                BinaryElement.TypeName => new BinaryElement(schemaElement),
                DateElement.TypeName => new DateElement(schemaElement),
                _ => throw new Exception("Invalid schema element")
            };
            return (TElement)ret;
        }
        /// <summary>
        /// Create a new element
        /// </summary>
        /// <param name="schemaElement">Element schema</param>
        /// <returns>The new element</returns>
        public BaseElement CreateElement(SchemaElement schemaElement)
        {
            if (schemaElement == null) throw new NullReferenceException(nameof(schemaElement));
            var type = Parser.GetElementType(schemaElement.Type);
            BaseElement? ret = schemaElement.Type switch
            {
                MasterElement.TypeName => new MasterElement(Parser, schemaElement),
                UintElement.TypeName => new UintElement(schemaElement),
                IntElement.TypeName => new IntElement(schemaElement),
                FloatElement.TypeName => new FloatElement(schemaElement),
                StringElement.TypeName => new StringElement(schemaElement),
                UTF8Element.TypeName => new UTF8Element(schemaElement),
                BinaryElement.TypeName => new BinaryElement(schemaElement),
                DateElement.TypeName => new DateElement(schemaElement),
                _ => throw new Exception("Invalid type")
            };
            return ret;
        }
        #endregion
        #region Add
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="data">Optional child elements</param>
        /// <returns>The new element</returns>
        /// <exception cref="Exception">Thrown if the element name is not found in the schema</exception>
        public MasterElement AddContainer(string name, IEnumerable<BaseElement>? data = null) => AddElement(CreateContainer(name, data));
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="data">Element data value</param>
        /// <returns>The new element</returns>
        /// <exception cref="Exception">Thrown if the element name is not found in the schema</exception>
        public UTF8Element AddUTF8(string name, string data) => AddElement(CreateUTF8(name, data));
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="data">Element data value</param>
        /// <returns>The new element</returns>
        /// <exception cref="Exception">Thrown if the element name is not found in the schema</exception>
        public StringElement AddASCII(string name, string data)=> AddElement(CreateASCII(name, data));
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="data">Element data value</param>
        /// <returns>The new element</returns>
        /// <exception cref="Exception">Thrown if the element name is not found in the schema</exception>
        public UintElement AddUint(string name, ulong data)=> AddElement(CreateUint(name, data));
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="data">Element data value</param>
        /// <returns>The new element</returns>
        /// <exception cref="Exception">Thrown if the element name is not found in the schema</exception>
        public IntElement AddInt(string name, long data)=> AddElement(CreateInt(name, data));
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="data">Element data value</param>
        /// <returns>The new element</returns>
        /// <exception cref="Exception">Thrown if the element name is not found in the schema</exception>
        public FloatElement AddFloat(string name, double data)=> AddElement(CreateFloat(name, data));
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="data">Element data value</param>
        /// <returns>The new element</returns>
        /// <exception cref="Exception">Thrown if the element name is not found in the schema</exception>
        public DateElement AddDate(string name, DateTime data) => AddElement(CreateDate(name, data));
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="data">Element data value</param>
        /// <returns>The new element</returns>
        /// <exception cref="Exception">Thrown if the element name is not found in the schema</exception>
        public BinaryElement AddBinary(string name, byte[] data) => AddElement(CreateBinary(name, data));
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is added to this element's Data
        /// </summary>
        /// <param name="elementSchema">Element schema</param>
        /// <returns>The new element</returns>
        public TElement AddElement<TElement>(SchemaElement elementSchema) where TElement : BaseElement => AddElement(CreateElement<TElement>(elementSchema));
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is added to this element's Data
        /// </summary>
        /// <param name="elementSchema">Element schema</param>
        /// <returns>The new element</returns>
        public BaseElement AddElement(SchemaElement elementSchema) => AddElement(CreateElement(elementSchema));
        #endregion
        #region Insert element
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="data">Optional child elements</param>
        /// <param name="index">Index in this element's Data list</param>
        /// <returns>The new element</returns>
        public MasterElement InsertContainer(string name, IEnumerable<BaseElement>? data = null, int index = 0) => InsertElement(CreateContainer(name, data), index);
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="index">Index in this element's Data list</param>
        /// <returns>The new element</returns>
        public MasterElement InsertContainer(string name, int index = 0) => InsertElement(CreateContainer(name), index);
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="data">Element data</param>
        /// <param name="index">Index in this element's Data list</param>
        /// <returns>The new element</returns>
        public FloatElement InsertFloat(string name, double data, int index = 0) => InsertElement(CreateFloat(name, data), index);
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="data">Element data</param>
        /// <param name="index">Index in this element's Data list</param>
        /// <returns>The new element</returns>
        public IntElement InsertInt(string name, long data, int index = 0) => InsertElement(CreateInt(name, data), index);
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="data">Element data</param>
        /// <param name="index">Index in this element's Data list</param>
        /// <returns>The new element</returns>
        public UintElement InsertUint(string name, ulong data, int index = 0) => InsertElement(CreateUint(name, data), index);
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="data">Element data</param>
        /// <param name="index">Index in this element's Data list</param>
        /// <returns>The new element</returns>
        public UTF8Element InsertUTF8(string name, string data, int index = 0) => InsertElement(CreateUTF8(name, data), index);
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="data">Element data</param>
        /// <param name="index">Index in this element's Data list</param>
        /// <returns>The new element</returns>
        public StringElement InsertASCII(string name, string data, int index = 0) => InsertElement(CreateASCII(name, data), index);
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="data">Element data</param>
        /// <param name="index">Index in this element's Data list</param>
        /// <returns>The new element</returns>
        public DateElement InsertDate(string name, DateTime data, int index = 0) => InsertElement(CreateDate(name, data), index);
        /// <summary>
        /// Creates a new element using this element's DocType<br/>
        /// The new element is added to this element's Data
        /// </summary>
        /// <param name="name">Element schema name</param>
        /// <param name="data">Element data</param>
        /// <param name="index">Index in this element's Data list</param>
        /// <returns>The new element</returns>
        public BinaryElement InsertBinary(string name, byte[] data, int index = 0) => InsertElement(CreateBinary(name, data), index);
        /// <summary>
        /// Insert a child element
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="element"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public TElement InsertElement<TElement>(TElement element, int index = 0) where TElement : BaseElement
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
            var data = (List<BaseElement>)Data;
            if (data.Contains(element)) return element;
            data.Insert(0, element);
            if (element.Parent != this) element.SetParent(this);
            element.OnChanged += Child_OnChanged;
            if (element is MasterElement masterElement)
            {
                masterElement.OnElementAdded += Child_OnElementAdded;
                masterElement.OnElementRemoved += Child_ElementRemoved;
            }
            OnElementAdded?.Invoke(this, element);
            DataChanged(new List<BaseElement> { element });
            return element;
        }
        #endregion
        /// <summary>
        /// Updates the container's CRC-32 element if it has one
        /// </summary>
        /// <returns>Returns true if the CRC was updated</returns>
        public bool UpdateCRC()
        {
            var crcEl = Data.FirstOrDefault(o => o is BinaryElement && o.Name == "CRC-32");
            if (crcEl is BinaryElement binaryElement)
            {
                Console.WriteLine($"Verifying CRC in: {Path}");
                var crc = CalculateCRC();
                if (crc != null && !binaryElement.Data.SequenceEqual(crc))
                {
                    Console.WriteLine($"CRC about to update: {Path}");
                    binaryElement.Data = crc;
                    Console.WriteLine($"CRC updated: {Path}");
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Calculate this elements CRC-32 value
        /// </summary>
        /// <returns></returns>
        public byte[]? CalculateCRC()
        {
            var crcSchema = Parser.GetElement("CRC-32");
            if (crcSchema == null) return null;
            var dataToCRC = Data.Where(o => o.Id != crcSchema.Id).Select(o => o.ToStream());
            using var stream = new MultiStreamSegment(dataToCRC);
            var hash = CRC.ComputeHash(stream);
            return hash;
        }
        /// <summary>
        /// Remove a child element
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="element"></param>
        /// <returns></returns>
        public TElement RemoveElement<TElement>(TElement element) where TElement : BaseElement
        {
            var data = (List<BaseElement>)Data;
            if (!data.Contains(element)) return element;
            data.Remove(element);
            element.OnChanged -= Child_OnChanged;
            if (element is MasterElement masterElement)
            {
                masterElement.OnElementAdded -= Child_OnElementAdded;
                masterElement.OnElementRemoved -= Child_ElementRemoved;
            }
            if (element.Parent == this) element.Remove();
            OnElementRemoved?.Invoke(this, element);
            DataChanged();
            return element;
        }
        /// <summary>
        /// Add a child element
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="element"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
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
            var data = (List<BaseElement>)Data;
            if (data.Contains(element))
            {
                return element;
            }
            var added = false;
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
            if (!added) data.Add(element);
            if (element.Parent != this) element.SetParent(this);
            element.OnChanged += Child_OnChanged;
            if (element is MasterElement masterElement)
            {
                masterElement.OnElementAdded += Child_OnElementAdded;
                masterElement.OnElementRemoved += Child_ElementRemoved;
            }
            OnElementAdded?.Invoke(this, element);
            DataChanged(new List<BaseElement> { element });
            return element;
        }
        #region Update element value
        /// <summary>
        /// Update an element's value at the specified path
        /// </summary>
        /// <param name="path">Element path</param>
        /// <param name="data">New value</param>
        /// <returns>True if the element data was set</returns>
        public bool UpdateDate(string path, DateTime data)
        {
            var element = GetElement<DateElement>(path);
            if (element == null) return false;
            element.Data = data;
            return true;
        }
        /// <summary>
        /// Update an element's value at the specified path
        /// </summary>
        /// <param name="path">Element path</param>
        /// <param name="data">New value</param>
        /// <returns>True if the element data was set</returns>
        public bool UpdateFloat(string path, double data)
        {
            var element = GetElement<FloatElement>(path);
            if (element == null) return false;
            element.Data = data;
            return true;
        }
        /// <summary>
        /// Update an element's value at the specified path
        /// </summary>
        /// <param name="path">Element path</param>
        /// <param name="data">New value</param>
        /// <returns>True if the element data was set</returns>
        public bool UpdateBinary(string path, byte[] data)
        {
            var element = GetElement<BinaryElement>(path);
            if (element == null) return false;
            element.Data = data;
            return true;
        }
        /// <summary>
        /// Update an element's value at the specified path
        /// </summary>
        /// <param name="path">Element path</param>
        /// <param name="data">New value</param>
        /// <returns>True if the element data was set</returns>
        public bool UpdateInt(string path, long data)
        {
            var element = GetElement<IntElement>(path);
            if (element == null) return false;
            element.Data = data;
            return true;
        }
        /// <summary>
        /// Update an element's value at the specified path
        /// </summary>
        /// <param name="path">Element path</param>
        /// <param name="data">New value</param>
        /// <returns>True if the element data was set</returns>
        public bool UpdateUint(string path, ulong data)
        {
            var element = GetElement<UintElement>(path);
            if (element == null) return false;
            element.Data = data;
            return true;
        }
        /// <summary>
        /// Update an element's value at the specified path
        /// </summary>
        /// <param name="path">Element path</param>
        /// <param name="data">New value</param>
        /// <returns>True if the element data was set</returns>
        public bool UpdateUTF8(string path, string data)
        {
            var element = GetElement<UTF8Element>(path);
            if (element == null) return false;
            element.Data = data;
            return true;
        }
        /// <summary>
        /// Update an element's value at the specified path
        /// </summary>
        /// <param name="path">Element path</param>
        /// <param name="data">New value</param>
        /// <returns>True if the element data was set</returns>
        public bool UpdateASCII(string path, string data)
        {
            var element = GetElement<StringElement>(path);
            if (element == null) return false;
            element.Data = data;
            return true;
        }
        /// <summary>
        /// Update an element's value at the specified path
        /// </summary>
        /// <param name="path">Element path</param>
        /// <param name="data">New value</param>
        /// <returns>True if the element data was set</returns>
        public bool UpdateString(string path, string data)
        {
            var element = GetElement<BaseElement>(path);
            if (element == null) return false;
            if (element is StringElement stringElement) stringElement.Data = data;
            else if (element is UTF8Element utf8Element) utf8Element.Data = data;
            else return false;
            return true;
        }
        #endregion
        #region Read element value
        /// <summary>
        /// Read the element data at the specified path
        /// </summary>
        /// <param name="path">Element path</param>
        /// <returns>The element value or null</returns>
        public DateTime? ReadDate(string path) => GetElement<DateElement>(path)?.Data;
        /// <summary>
        /// Read the element data at the specified path
        /// </summary>
        /// <param name="path">Element path</param>
        /// <returns>The element value or null</returns>
        public double? ReadFloat(string path) => GetElement<FloatElement>(path)?.Data;
        /// <summary>
        /// Read the element data at the specified path
        /// </summary>
        /// <param name="path">Element path</param>
        /// <returns>The element value or null</returns>
        public byte[]? ReadBinary(string path) => GetElement<BinaryElement>(path)?.Data;
        /// <summary>
        /// Read the element data at the specified path
        /// </summary>
        /// <param name="path">Element path</param>
        /// <returns>The element value or null</returns>
        public long? ReadInt(string path) => GetElement<IntElement>(path)?.Data;
        /// <summary>
        /// Read the element data at the specified path
        /// </summary>
        /// <param name="path">Element path</param>
        /// <returns>The element value or null</returns>
        public ulong? ReadUint(string path) => GetElement<UintElement>(path)?.Data;
        /// <summary>
        /// Read the element data at the specified path
        /// </summary>
        /// <param name="path">Element path</param>
        /// <returns>The element value or null</returns>
        public string? ReadUTF8(string path) => GetElement<UTF8Element>(path)?.Data;
        /// <summary>
        /// Read the element data at the specified path
        /// </summary>
        /// <param name="path">Element path</param>
        /// <returns>The element value or null</returns>
        public string? ReadASCII(string path) => GetElement<StringElement>(path)?.Data;
        /// <summary>
        /// Read the element data at the specified path
        /// </summary>
        /// <param name="path">Element path</param>
        /// <returns>The element value or null</returns>
        public string? ReadString(string path)
        {
            var stringElement = GetElement<BaseElement>(path);
            if (stringElement == null) return null;
            if (stringElement is UTF8Element stringUTF8) return stringUTF8.Data;
            if (stringElement is StringElement stringASCII) return stringASCII.Data;
            return null;
        }
        #endregion
        /// <summary>
        /// Get all children recursively
        /// </summary>
        public IEnumerable<BaseElement> Descendants
        {
            get
            {
                var ret = new List<BaseElement>();
                var children = Data;
                ret.AddRange(children);
                foreach (var child in children)
                {
                    if (child is MasterElement masterElement)
                    {
                        ret.AddRange(masterElement.Descendants);
                    }
                }
                return ret;
            }
        }
        public BaseElement? GetElement(string path) => GetElements(path).FirstOrDefault();
        public TElement? GetElement<TElement>(string path) where TElement : BaseElement => GetElements<TElement>(path).FirstOrDefault();
        /// <summary>
        /// Returns all elements that match the specified path and return type TElement
        /// </summary>
        /// <typeparam name="TElement">The element type filter</typeparam>
        /// <param name="path">
        /// Element path. Example:<br/>
        /// - "/EBML/DocType"<br/>
        /// </param>
        /// <returns>The match results</returns>
        public IEnumerable<TElement> GetElements<TElement>(string path) where TElement : BaseElement
        {
            var parts = path.Split(EBMLParser.PathDelimiters, StringSplitOptions.TrimEntries);
            if (parts.Length >= 2 && parts[0] == "" && parts[1] == "")
            {
                parts = parts.Skip(2).ToArray();
                path = string.Join(EBMLParser.PathDelimiter, parts);
                var document = Document;
                if (this != document)
                {
                    return document == null ? Enumerable.Empty<TElement>() : document.GetElements<TElement>(path);
                }
            }
            else if (parts.Length >= 1 && parts[0] == "")
            {
                parts = parts.Skip(1).ToArray();
                path = string.Join(EBMLParser.PathDelimiter, parts);
                var rootElement = LastAncestor;
                if (rootElement != null)
                {
                    return rootElement == null ? Enumerable.Empty<TElement>() : rootElement.GetElements<TElement>(path);
                }
            }
            if (parts.Length == 0) return Data.Where(o => o is TElement).Cast<TElement>().ToList();
            var masterEls = new List<MasterElement> { this };
            for (var i = 0; i < parts.Length - 1; i++)
            {
                var pathPart = parts[i];
                masterEls = masterEls.SelectMany(o => o.GetChildren<MasterElement>(pathPart)).ToList();
                if (masterEls.Count == 0) return Enumerable.Empty<TElement>();
            }
            var finalPart = parts.Last();
            return masterEls.SelectMany(o => o.GetChildren<TElement>(finalPart)).ToList();
        }
        internal IEnumerable<TElement> GetChildren<TElement>(string path) where TElement : BaseElement
        {
            var ret = new List<TElement>();
            var matchIndex = 0;
            NameParts(path, out var childName, out var childIndex);
            var requireChildName = !string.IsNullOrEmpty(childName);
            foreach (var child in Data)
            {
                if (child is TElement childMaster && (!requireChildName || childMaster.Name == childName))
                {
                    if (childIndex < 0 || matchIndex == childIndex)
                    {
                        ret.Add(childMaster);
                    }
                    matchIndex++;
                }
            }
            return ret;
        }
        private void NameParts(string instanceName, out string name, out int index)
        {
            var partParts = instanceName.Split(EBMLParser.IndexDelimiter);
            if (partParts.Length == 2)
            {
                index = int.Parse(partParts[1]);
                name = partParts[0];
            }
            else
            {
                index = -1; // indicates no specific instance requested
                name = instanceName;
            }
        }
        public IEnumerable<BaseElement> GetElements(string path) => GetElements<BaseElement>(path);
        public IEnumerable<MasterElement> GetContainers(string path) => GetElements<MasterElement>(path);
        public MasterElement? GetContainer(string path) => GetElements<MasterElement>(path).FirstOrDefault();
        protected override void DataFromSegmentSource(ref IEnumerable<BaseElement> _data)
        {
            SegmentSource.Position = 0;
            var source = SegmentSource;
            var data = new List<BaseElement>();
            _data = data;
            var isUnknownSize = _ElementHeader != null && _ElementHeader.Size == null;
            var parsingDocType = DocType ?? EBMLParser.EBML;
            while (true)
            {
                BaseElement? element = null;
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
                var schemaElement = Parser.GetElement(id, parsingDocType);
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
                var canParent = Parser.CheckParent(this, schemaElement);
                if (!canParent)
                {
                    source.Position = elementDataOffset;
                    break;
                }
                else
                {
                    element = Create(schemaElement, elementSegmentSource, elementHeader);
                    if (element == null)
                    {
                        element = new BinaryElement(schemaElement, source, elementHeader);
                        //ret = new BaseElement(id, schemaElement, elementSegmentSource, elementHeader);
                    }
                }
                if (element == null)
                {
                    break;
                }
                data.Add(element);
                element.SetParent(this);
                element.OnChanged += Child_OnChanged;
                if (element is MasterElement masterElement)
                {
                    masterElement.OnElementAdded += Child_OnElementAdded;
                    masterElement.OnElementRemoved += Child_ElementRemoved;
                    if (element.Name == "EBML")
                    {
                        var newDocType = masterElement.ReadString("DocType");
                        if (!string.IsNullOrEmpty(newDocType))
                        {
                            parsingDocType = newDocType;
                        }
                    }
                }
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
        }
        private void Child_ElementRemoved(MasterElement masterElement, BaseElement element)
        {
            OnElementRemoved?.Invoke(masterElement, element);
        }
        private void Child_OnElementAdded(MasterElement masterElement, BaseElement element)
        {
            OnElementAdded?.Invoke(masterElement, element);
        }
        private void Child_OnChanged(IEnumerable<BaseElement> elements)
        {
            DataChanged(elements);
        }
        protected override void DataToSegmentSource(ref SegmentSource source)
        {
            source = new MultiStreamSegment(Data.Select(o => o.ToStream()).ToArray());
        }
    }
}
