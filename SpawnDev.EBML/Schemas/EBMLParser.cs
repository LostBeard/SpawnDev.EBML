using SpawnDev.EBML.Elements;
using SpawnDev.EBML.ElementTypes;
using SpawnDev.EBML.Engines;
using SpawnDev.EBML.Extensions;
using SpawnDev.EBML.Matroska;
using System.Reflection;

namespace SpawnDev.EBML.Schemas
{
    /// <summary>
    /// This class can parse EBML schema XML and use the parsed schemas to parse, edit, and create EBML documents
    /// </summary>
    public class EBMLParser
    {
        /// <summary>
        /// EBML string "ebml"
        /// </summary>
        public const string EBML = "ebml";
        /// <summary>
        /// Allowed path delimiters
        /// </summary>
        public static char[] PathDelimiters { get; } = new char[] { '\\', '/' };
        /// <summary>
        /// Default Path delimiter<br/>
        /// </summary>
        public static char PathDelimiter { get; } = '/';
        /// <summary>
        /// Index delimiter that can be used in path queries
        /// </summary>
        public static char IndexDelimiter { get; } = ',';
        /// <summary>
        /// Loaded schemas
        /// </summary>
        public Dictionary<string, Schema> Schemas { get; } = new Dictionary<string, Schema>();
        /// <summary>
        /// Document engines can handle document events and provide additional functionality<br/>
        /// For example:<br/>
        /// The included EBML document engine can keep CRC-32 elements up to date if a document is modified.<br/>
        /// The included Matroska document engine can auto-populate SeekHead elements and keep the data in a SeekHead element up to date if a document is modified.<br/>
        /// </summary>
        public IEnumerable<EngineInfo> DocumentEngines => _EBMLDocumentEngines;
        /// <summary>
        /// Create a new ShemaSet and load defaults parser configuration
        /// </summary>
        /// <param name="defaultConfig">If true, all included schemas and document engines will be loaded</param>
        public EBMLParser(bool defaultConfig = true)
        {
            if (defaultConfig)
            {
                LoadDefaultSchemas();
                RegisterDocumentEngine<EBMLEngine>();
                RegisterDocumentEngine<MatroskaEngine>();
                // Add default types to use based on DocType
                // Elements can also be cast to other Element types using Element.As<TElement>()
                DocumentElementTypes.Add("ebml", new Dictionary<string, Type>
                {
                    { "EBML", typeof(EBMLHeader) },
                    { "CRC-32", typeof(CRC32Element) },
                    { "Void", typeof(VoidElement) },
                });
                var matroska = new Dictionary<string, Type>
                {
                    { nameof(Block), typeof(Block) },
                    { nameof(SimpleBlock), typeof(SimpleBlock) },
                    { nameof(Segment), typeof(Segment) },
                    { nameof(Cluster), typeof(Cluster) },
                };
                DocumentElementTypes.Add("matroska", matroska);
                DocumentElementTypes.Add("webm", matroska);
            }
        }
        public static ulong EBMLId { get; } = 0x1A45DFA3;
        public bool IsEBML(Stream stream)
        {
            if (stream == null || stream.Length - stream.Position < 4) return false;
            var pos = stream.Position;
            var id = stream.ReadEBMLElementIdRaw();
            stream.Position = pos;
            return id == EBMLId;
        }
        /// <summary>
        /// Loads schema XMLs that are included with SpawnDev.EBML (currently ebml, matroska, and webm)
        /// </summary>
        /// <param name="predicate">Optional predicate for selective schema loading</param>
        /// <returns></returns>
        public List<Schema> LoadDefaultSchemas(Func<string, bool>? predicate = null)
        {
            var assembly = Assembly.GetExecutingAssembly();
            return LoadEmbeddedSchemaXMLs(assembly, predicate);
        }
        /// <summary>
        /// Parses an xml document into a list of EBML schema
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public List<Schema> ParseSchemas(string xml)
        {
            var schemas = Schema.FromXML(xml);
            foreach (var schema in schemas)
            {
                Schemas[schema.DocType] = schema;
            }
            return schemas;
        }
        /// <summary>
        /// Parses a stream returning EBML documents as they are found<br/>
        /// </summary>
        /// <param name="stream">The stream to read EBMLDocuments from</param>
        /// <returns></returns>
        public IEnumerable<EBMLDocument> ParseDocuments(Stream stream)
        {
            var startPos = stream.Position;
            while (startPos < stream.Length)
            {
                stream.Position = startPos;
                var isEBML = IsEBML(stream);
                stream.Position = startPos;
                if (!isEBML) yield break;
                var doc = new EBMLDocument(stream, this);
                var docSize = doc.TotalSize;
                if (docSize == 0)
                {
                    yield break;
                }
                startPos = startPos + docSize;
                yield return doc;
            }
        }
        /// <summary>
        /// parses a single EBML document from the stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public EBMLDocument? ParseDocument(Stream stream, string? filename = null)
        {
            var startPos = stream.Position;
            var isEBML = IsEBML(stream);
            stream.Position = startPos;
            if (!isEBML) return null;
            return new EBMLDocument(stream, this, filename);
        }
        /// <summary>
        /// Creates a new EBML document with the specified DocType
        /// </summary>
        /// <param name="docType"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public EBMLDocument CreateDocument(string docType, string? filename = null) => new EBMLDocument(docType, this);
        /// <summary>
        /// Returns the element name the Element type is associated with
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="docType"></param>
        /// <returns></returns>
        public string? GetElementNameFromType<TElement>(string docType) where TElement : ElementBase
        {
            var typeT = typeof(TElement);
            var typeName = typeT.Name;
            if (!string.IsNullOrEmpty(docType) && GetElement(typeName, docType) != null)
            {
                return typeName;
            }
            if (!string.IsNullOrEmpty(docType) && DocumentElementTypes.TryGetValue(docType, out var elementTypes))
            {
                var kvp = elementTypes.Where(o => o.Value == typeT).Select(o => o.Key).FirstOrDefault();
                if (!string.IsNullOrEmpty(kvp)) return kvp;
            }
            if (docType != EBML && DocumentElementTypes.TryGetValue(EBML, out var ebmlTypes))
            {
                var kvp = ebmlTypes.Where(o => o.Value == typeT).Select(o => o.Key).FirstOrDefault();
                if (!string.IsNullOrEmpty(kvp)) return kvp;
            }
            var elementNameAttributes = typeT.GetCustomAttributes<ElementNameAttribute>().ToList();
            if (elementNameAttributes.Count == 1) return elementNameAttributes.First().Name;
            var elementNameAttribute = elementNameAttributes.FirstOrDefault(o => o.DocType == docType);
            if (elementNameAttribute != null) return elementNameAttribute.Name;
            var elementNameAttribute1 = elementNameAttributes.FirstOrDefault(o => o.DocType == EBML);
            if (elementNameAttribute1 != null) return elementNameAttribute1.Name;
            return null;
        }
        /// <summary>
        /// Returns a docType specific Element Type if found, otherwise returns the default type for that element.<br/>
        /// elementType:<br/>
        /// - master<br/>
        /// - uinteger<br/>
        /// - integer<br/>
        /// - float<br/>
        /// - binary<br/>
        /// - string<br/>
        /// - utf-8<br/>
        /// - date<br/>
        /// </summary>
        /// <param name="elementType"></param>
        /// <param name="elementName"></param>
        /// <param name="docType"></param>
        /// <returns></returns>
        public Type GetElementType(string elementType, string elementName, string docType)
        {
            if (!string.IsNullOrEmpty(elementName))
            {
                if (!string.IsNullOrEmpty(docType) && DocumentElementTypes.TryGetValue(docType, out var elementTypes) && elementTypes.TryGetValue(elementName, out var type))
                {
                    return type!;
                }
                if (docType != EBML && DocumentElementTypes.TryGetValue(EBML, out var ebmlTypes) && ebmlTypes.TryGetValue(elementName, out var ebmlType))
                {
                    return ebmlType!;
                }
            }
            switch (elementType)
            {
                case "master": return typeof(MasterElement);
                case "uinteger": return typeof(UintElement);
                case "integer": return typeof(IntElement);
                case "float": return typeof(FloatElement);
                case "string": return typeof(StringElement);
                case "utf-8": return typeof(StringElement);
                case "binary": return typeof(BinaryElement);
                case "date": return typeof(DateElement);
                default: return typeof(UnknownElement);
            }
        }
        public Dictionary<string, Dictionary<string, Type>> DocumentElementTypes = new Dictionary<string, Dictionary<string, Type>>();
        /// <summary>
        /// Returns a list of valid schema elements for a specified DocType
        /// </summary>
        /// <param name="docType"></param>
        /// <returns></returns>
        public Dictionary<ulong, SchemaElement> GetElements(string? docType = EBML)
        {
            var ret = docType != EBML && Schemas.TryGetValue(EBML, out var ebmlSchema) ? new Dictionary<ulong, SchemaElement>(ebmlSchema.Elements) : new Dictionary<ulong, SchemaElement>();
            if (!string.IsNullOrEmpty(docType) && Schemas.TryGetValue(docType, out var schema))
            {
                foreach (var kvp in schema.Elements)
                {
                    ret[kvp.Key] = kvp.Value;
                }
            }
            return ret;
        }
        /// <summary>
        /// Returns the schema for the given element id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="docType"></param>
        /// <returns></returns>
        public SchemaElement? GetElement(ulong id, string? docType = EBML)
        {
            if (!string.IsNullOrEmpty(docType) && Schemas.TryGetValue(docType, out var schema) && schema.Elements.TryGetValue(id, out var element)) return element;
            return docType != EBML ? GetElement(id) : null;
        }
        /// <summary>
        /// Returns the schema for the given element name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="docType"></param>
        /// <returns></returns>
        public SchemaElement? GetElement(string name, string? docType = EBML)
        {
            if (!string.IsNullOrEmpty(docType) && Schemas.TryGetValue(docType, out var schema))
            {
                var tmp = schema.Elements.Values.FirstOrDefault(o => o.Name == name);
                if (tmp != null) return tmp;
            }
            return docType != EBML ? GetElement(name) : null;
        }
        /// <summary>
        /// Returns true if the MasterElement can contain the schema element
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="schemaElement"></param>
        /// <returns></returns>
        public bool CheckParent(MasterElement? parent, SchemaElement? schemaElement)
        {
            if (parent == null)
            {
                // must be a top-level allowed object
                return false;
            }
            if (schemaElement == null)
            {
                return false;
            }
            var elementName = schemaElement.Name;
            if (elementName == "CRC-32" || elementName == "Void")
            {
                var nmt = true;
            }
            var parentPath = parent.Path;
            var path = $@"{parentPath.TrimEnd(PathDelimiters)}{PathDelimiter}{elementName}";
            var depth = parent.Depth + 1;
            if (path == schemaElement.Path)
            {
                return true;
            }
            else if (schemaElement.MinDepth > depth)
            {
                return false;
            }
            else if (path == schemaElement.Path.Replace("+", ""))
            {
                // TODO - better check than this
                // this won't work for nested which is what + indicates is possible
                // Tags
                return true;
            }
            return schemaElement.IsGlobal;
        }
        public bool CheckParent(string parentPath, SchemaElement schemaElement)
        {
            if (parentPath == null || schemaElement == null) return false;
            var path = $@"{parentPath.TrimEnd(PathDelimiters)}{PathDelimiter}{schemaElement.Name}";
            var depth = parentPath.Split(PathDelimiters, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length + 1;
            if (path == schemaElement.Path)
            {
                return true;
            }
            else if (schemaElement.MinDepth > depth)
            {
                return false;
            }
            else if (path == schemaElement.Path.Replace("+", ""))
            {
                // TODO - better check than this
                // this won't work for nested which is what + indicates is possible
                // Tags
                return true;
            }
            return schemaElement.IsGlobal;
        }
        /// <summary>
        /// Given a container chain (nested elements) the parents are checked starting from the last up to the first<br/>
        /// removing invalid parents until a valid parent is found<br/>
        /// Used to determine when an element of unknown size ends
        /// </summary>
        /// <param name="parents"></param>
        /// <param name="schemaElement"></param>
        /// <returns></returns>
        public List<MasterElement> CheckParents(List<MasterElement> parents, SchemaElement? schemaElement)
        {
            if (schemaElement == null)
            {
                return parents;
            }
            var tmp = parents.ToList();
            bool ret = false;
            while (!ret && tmp.Count > 0)
            {
                ret = CheckParent(tmp.LastOrDefault(), schemaElement);
                if (!ret) tmp.RemoveAt(tmp.Count - 1);
            }
            return tmp;
        }
        /// <summary>
        /// Register a document engine that can handle document events and provide additional tools for a document
        /// </summary>
        public void RegisterDocumentEngine(Type engineType)
        {
            var ebmlDocumentParserInfo = new EngineInfo(engineType);
            _EBMLDocumentEngines.Add(ebmlDocumentParserInfo);
        }
        /// <summary>
        /// Register a document engine that can handle document events and provide additional tools for a document
        /// </summary>
        public void RegisterDocumentEngine<TEBMLDocumentEngine>() where TEBMLDocumentEngine : DocumentEngine
        {
            var ebmlDocumentParserInfo = new EngineInfo(typeof(TEBMLDocumentEngine));
            _EBMLDocumentEngines.Add(ebmlDocumentParserInfo);
        }
        /// <summary>
        /// Register a document engine that can handle document events and provide additional tools for a document
        /// </summary>
        public void RegisterDocumentEngine<TEBMLDocumentEngine>(Func<EBMLDocument, DocumentEngine> factory) where TEBMLDocumentEngine : DocumentEngine
        {
            var ebmlDocumentParserInfo = new EngineInfo(typeof(TEBMLDocumentEngine), factory);
            _EBMLDocumentEngines.Add(ebmlDocumentParserInfo);
        }
        /// <summary>
        /// Register a document engine that can handle document events and provide additional tools for a document
        /// </summary>
        public void RegisterDocumentEngine(Type engineType, Func<EBMLDocument, DocumentEngine> factory)
        {
            var ebmlDocumentParserInfo = new EngineInfo(engineType, factory);
            _EBMLDocumentEngines.Add(ebmlDocumentParserInfo);
        }
        private List<Schema> LoadEmbeddedSchemaXMLs(Assembly assembly, Func<string, bool>? predicate = null)
        {
            var ret = new List<Schema>();
            var resourceNames = GetEmbeddedSchemasXMLResourceNames(assembly);
            if (predicate != null) resourceNames = resourceNames.Where(predicate).ToArray();
            foreach (var resourceName in resourceNames)
            {
                var tmp = LoadEmbeddedSchemaXML(assembly, resourceName);
                ret.AddRange(tmp);
            }
            return ret;
        }
        private List<Schema> LoadEmbeddedSchemaXML(Assembly assembly, string resourceName)
        {
            var xml = ReadEmbeddedResourceString(assembly, resourceName);
            return string.IsNullOrEmpty(xml) ? new List<Schema>() : ParseSchemas(xml);
        }
        private string[] GetEmbeddedSchemasXMLResourceNames(Assembly assembly)
        {
            var ret = new List<string>();
            var temp = assembly.GetManifestResourceNames();
            foreach (var name in temp)
            {
                if (!name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) continue;
                ret.Add(name);
            }
            return ret.ToArray();
        }
        private string? ReadEmbeddedResourceString(Assembly assembly, string resourceName)
        {
            try
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) return null;
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch
            {
                return null;
            }
        }
        private List<EngineInfo> _EBMLDocumentEngines { get; } = new List<EngineInfo>();
    }
}
