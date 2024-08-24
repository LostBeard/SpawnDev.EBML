namespace SpawnDev.EBML.Segments
{
    public class StreamSegment : SegmentSource<Stream>
    {
        #region Constructors
        public StreamSegment(Stream source, long offset, long size) : base(source, offset, size)
        {
        }
        public StreamSegment(Stream source, long size) : base(source, source.Position, size)
        {
        }
        public StreamSegment(Stream source) : base(source, source.Position, source.Length - source.Position)
        {
        }
        #endregion
        protected override long SourcePosition { get => Source.Position; set => Source.Position = value; }
        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesLeftInSegment = Length - Position;
            count = (int)Math.Min(count, bytesLeftInSegment);
            if (count <= 0) return 0;
            return Source.Read(buffer, offset, count);
        }
        //public override StreamSegment Slice(long offset, long size, bool? ownsSource = null)
        //{
        //    var slice = new StreamSegment(Source, Offset + offset, size,  OwnsSource)!;
        //    if (slice.Position != 0) slice.Position = 0;
        //    return slice;
        //}
    }
}
