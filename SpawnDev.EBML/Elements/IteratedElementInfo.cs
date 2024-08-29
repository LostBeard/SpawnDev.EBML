namespace SpawnDev.EBML.Elements
{
    /// <summary>
    /// Used internally when parsing a stream
    /// </summary>
    internal class IteratedElementInfo
    {
        public Dictionary<ulong, int> Counts = new Dictionary<ulong, int>();
        public int Seen(ulong id)
        {
            if (!Counts.TryGetValue(id, out var count))
            {
                Counts.Add(id, 0);
            }
            return Counts[id]++;
        }
        public int ChildCount => Counts.Values.Sum(o => o);
        public string Path { get; set; }
        public string InstancePath { get; set; }
        public long MaxDataSize { get; set; }
        public long DataOffset { get; set; }
        public bool UnknownSize { get; set; }
        public int Depth { get; set; }
    }
}
