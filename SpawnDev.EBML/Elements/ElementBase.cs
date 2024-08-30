using SpawnDev.EBML.Crc32;
using SpawnDev.EBML.Extensions;
using SpawnDev.EBML.Schemas;
using SpawnDev.PatchStreams;
using System.Text;

namespace SpawnDev.EBML.Elements
{
    /// <summary>
    /// An EBML element
    /// </summary>
    public class ElementBase
    {
        /// <summary>
        /// Instance stream info
        /// </summary>
        public virtual ElementStreamInfo Info
        {
            get
            {
                if (!DocumentRoot)
                {
                    var info = Document.FindInfo(_Info.InstancePath).FirstOrDefault();
                    if (info != null)
                    {
                        _Info.Exists = true;    
                        if (_Info != info) _Info = info;
                    }
                    else
                    {
                        _Info.Exists = false;
                    }
                }
                return _Info;
            }
            protected set => _Info = value;
        }
        /// <summary>
        /// Recast this element to another Element type<br/>
        /// IF this element is already assignable to TElement, this element is returned<br/>
        /// TElement must have a constructor: (StreamElementInfo info) : base(info)
        /// </summary>
        /// <typeparam name="TElement">Element type to return</typeparam>
        /// <returns></returns>
        public TElement As<TElement>() where TElement : ElementBase
        {
            if (this is TElement element) return (TElement)element;
            var ret = (TElement)Activator.CreateInstance(typeof(TElement), Document, Info)!;
            return ret;
        }
        /// <summary>
        /// Throws an exception if can edit returns false
        /// </summary>
        /// <exception cref="Exception"></exception>
        protected void ThrowIfCannotEdit()
        {
            if (!CanEdit) throw new Exception("Cannot edit element in current state");
        }
        /// <summary>
        /// Returns true if the current element stream info is update to date with the document primary stream<br/>
        /// If true, this element's data is available for editing
        /// </summary>
        public bool CanEdit
        {
            get
            {
                return Stream != null && PatchId == Document.Stream.PatchId && Document.Stream.RestorePoint;
            }
        }
        /// <summary>
        /// Returns true if an update is needed and not already updating
        /// </summary>
        public bool CanUpdate
        {
            get
            {
                return !UsingLatestStable && !Updating;
            }
        }
        /// <summary>
        /// Returns true if this element is using data from the current primary stream patch<br/>
        /// An element will only ever use the latest patch if it is marked as stable (a restore point)
        /// </summary>
        public bool UsingLatest
        {
            get
            {
                return PatchId == Document.Stream.PatchId;
            }
        }
        /// <summary>
        /// Returns true if the latest stream patch is marked as a restore point, also referred to in this library as a stable patch
        /// </summary>
        public bool LatestIsStable => Document.Stream.RestorePoint;
        /// <summary>
        /// Returns true if this element is using data from the latest (relative to the current) stable stream patch<br/>
        /// The latest patch may not be marked as stable as modifications may be in process that would cause the stream to appear corrupted.<br/>
        /// So elements generally only look at the latest stable stream patch
        /// </summary>
        public bool UsingLatestStable
        {
            get
            {
                return PatchId == Document.Stream.LatestStable.PatchId;
            }
        }
        /// <summary>
        /// Returns true if updating elements stream info
        /// </summary>
        public bool Updating { get; protected set; } = false;
        /// <summary>
        /// The Document this element belong(s|ed) to
        /// </summary>
        public virtual EBMLDocument Document { get; protected set; }
        /// <summary>
        /// EBML schema parser
        /// </summary>
        public virtual EBMLParser Parser => Document.Parser;
        /// <summary>
        /// The source stream. The data in this stream may change. If it does Stream.PatchId will no longer match PatchId. UpdateNeeded will == true.<br/>
        /// StreamSnapShot will still contain the data before Stream was changed, if it is needed. Calling Update() will update this element's metadata and SnapShot
        /// </summary>
        public virtual PatchStream Stream => Document.Stream[PatchId];
        /// This element's index in its container
        /// </summary>
        public int Index => Info.Index;
        /// <summary>
        /// This element's index by type in its container
        /// </summary>
        public int TypeIndex => Info.TypeIndex;
        /// <summary>
        /// The element's Id
        /// </summary>
        public ulong Id => Info.Id;
        /// <summary>
        /// The element's hex id
        /// </summary>
        public string HexId => EBMLConverter.ElementIdToHexId(Id);
        /// <summary>
        /// The element's name
        /// </summary>
        public string Name => Info.Name ?? "";
        /// <summary>
        /// The element's path
        /// </summary>
        public string Path => Info.Path;
        /// <summary>
        /// The element's instance path
        /// </summary>
        public string InstancePath => Info.InstancePath;
        /// <summary>
        /// The element's EBML schema
        /// </summary>
        public SchemaElement? SchemaElement => Info.SchemaElement;
        /// <summary>
        /// Returns true is a snap shot will be used for reads. Update() to resync.
        /// </summary>
        /// <summary>
        /// The position in the stream where the EBML document containing this element starts
        /// </summary>
        public virtual long DocumentOffset => Info.DocumentOffset;
        /// <summary>
        /// The position in the stream of this element
        /// </summary>
        public long Offset => Info.Offset;
        /// <summary>
        /// The position of the byte following this element
        /// </summary>
        public long EndPos => Offset + TotalSize;
        /// <summary>
        /// The size of this element, if specified by header
        /// </summary>
        public ulong? Size => DocumentRoot ? (ulong)Stream.LatestStable.Length - (ulong)DocumentOffset : Info.Size;
        /// <summary>
        /// The size of the element's header
        /// </summary>
        public long HeaderSize => Info.HeaderSize;
        /// <summary>
        /// The size of this element, if specified by header, else the size of data left in the stream
        /// </summary>
        public long DataSize => DocumentRoot ? Stream.LatestStable.Length - DocumentOffset : Info.DataSize;
        /// <summary>
        /// The total size of this element. Header size + data size.
        /// </summary>
        public long TotalSize => DocumentRoot ? Stream.LatestStable.Length - DocumentOffset : Info.TotalSize;
        /// <summary>
        /// The position in the stream where this element's data starts
        /// </summary>
        public long DataOffset => Info.DataOffset;
        /// <summary>
        /// The patch id of the PatchStream when this element's metadata was last updated
        /// </summary>
        public string PatchId => Info.PatchId;
        /// <summary>
        /// Returns true if this element's InstancePath is still found in the containing EBML document or if this element is an EBML document master element<br/>
        /// </summary>
        public bool Exists => Info.Exists;
        /// <summary>
        /// Returns true if this element has an empty name and Offset == DocumentOffset
        /// </summary>
        public bool DocumentRoot => this is EBMLDocument;
        /// <summary>
        /// The number of elements if this elements path - 1
        /// </summary>
        public int Depth => Info.Depth;
        /// <summary>
        /// A Root Element is a mandatory, nonrepeating EBML Element that occurs at the top level of the path hierarchy within an EBML Body and contains all other EBML Elements of the EBML Body, excepting optional Void Elements.
        /// </summary>
        public bool Root => Depth == 0;
        /// <summary>
        /// A Top-Level Element is an EBML Element defined to only occur as a Child Element of the Root Element.
        /// </summary>
        public bool TopLevel => Depth == 1;
        private ElementStreamInfo _Info = new ElementStreamInfo();
        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="element"></param>
        public ElementBase(EBMLDocument document, ElementStreamInfo element)
        {
            //Console.WriteLine($"** {GetType().Name}");
            if (element == null) throw new ArgumentNullException(nameof(element));
            Document = document;
            Info = element;
        }
        public static bool Verbose { get; set; } = false;
        /// <summary>
        /// Constructor for derived classes
        /// </summary>
        protected ElementBase()
        {
            //Console.WriteLine($"** {GetType().Name}");
        }

        /// <summary>
        /// Removes the element from the Document<br/>
        /// The Element.Exists
        /// </summary>
        /// <returns></returns>
        public bool Remove()
        {
            ThrowIfCannotEdit();
            if (!Exists || Size == null) return false;
            DataChanged(this, -1);
            return true;
        }
        public List<MasterElement> GetAncestors(bool removeRoot = false)
        {
            var paths = GetAncestorInstancePaths(removeRoot);
            if (removeRoot && paths.FirstOrDefault() == "/") paths.RemoveAt(0);
            var ret = paths.Select(o => Document.FindMaster(o)).ToList();
            return ret;
        }
        public List<string> GetAncestorInstancePaths(bool removeRoot = false)
        {
            var ret = new List<string>();
            var path = EBMLConverter.PathParent(InstancePath);
            while (!string.IsNullOrEmpty(path))
            {
                if (removeRoot && path == "/") break;
                ret.Add(path);
                path = EBMLConverter.PathParent(path);
                if (ret.Count > 5)
                {
                    var ggg = true;
                }
            }
            return ret;
        }
        protected internal virtual void AfterAdded() { }
        /// <summary>
        /// Returns the element this element is contained in
        /// </summary>
        public MasterElement? Parent => DocumentRoot ? null : Root ? Document : Document.FindMaster(ParentInstancePath!);
        /// <summary>
        /// Returns this elements parent instance path
        /// </summary>
        public string? ParentInstancePath => DocumentRoot || Root ? null : Info.ParentInstancePath;
        /// <summary>
        /// This method should be called when this element's data has changed so the header data size information for all parent nodes can be updated
        /// </summary>
        /// <param name="changedElement">The element that is calling the event</param>
        /// <param name="newDataSize">The number of bytes added or removed from this element's data. May be 0, but this needs to be called if the element's data has changed</param>
        protected internal virtual void DataChanged(ElementBase changedElement, long newDataSize)
        {
            Document.DataChanged(changedElement, newDataSize);
        }
        /// <summary>
        /// Returns the DocType
        /// </summary>
        public virtual string? DocType { get => SchemaElement?.DocType ?? Document.DocType; set { } }
        /// <summary>
        /// Returns the entire element, header and data, as a PatchStream
        /// </summary>
        /// <returns></returns>
        public PatchStream ElementStreamSlice()
        {
            return Stream.Slice(Offset, TotalSize);
        }
        /// <summary>
        /// Returns the element data as a PatchStream
        /// </summary>
        /// <returns></returns>
        public PatchStream ElementStreamDataSlice()
        {
            return Stream.Slice(DataOffset, DataSize);
        }
        public virtual void CopyTo(Stream stream)
        {
            Stream.Position = Offset;
            Stream.CopyTo(stream, (int)TotalSize);
        }
        public virtual async Task CopyToAsync(Stream stream, CancellationToken cancellationToken)
        {
            Stream.Position = Offset;
            await Stream.CopyToAsync(stream, (int)TotalSize, cancellationToken);
        }
        /// <summary>
        /// Replace this element's data with the specified stream
        /// </summary>
        /// <param name="replacementData"></param>
        public void ReplaceData(byte[] replacementData)
        {
            ReplaceData(new MemoryStream(replacementData));
        }
        /// <summary>
        /// The element type
        /// </summary>
        public string Type => DocumentRoot ? "document" : SchemaElement?.Type ?? "";
        /// <summary>
        /// A string that represents this element
        /// </summary>
        public virtual string DataString
        {
            get
            {
                return $"{InstancePath}";
            }
            set
            {

            }
        }
        /// <summary>
        /// Replace this element's data with the specified stream
        /// </summary>
        /// <param name="replacementData"></param>
        public void ReplaceData(Stream replacementData)
        {
            ThrowIfCannotEdit();
            Stream.Position = DataOffset;
            Stream.Insert(replacementData, DataSize);
            DataChanged(this, replacementData.Length);
        }
        /// <summary>
        /// Deletes the elements data<br/>
        /// The element still exist with a data size of 0
        /// </summary>
        public void DeleteData()
        {
            ThrowIfCannotEdit();
            Stream.Position = DataOffset;
            Stream.Delete(DataSize);
            DataChanged(this, 0);
        }
        #region Equals
        public static bool operator ==(ElementBase? b1, ElementBase? b2)
        {
            if ((object?)b1 == null) return (object?)b2 == null;
            return b1.Equals(b2);
        }

        public static bool operator !=(ElementBase? b1, ElementBase? b2)
        {
            return !(b1 == b2);
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || !(obj is ElementBase element)) return false;
            // if the instance path is different or the Document is different ? false
            if (InstancePath != element.InstancePath || !object.ReferenceEquals(Document, element.Document)) return false;
            return true;
        }

        protected static Crc32Algorithm CRC = new Crc32Algorithm(false);
        public override int GetHashCode()
        {
            var bytes = Encoding.UTF8.GetBytes(InstancePath);
            var crc = CRC.ComputeHash(bytes);
            var hashCode = BitConverter.ToInt32(crc);
            return hashCode;
        }
        #endregion
    }
}
