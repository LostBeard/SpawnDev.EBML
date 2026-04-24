using SpawnDev.EBML.Extensions;
using SpawnDev.EBML.Schemas;
using SpawnDev.PatchStreams;

namespace SpawnDev.EBML.Elements
{
    /// <summary>
    /// Describes a single element in a stream
    /// </summary>
    public class ElementStreamInfo
    {
        /// <summary>
        /// Element's id
        /// </summary>
        public ulong Id { get; protected internal set; }
        /// <summary>
        /// Element's schema information
        /// </summary>
        public SchemaElement? SchemaElement { get; protected internal set; }
        /// <summary>
        /// Element's name
        /// </summary>
        public string Name { get; protected internal set; } = "";
        /// <summary>
        /// This element's parent element
        /// </summary>
        public ElementStreamInfo? Parent { get; protected internal set; }
        /// <summary>
        /// The byte offset of this element in the stream
        /// </summary>
        public long Offset { get; protected internal set; }
        /// <summary>
        /// The size of this element where null represents unknown size
        /// </summary>
        public ulong? Size { get; protected internal set; }
        /// <summary>
        /// If known, the size of this element, else the max size of this element (usually end of stream)
        /// </summary>
        public long DataSize { get; protected internal set; }
        /// <summary>
        /// The total size of this element including the header. May be 0 if a header has not been created yet.
        /// </summary>
        public long TotalSize { get; protected internal set; }
        /// <summary>
        /// The offset of this element's data in the stream
        /// </summary>
        public long DataOffset { get; protected internal set; }
        /// <summary>
        /// The size of this element's header. May be 0 if a header has not been created yet.
        /// </summary>
        public long HeaderSize { get; protected internal set; }
        /// <summary>
        /// The index of this element in its parent
        /// </summary>
        public int Index { get; protected internal set; }
        /// <summary>
        /// The index of this element type in its parent
        /// </summary>
        public int TypeIndex { get; protected internal set; }
        /// <summary>
        /// If this element exists
        /// </summary>
        public bool Exists { get; protected internal set; }
        /// <summary>
        /// Element depth where<br/>
        /// -1 = Document<br/>
        ///  0 = Root<br/>
        ///  1 = Top Level
        /// </summary>
        public int Depth { get; protected internal set; }
        /// <summary>
        /// Stream patch id
        /// </summary>
        public PatchStream Stream { get; protected internal set; }
        /// <summary>
        /// Create an instance
        /// </summary>
        //public ElementStreamInfo(PatchStream stream, long offset, int index, int typeIndex, ElementStreamInfo parent, SchemaElement schemaElement)
        //{
        //    Stream = stream;
        //    Index = index;
        //    TypeIndex = typeIndex;
        //    Parent = parent;
        //    SchemaElement = schemaElement;
        //    Name = schemaElement.Name;
        //}
        ///// <summary>
        ///// Create an instance
        ///// </summary>
        //public ElementStreamInfo(PatchStream stream, long offset, int index, int typeIndex, ElementStreamInfo parent, ulong id)
        //{
        //    Stream = stream;
        //    Index = index;
        //    TypeIndex = typeIndex;
        //    Parent = parent;
        //    SchemaElement = null;
        //    Name = EBMLConverter.ElementIdToHexId(id);
        //}
        ///// <summary>
        ///// Create an instance
        ///// </summary>
        //public ElementStreamInfo(PatchStream stream, long offset, int index, int typeIndex, ElementStreamInfo parent, ulong id)
        //{
        //    Stream = stream;
        //    Index = index;
        //    TypeIndex = typeIndex;
        //    Parent = parent;
        //    SchemaElement = null;
        //    Name = EBMLConverter.ElementIdToHexId(id);
        //}
        //public ElementStreamInfo() { }
        /// <summary>
        /// Element's parent's instance path.
        /// If set directly, the stored value is returned; otherwise it falls
        /// back to walking the Parent chain.
        /// </summary>
        public string? ParentInstancePath
        {
            get => _ParentInstancePath ?? Parent?.InstancePath;
            set => _ParentInstancePath = value;
        }
        private string? _ParentInstancePath;
        /// <summary>
        /// Element's path.
        /// If set directly, the stored value is returned; otherwise it falls
        /// back to walking the Parent chain.
        /// </summary>
        public string Path
        {
            get => _Path ?? (Parent != null ? $"{Parent.Path}/{Name}" : Name);
            set => _Path = value;
        }
        private string? _Path;
        /// <summary>
        /// Element's instance path.
        /// The iterator assigns this directly when producing an element;
        /// if unset, it falls back to walking the Parent chain so externally
        /// constructed instances without a parent still expose a usable
        /// InstancePath (= InstanceName).
        /// </summary>
        public string InstancePath
        {
            get => _InstancePath ?? (Parent != null ? $"{Parent.InstancePath}/{InstanceName}" : InstanceName);
            set => _InstancePath = value;
        }
        private string? _InstancePath;
        /// <summary>
        /// True if this element is a document
        /// </summary>
        public bool IsDocument => Name == "";
        /// <summary>
        /// Element's instance name
        /// </summary>
        public string InstanceName => $"{Name},{TypeIndex}";
        /// <summary>
        /// The starting position of the root element
        /// </summary>
        public long RootOffset => FirstAncestor?.Offset ?? Offset;
        /// <summary>
        /// All ancestor elements
        /// </summary>
        public List<ElementStreamInfo> Ancestors
        {
            get
            {
                var ret = new List<ElementStreamInfo>();
                ElementStreamInfo? el = this;
                while ((el = el.Parent) != null)
                {
                    ret.Add(el);
                }
                return ret;
            }
        }
        /// <summary>
        /// The root element
        /// </summary>
        public ElementStreamInfo FirstAncestor => Ancestors.FirstOrDefault() ?? this;
        /// <summary>
        /// Stream patch id
        /// </summary>
        public string PatchId => Stream.PatchId;
    }
}
