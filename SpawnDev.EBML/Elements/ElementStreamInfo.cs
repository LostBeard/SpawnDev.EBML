using SpawnDev.EBML.Schemas;
using SpawnDev.PatchStreams;

namespace SpawnDev.EBML.Elements
{
    public class ElementStreamInfo
    {
        public string ParentInstancePath { get; protected internal set; }
        public ulong Id { get; protected internal set; }
        public string Path { get; protected internal set; }
        public string InstancePath { get; protected internal set; }
        public SchemaElement? SchemaElement { get; protected internal set; }
        public string Name { get; protected internal set; }
        public int Index { get; protected internal set; }
        public int TypeIndex { get; protected internal set; }
        public long DocumentOffset { get; protected internal set; }
        public long Offset { get; protected internal set; }
        public ulong? Size { get; protected internal set; }
        public long MaxDataSize { get; protected internal set; }
        public long MaxTotalSize { get; protected internal set; }
        public long DataOffset { get; protected internal set; }
        public long HeaderSize { get; protected internal set; }
        public string PatchId { get; protected internal set; }
        public bool Exists{ get; protected internal set; }
        public int Depth { get; protected internal set; }
    }
}
