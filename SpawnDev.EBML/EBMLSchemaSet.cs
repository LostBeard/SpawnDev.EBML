using SpawnDev.EBML.Elements;
using SpawnDev.EBML.Segments;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;

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
        public BaseElement? Create(EBMLSchemaElement? schemaElement, SegmentSource source, ElementHeader? header = null)
        {
            if (schemaElement == null) return null;
            var type = GetElementType(schemaElement.Type);
            if (type == null) return null;
            BaseElement? ret = schemaElement.Type switch
            {
                MasterElement.TypeName => new MasterElement(this, schemaElement, source, header),
                UintElement.TypeName => new UintElement(schemaElement, source, header),
                IntElement.TypeName => new IntElement(schemaElement, source, header),
                FloatElement.TypeName => new FloatElement(schemaElement, source, header),
                StringElement.TypeName => new StringElement(schemaElement, source, header),
                UTF8Element.TypeName => new UTF8Element(schemaElement, source, header),
                BinaryElement.TypeName => new BinaryElement(schemaElement, source, header),
                DateElement.TypeName => new DateElement(schemaElement, source, header),
                _ => null
            };
            return ret;
        }
        public TElement? Create<TElement>(EBMLSchemaElement? schemaElement) where TElement : BaseElement
        {
            if (schemaElement == null) return null;
            var type = GetElementType(schemaElement.Type);
            if (type == null) return null;
            if (!typeof(TElement).IsAssignableFrom(type)) throw new Exception("Create type mismatch");
            BaseElement? ret = schemaElement.Type switch
            {
                MasterElement.TypeName => new MasterElement(this, schemaElement),
                UintElement.TypeName => new UintElement(schemaElement),
                IntElement.TypeName => new IntElement(schemaElement),
                FloatElement.TypeName => new FloatElement(schemaElement),
                StringElement.TypeName => new StringElement(schemaElement),
                UTF8Element.TypeName => new UTF8Element(schemaElement),
                BinaryElement.TypeName => new BinaryElement(schemaElement),
                DateElement.TypeName => new DateElement(schemaElement),
                _ => null
            };
            return (TElement?)ret;
        }
        public BaseElement? Create(EBMLSchemaElement? schemaElement)
        {
            if (schemaElement == null) return null;
            var type = GetElementType(schemaElement.Type);
            if (type == null) return null;
            BaseElement? ret = schemaElement.Type switch
            {
                MasterElement.TypeName => new MasterElement(this, schemaElement),
                UintElement.TypeName => new UintElement(schemaElement),
                IntElement.TypeName => new IntElement(schemaElement),
                FloatElement.TypeName => new FloatElement(schemaElement),
                StringElement.TypeName => new StringElement(schemaElement),
                UTF8Element.TypeName => new UTF8Element(schemaElement),
                BinaryElement.TypeName => new BinaryElement(schemaElement),
                DateElement.TypeName => new DateElement(schemaElement),
                _ => null
            };
            return ret;
        }
        public MasterElement CreateContainer(EBMLSchemaElement schemaElement)
        {
            if (schemaElement?.Type != MasterElement.TypeName) throw new Exception("Cannot create element: invalid EBMLSchemaElement.");
            return new MasterElement(this, schemaElement);
        }
        public UintElement CreateUint(EBMLSchemaElement schemaElement, ulong value)
        {
            if (schemaElement?.Type != UintElement.TypeName) throw new Exception("Cannot create element: invalid EBMLSchemaElement.");
            return new UintElement(schemaElement, value);
        }
        public IntElement CreateInt(EBMLSchemaElement schemaElement, long value)
        {
            if (schemaElement?.Type != IntElement.TypeName) throw new Exception("Cannot create element: invalid EBMLSchemaElement.");
            return new IntElement(schemaElement, value);
        }
        public FloatElement CreateFloat(EBMLSchemaElement schemaElement, double value)
        {
            if (schemaElement?.Type != FloatElement.TypeName) throw new Exception("Cannot create element: invalid EBMLSchemaElement.");
            return new FloatElement(schemaElement, value);
        }
        public UTF8Element CreateUTF8(EBMLSchemaElement schemaElement, string value)
        {
            if (schemaElement?.Type != UTF8Element.TypeName) throw new Exception("Cannot create element: invalid EBMLSchemaElement.");
            return new UTF8Element(schemaElement, value);
        }
        public StringElement CreateString(EBMLSchemaElement schemaElement, string value)
        {
            if (schemaElement?.Type != StringElement.TypeName) throw new Exception("Cannot create element: invalid EBMLSchemaElement.");
            return new StringElement(schemaElement, value);
        }
        public BinaryElement CreateBinary(EBMLSchemaElement schemaElement, byte[] value)
        {
            if (schemaElement?.Type != BinaryElement.TypeName) throw new Exception("Cannot create element: invalid EBMLSchemaElement.");
            return new BinaryElement(schemaElement, value);
        }
        public DateElement CreateDate(EBMLSchemaElement schemaElement, DateTime value)
        {
            if (schemaElement?.Type != DateElement.TypeName) throw new Exception("Cannot create element: invalid EBMLSchemaElement.");
            return new DateElement(schemaElement, value);
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
            var ret = docType != EBML && Schemas.TryGetValue(EBML, out var ebmlSchema) ? ebmlSchema.Elements : new Dictionary<ulong, EBMLSchemaElement>();
            if (Schemas.TryGetValue(docType, out var schema))
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
            if (Schemas.TryGetValue(docType, out var schema) && schema.Elements.TryGetValue(id, out var element)) return element;
            return docType != EBML ? GetEBMLSchemaElement(id) : null;
        }
        public EBMLSchemaElement? GetEBMLSchemaElement(string name, string docType = EBML)
        {
            if (Schemas.TryGetValue(docType, out var schema))
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
                // TODO
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
            if (elementName == "CRC-32")
            {
                var nmt = true;
            }
            else if (elementName == "Void")
            {
                var nmt = true;
            }
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
                return true;
            }
            //else if (elementName == "CRC-32")
            //{
            //    return true;
            //}
            //else if (elementName == "Void")
            //{
            //    return true;
            //}
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
                ret.AddRange(LoadEmbeddedSchemaXML(assembly, resourceName));
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
    }
}
