namespace SpawnDev.EBML
{
    public class EBMLEntry<T> where T : struct
    {
        public long HeadOffset { get; init; }
        public T Id { get; init; }
        public long DataOffset { get; init; }
        public ulong DataSize { get; init; }
        public EBMLEntry(long headOffset, T id, long dataOffset, ulong dataSize)
        {
            HeadOffset = headOffset;
            Id = id;
            DataOffset = dataOffset;
            DataSize = dataSize;
        }
    }
    public class EBMLEntry 
    {
        public long HeadOffset { get; init; }
        public ulong Id { get; init; }
        public long DataOffset { get; init; }
        public ulong DataSize { get; init; }
        public EBMLEntry(long headOffset, ulong id, long dataOffset, ulong dataSize)
        {
            HeadOffset = headOffset;
            Id = id;
            DataOffset = dataOffset;
            DataSize = dataSize;
        }
    }
}
