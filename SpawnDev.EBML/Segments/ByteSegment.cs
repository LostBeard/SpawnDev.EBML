
namespace SpawnDev.EBML.Segments
{
    public class ByteSegment : SegmentSource<byte[]>
    {
        #region Constructors
        /// <summary>
        /// Creates a new ByteSegment
        /// </summary>
        public ByteSegment(byte[] source, long offset, long size) : base(source, offset, size) { }
        /// <summary>
        /// Creates a new ByteSegment
        /// </summary>
        public ByteSegment(byte[] source, long size) : base(source, 0, size) { }
        /// <summary>
        /// Creates a new ByteSegment
        /// </summary>
        public ByteSegment(byte[] source) : base(source, 0, source.Length) { }
        #endregion
        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesLeftInSegment = Length - Position;
            count = (int)Math.Min(count, bytesLeftInSegment);
            if (count <= 0) return 0;
            Array.Copy(Source, SourcePosition, buffer, offset, count);
            SourcePosition += count;
            return count;
        }
        public ByteSegment() : base(new byte[0], 0, 0) { }
        public static ByteSegment Empty => new ByteSegment();

        //public override ByteSegment Slice(long offset, long size)
        //{
        //    var slice = new ByteSegment(Source, Offset + offset, size, OwnsSource)!;
        //    if (slice.Position != 0) slice.Position = 0;
        //    return slice;
        //}
    }
}
