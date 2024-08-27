using SpawnDev.EBML.Extensions;
using SpawnDev.PatchStreams;

namespace SpawnDev.EBML.Streams
{
    public partial class MasterElement : Element
    {
        public MasterElement(StreamElementInfo element) : base(element)
        {
        }
        public MasterElement(EBMLParser parser, PatchStream patchStream, string instancePath = "/", string docType = "") : base(parser, patchStream, instancePath, docType)
        { }
        public MasterElement(EBMLParser parser, Stream stream, string instancePath = "/", string docType = "") : base(parser, stream is PatchStream patchStream ? patchStream : new PatchStream(stream), instancePath, docType)
        { }
        public MasterElement(PatchStream patchStream, string instancePath = "/", string docType = "") : base(new EBMLParser(), patchStream, instancePath, docType)
        { }
        public MasterElement(Stream stream, string instancePath = "/", string docType = "") : base(new EBMLParser(), stream is PatchStream patchStream ? patchStream : new PatchStream(stream), instancePath, docType)
        { }

        public BinaryElement AddBinaryElement(string name, byte[]? value = null)
        {
            var ret = AddElement<BinaryElement>(name);
            if (value != null)
            {
                ret.Value = value;
                Update();
            }
            return ret;
        }
        public DateElement AddDateElement(string name, DateTime? value = null)
        {
            var ret = AddElement<DateElement>(name);
            if (value != null)
            {
                ret.Value = value.Value;
                Update();
            }
            return ret;
        }
        public FloatElement AddFloatElement(string name, double? value = null)
        {
            var ret = AddElement<FloatElement>(name);
            if (value != null)
            {
                ret.Value = value.Value;
                Update();
            }
            return ret;
        }
        public UintElement AddUintElement(string name, ulong? value = null)
        {
            var ret = AddElement<UintElement>(name);
            if (value != null)
            {
                ret.Value = value.Value;
                Update();
            }
            return ret;
        }
        public IntElement AddIntElement(string name, long? value = null)
        {
            var ret = AddElement<IntElement>(name);
            if (value != null)
            {
                ret.Value = value.Value;
                Update();
            }
            return ret;
        }
        public bool RemoveFirst(string name)
        {
            var ret = Find(name).FirstOrDefault();
            return ret?.Remove() ?? false;
        }
        public int RemoveAll(string name)
        {
            var ret = Find(name);
            return ret.Select(o => o.Remove()).Where(o => o).Count();
        }
        public int RemoveAll()
        {
            var ret = Find("");
            return ret.Select(o => o.Remove()).Where(o => o).Count();
        }
        public string? ReadString(string name) => Find<StringElement>(name).FirstOrDefault()?.Value;
        public StringElement? WriteString(string name, string value)
        {
            var ret = Find<StringElement>(name).FirstOrDefault();
            if (ret != null) ret.Value = value;
            else
                ret = AddStringElement(name, value);
            return ret;
        }
        public StringElement AddStringElement(string name, string? value = null)
        {
            var ret = AddElement<StringElement>(name);
            if (value != null)
            {
                ret.Value = value;
                Update();
            }
            return ret;
        }
        public MasterElement AddMasterElement(string name)
        {
            var ret = AddElement<MasterElement>(name);
            return ret;
        }
        public TElement AddElement<TElement>(string name) where TElement : Element
        {
            EBMLConverter.NameToNameIndex(name, out var namei, out var index, true);
            return AddElement<TElement>(index, namei);
        }
        public TElement AddElement<TElement>(int atIndex, string name) where TElement : Element
        {
            // here, index in name is ignore. if nameIndex notations idesired, use the method that does not contain an index
            EBMLConverter.NameToNameIndex(name, out var namei, out var index, true);
            var pos = Stream.Position;
            Update();
            var children = Children;
            long insertPosition = -1;
            var i = 0;
            long lastStartOffset = -1;
            foreach(var child in children)
            {
                if (atIndex >= 0 && atIndex == i)
                {
                    insertPosition = child.Offset;
                    break;
                }
                lastStartOffset = child.Size == null ? -1 : (long)child.Size.Value + child.DataOffset;
                i++;
            }
            if (i == 0)
            {
                insertPosition = DataOffset;
            }
            else if (insertPosition < 0 && lastStartOffset >= 0)
            {
                insertPosition = lastStartOffset;
            }
            if (!Exists) throw new Exception("This element no longer exists");
            var schemaElement = EBMLParser.GetElement(namei, SchemaElement?.DocType ?? EBMLParser.EBML);
            if (schemaElement == null)
            {
                var docType = DocType;
                if (!string.IsNullOrEmpty(docType))
                {
                    schemaElement = EBMLParser.GetElement(namei, docType);
                }
            }
            if (schemaElement == null) throw new Exception("Element type not found");
            var newElementStream = new MemoryStream();
            newElementStream.WriteEBMLElementIdRaw(schemaElement.Id);
            newElementStream.WriteEBMLElementSize(0);
            newElementStream.Position = 0;
            var currentSize = MaxDataSize;
            Stream.Position = insertPosition;
            Stream.Insert(newElementStream);
            // now update all parents headers to reflect the added data
            ResizeAdd(newElementStream.Length);
            Update();
            var ret = Find<TElement>($",{i}").First();
            return ret;
        }
        public Element AddElement(string name) => AddElement<Element>(name);
    }
}
