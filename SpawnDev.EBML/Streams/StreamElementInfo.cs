using SpawnDev.PatchStreams;

namespace SpawnDev.EBML.Streams
{
    public class StreamElementInfo
    {
        public EBMLParser EBMLParser { get; init; }
        public ulong Id { get; init; }
        public string Path { get; init; }
        public long DocumentOffset { get; init; }
        public string InstancePath { get; init; }
        public SchemaElement SchemaElement { get; init; }
        public PatchStream Stream { get; init; }
        // 
        public int Index { get; internal set; }
        public long Offset { get; internal set; }
        public ulong? Size { get; internal set; }
        public long MaxDataSize { get; internal set; }
        public long MaxTotalSize { get; internal set; }
        public long DataOffset { get; internal set; }
        public string PatchId { get; internal set; }
        public bool Exists { get; internal set; }
    }
}
