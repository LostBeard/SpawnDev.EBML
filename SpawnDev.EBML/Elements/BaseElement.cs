using SpawnDev.EBML.Extensions;
using SpawnDev.EBML.Segments;

namespace SpawnDev.EBML.Elements
{
    public enum Source
    {
        None,
        SegmentSource,
        Data,
    }
    public class BaseElement
    {
        public long Offset
        {
            get
            {
                var index = Index;
                if (index < 0) return 0;
                long offset = 0;
                BaseElement prev = index == 0 ? Parent! : Parent!.Data.ElementAt(index - 1);
                if (index == 0)
                {
                    offset = prev.Offset + (long)Parent!.HeaderSize;
                }
                else
                {
                    offset = prev.Offset + (long)prev.TotalSize;
                }
                return offset;
            }
        }
        public virtual Source Source { get; protected set; }
        public int Index => Parent == null ? -1 : Parent.GetChildIndex(this);
        public virtual string Type => SchemaElement?.Type ?? "";
        public virtual string DataString { get; set; } = "";
        public virtual string DocType => SchemaElement?.DocType ?? Parent?.DocType ?? EBMLSchemaSet.EBML;
        public virtual MasterElement? Parent { get; private set; }
        internal virtual void SetParent(MasterElement? parent)
        {
            if (Parent == parent) return;
            if (Parent != null) Remove();
            Parent = parent;
        }
        public void Remove()
        {
            if (Parent == null) return;
            var parent = Parent;
            Parent = null;
            parent.RemoveElement(this);
        }
        /// <summary>
        /// Returns true if this element requires a parent and does not have one
        /// </summary>
        public bool IsOrphan => ElementHeaderRequired && Parent == null;
        /// <summary>
        /// Returns true if the Depth == 0 and not an orphan
        /// </summary>
        public bool PathIsRoot => !IsOrphan && Depth == -1;
        /// <summary>
        /// Returns true if the Depth == 1 and not an orphan
        /// </summary>
        public bool PathIsTopLevel => !IsOrphan && Depth == 0;
        public int Depth => Path.Split('\\', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length - 1;
        public string? Name => SchemaElement?.Name ?? (Id == 0 ? "" : $"{Id}");
        public ulong TotalSize => DataSize + HeaderSize;
        public ulong HeaderSize => ElementHeader == null ? 0 : (ulong)ElementHeader.HeaderSize;
        public virtual ulong DataSize => (ulong)SegmentSource.Length;
        public virtual bool ElementHeaderRequired { get; } = true;
        public ulong Id { get; protected set; }
        public string IdHex => $"0x{Convert.ToHexString(EBMLConverter.ToUIntBytes(Id))}";
        protected virtual ElementHeader? _ElementHeader { get; set; }
        public ElementHeader? ElementHeader
        {
            get
            {
                if (_ElementHeader == null && ElementHeaderRequired) _ElementHeader = new ElementHeader(Id, DataSize);
                return _ElementHeader;
            }
            set => _ElementHeader = ElementHeaderRequired ? value : null;
        }
        public EBMLSchemaElement? SchemaElement { get; set; }
        public bool Modified { get; set; }
        public virtual string Path => Parent == null ? $@"\{Name}" : $@"{Parent.Path.TrimEnd('\\')}\{Name}";
        protected virtual SegmentSource? _SegmentSource { get; set; } = null;
        public virtual SegmentSource SegmentSource
        {
            get
            {
                if (_SegmentSource == null)
                {
                    _SegmentSource = DataToSegmentSource();
                }
                return _SegmentSource;
            }
            set
            {
                _SegmentSource = value;
                if (_SegmentSource == null) return;
                StreamChanged();
            }
        }
        public MasterElement? GetRootLevelElement()
        {
            MasterElement? ret = Parent;
            while (ret?.Parent != null)
            {
                ret = ret.Parent;
            }
            return ret;
        }
        public EBMLDocument? GetDocumentElement()
        {
            return GetRootLevelElement() as EBMLDocument;
        }
        protected virtual void StreamChanged() { }
        /// <summary>
        /// Constructor used by MasterElements when reading elements from its SegmentSource
        /// </summary>
        public BaseElement(ulong id, EBMLSchemaElement? schemaElement, SegmentSource source, ElementHeader? header)
        {
            Id = id;
            SchemaElement = schemaElement;
            _SegmentSource = source;
            if (_SegmentSource != null) Source = Source.SegmentSource;
            ElementHeader = header;
        }
        /// <summary>
        /// Constructor used by typed BaseElements
        /// </summary>
        /// <param name="id"></param>
        /// <param name="schemaElement"></param>
        public BaseElement(ulong id, EBMLSchemaElement? schemaElement)
        {
            SchemaElement = schemaElement;
            Id = id;
        }
        public virtual void CopyTo(Stream stream)
        {
            SegmentSource.Seek(0, SeekOrigin.Begin);
            if (ElementHeader != null)
            {
                ElementHeader.CopyTo(stream);
            }
            SegmentSource.Position = 0;
            SegmentSource.CopyTo(stream);
        }
        public virtual async Task CopyToAsync(Stream stream)
        {
            SegmentSource.Seek(0, SeekOrigin.Begin);
            if (ElementHeader != null)
            {
                await ElementHeader.CopyToAsync(stream);
            }
            SegmentSource.Position = 0;
            await SegmentSource.CopyToAsync(stream);
        }
        protected virtual SegmentSource DataToSegmentSource() => throw new NotImplementedException();
        protected virtual void Changed() => OnChanged?.Invoke(this);
        public event Action<BaseElement> OnChanged;
        public Stream ToStream()
        {
            return ElementHeader == null ? new MultiStreamSegment(new Stream[] { SegmentSource }) : new MultiStreamSegment(new Stream[] { ElementHeader.SegmentSource, SegmentSource });
        }
        public byte[] ToBytes()
        {
            using var stream = ToStream();
            byte[] output = new byte[stream.Length];
            int bytesRead = stream.Read(output, 0, output.Length);
            return output;
        }
    }
    public abstract class BaseElement<T> : BaseElement
    {
        public override string DataString
        {
            get => Data?.ToString() ?? "";
        }
        protected virtual Lazy<T> _Data { get; set; }
        protected virtual bool EqualCheck(T obj1, T obj2) => obj1?.Equals(obj2) ?? false;
        public virtual T Data
        {
            get => _Data.Value;
            set
            {
                if (_Data.IsValueCreated && EqualCheck(value, _Data.Value)) return;
                _Data = new Lazy<T>(value);
                DataChanged();
            }
        }
        public BaseElement(EBMLSchemaElement schemaElement, T data) : base(schemaElement.Id, schemaElement)
        {
            Source = Source.Data;
            _Data = new Lazy<T>(data);
        }
        /// <summary>
        /// Creates new instance with data, and unknown EBMLSchemaElement
        /// </summary>
        public BaseElement(ulong id, T data) : base(id, null)
        {
            Source = Source.Data;
            _Data = new Lazy<T>(data);
        }
        public BaseElement(EBMLSchemaElement? schemaElement, SegmentSource source, ElementHeader? header) : base(schemaElement?.Id ?? header?.Id ?? 0, schemaElement, source, header)
        {
            Source = Source.SegmentSource;
            _Data = new Lazy<T>(() => DataFromSegmentSource());
        }
        /// <summary>
        /// This method must must convert SegmentSource and to type T<br/>
        /// Starts at 0 and is the full size of the SegmentSource unless unknown size
        /// </summary>
        protected abstract T DataFromSegmentSource();
        protected virtual void DataChanged()
        {
            Source = Source.Data;
            Modified = true;
            ElementHeader = null;
            _SegmentSource = null;
            Changed();
        }
        protected override void StreamChanged()
        {
            Source = Source.SegmentSource;
            Modified = true;
            ElementHeader = null;
            _Data = new Lazy<T>(() => DataFromSegmentSource());
            Changed();
        }
    }
}
