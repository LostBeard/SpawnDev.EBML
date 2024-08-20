using SpawnDev.EBML.Elements;
using SpawnDev.EBML.Segments;
using System.Reflection;

namespace SpawnDev.EBML
{
    public class EBMLSchemaSet
    {
        public const string EBML = "ebml";
        public Dictionary<string, EBMLSchema> Schemas { get; } = new Dictionary<string, EBMLSchema>();
        public List<EBMLSchema> ParseXML(string xml)
        {
            var schemas = EBMLSchema.FromXML(xml);
            foreach (var schema in schemas)
            {
                Schemas[schema.DocType] = schema;
            }
            return schemas;
        }
        public Type? GetElementType(string elementType)
        {
            switch (elementType)
            {
                case MasterElement.TypeName: return typeof(MasterElement);
                case UintElement.TypeName: return typeof(UintElement);
                case IntElement.TypeName: return typeof(IntElement);
                case FloatElement.TypeName: return typeof(FloatElement);
                case StringElement.TypeName: return typeof(StringElement);
                case UTF8Element.TypeName: return typeof(UTF8Element);
                case BinaryElement.TypeName: return typeof(BinaryElement);
                case DateElement.TypeName: return typeof(DateElement);
                default: return null;
            }
        }
        public Dictionary<ulong, EBMLSchemaElement> GetElements(string docType = EBML)
        {
            var ret = docType != EBML && Schemas.TryGetValue(EBML, out var ebmlSchema) ? new Dictionary<ulong, EBMLSchemaElement>(ebmlSchema.Elements) : new Dictionary<ulong, EBMLSchemaElement>();
            if (!string.IsNullOrEmpty(docType) && Schemas.TryGetValue(docType, out var schema))
            {
                foreach (var kvp in schema.Elements)
                {
                    ret[kvp.Key] = kvp.Value;
                }
            }
            return ret;
        }
        public EBMLSchemaElement? GetEBMLSchemaElement(ulong id, string docType = EBML)
        {
            if (!string.IsNullOrEmpty(docType) && Schemas.TryGetValue(docType, out var schema) && schema.Elements.TryGetValue(id, out var element)) return element;
            return docType != EBML ? GetEBMLSchemaElement(id) : null;
        }
        public EBMLSchemaElement? GetEBMLSchemaElement(string name, string docType = EBML)
        {
            if (!string.IsNullOrEmpty(docType) && Schemas.TryGetValue(docType, out var schema))
            {
                var tmp = schema.Elements.Values.FirstOrDefault(o => o.Name == name);
                if (tmp != null) return tmp;
            }
            return docType != EBML ? GetEBMLSchemaElement(name) : null;
        }
        /// <summary>
        /// Returns true if the MasterElement can contain the schema element
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="schemaElement"></param>
        /// <returns></returns>
        public bool CheckParent(MasterElement? parent, EBMLSchemaElement? schemaElement)
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
            var parentPath = parent.Path;
            var parentMasterName = parent.Name;
            var path = $@"{parentPath.TrimEnd('\\')}\{elementName}";
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
        public List<MasterElement> CheckParents(List<MasterElement> parents, EBMLSchemaElement? schemaElement)
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
        public List<EBMLSchema> LoadExecutingAssemblyEmbeddedSchemaXMLs(Func<string, bool>? predicate = null)
        {
            var assembly = Assembly.GetExecutingAssembly();
            return LoadEmbeddedSchemaXMLs(assembly, predicate);
        }
        public List<EBMLSchema> LoadCallingAssemblyEmbeddedSchemaXMLs(Func<string, bool>? predicate = null)
        {
            var assembly = Assembly.GetCallingAssembly();
            return LoadEmbeddedSchemaXMLs(assembly, predicate);
        }
        public List<EBMLSchema> LoadEmbeddedSchemaXMLs(Assembly assembly, Func<string, bool>? predicate = null)
        {
            var ret = new List<EBMLSchema>();
            var resourceNames = GetEmbeddedSchemasXMLResourceNames(assembly);
            if (predicate != null) resourceNames = resourceNames.Where(predicate).ToArray();
            foreach (var resourceName in resourceNames)
            {
                var tmp = LoadEmbeddedSchemaXML(assembly, resourceName);
                ret.AddRange(tmp);
            }
            return ret;
        }
        public List<EBMLSchema> LoadEmbeddedSchemaXML(Assembly assembly, string resourceName)
        {
            var xml = ReadEmbeddedResourceString(assembly, resourceName);
            return string.IsNullOrEmpty(xml) ? new List<EBMLSchema>() : ParseXML(xml);
        }
        public List<EBMLSchema> LoadExecutingAssemblyEmbeddedSchemaXML(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            return LoadEmbeddedSchemaXML(assembly, resourceName);
        }
        public string[] GetExecutingAssemblyEmbeddedSchemasXMLResourceNames()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return GetEmbeddedSchemasXMLResourceNames(assembly);
        }
        public List<EBMLSchema> LoadCallingAssemblyEmbeddedSchemaXML(string resourceName)
        {
            var assembly = Assembly.GetCallingAssembly();
            return LoadEmbeddedSchemaXML(assembly, resourceName);
        }
        public string[] GetCallingAssemblyEmbeddedSchemasXMLResourceNames()
        {
            var assembly = Assembly.GetCallingAssembly();
            return GetEmbeddedSchemasXMLResourceNames(assembly);
        }
        public string[] GetEmbeddedSchemasXMLResourceNames(Assembly assembly)
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
        public string? ReadEmbeddedResourceString(Assembly assembly, string resourceName)
        {
            try
            {
                using (Stream stream = assembly.GetManifestResourceStream(resourceName)!)
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
            catch
            {
                return null;
            }
        }
        public IEnumerable<EBMLDocument> Parse(Stream stream)
        {
            while (stream.Position < stream.Length)
            {
                var startPos = stream.Position;
                var doc = new EBMLDocument(stream, this);
                if (doc.Data.Count() == 0)
                {
                    yield break;
                }
                var parserInfo = EBMLDocumentParsers.FirstOrDefault(o => o.DocTypes.Contains(doc.DocType));
                if (parserInfo != null)
                {
                    var newDoc = parserInfo.Create(doc);

                }
                var docSize = doc.TotalSize;
                stream.Position = startPos + (long)docSize;
                yield return doc;
            }
        }
        public List<EBMLDocumentParserInfo> _EBMLDocumentParsers { get; } = new List<EBMLDocumentParserInfo>();
        public IEnumerable<EBMLDocumentParserInfo> EBMLDocumentParsers => _EBMLDocumentParsers;
        public void RegisterEBMLDocumentType(Type parserType, string docType) => RegisterEBMLDocumentType(parserType, new[] { docType });
        public void RegisterEBMLDocumentType(Type parserType, IEnumerable<string> docTypes) 
        {
            // find the required constructor (EBMLDocument)
            var constructor = parserType.GetConstructors().FirstOrDefault(o =>
            {
                var tmp = o.GetParameters();
                return tmp.Length == 1 && typeof(EBMLDocument).IsAssignableFrom(tmp[0].ParameterType);
            });
            if (constructor == null)
            {
                throw new Exception("EBMLDocument parser does not have a valid constructor.");
            }
            var factory = (EBMLDocument doc) => (EBMLDocument)constructor.Invoke(new object?[] { doc });
            var ebmlDocumentParserInfo = new EBMLDocumentParserInfo(docTypes, parserType, factory);
            _EBMLDocumentParsers.Add(ebmlDocumentParserInfo);
        }
        public void RegisterEBMLDocumentType<TEBMLDocument>(string docType) where TEBMLDocument : EBMLDocument => RegisterEBMLDocumentType<TEBMLDocument>(new[] { docType });
        public void RegisterEBMLDocumentType<TEBMLDocument>(IEnumerable<string> docTypes) where TEBMLDocument : EBMLDocument
        {
            var parserType = typeof(TEBMLDocument);
            // find the required constructor (EBMLDocument)
            var constructor = parserType.GetConstructors().FirstOrDefault(o =>
            {
                var tmp = o.GetParameters();
                return tmp.Length == 1 && typeof(EBMLDocument).IsAssignableFrom(tmp[0].ParameterType);
            });
            if (constructor == null)
            {
                throw new Exception("EBMLDocument parser does not have a valid constructor.");
            }
            var factory = (EBMLDocument doc) => (EBMLDocument)constructor.Invoke(new object?[] { doc });
            var ebmlDocumentParserInfo = new EBMLDocumentParserInfo(docTypes, parserType, factory);
            _EBMLDocumentParsers.Add(ebmlDocumentParserInfo);
        }
        public void RegisterEBMLDocumentType<TEBMLDocument>(string docType, Func<EBMLDocument, EBMLDocument> factory) where TEBMLDocument : EBMLDocument => RegisterEBMLDocumentType<TEBMLDocument>(new[] { docType }, factory);
        public void RegisterEBMLDocumentType<TEBMLDocument>(IEnumerable<string> docTypes, Func<EBMLDocument, EBMLDocument> factory) where TEBMLDocument : EBMLDocument
        {
            var parserType = typeof(TEBMLDocument);
            var ebmlDocumentParserInfo = new EBMLDocumentParserInfo(docTypes, parserType, factory);
            _EBMLDocumentParsers.Add(ebmlDocumentParserInfo);
        }
        public void RegisterEBMLDocumentType(Type parserType, string docType, Func<EBMLDocument, EBMLDocument> factory) => RegisterEBMLDocumentType(parserType, new[] { docType }, factory);
        public void RegisterEBMLDocumentType(Type parserType, IEnumerable<string> docTypes, Func<EBMLDocument, EBMLDocument> factory) 
        {
            var ebmlDocumentParserInfo = new EBMLDocumentParserInfo(docTypes, parserType, factory);
            _EBMLDocumentParsers.Add(ebmlDocumentParserInfo);
        }
    }
}
