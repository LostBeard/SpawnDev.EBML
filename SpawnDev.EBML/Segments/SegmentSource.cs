namespace SpawnDev.EBML.Segments
{
    public abstract class SegmentSource : Stream
    {
        /// <summary>
        /// The underlying source of the segment
        /// </summary>
        public virtual object SourceObject { get; protected set; }
        /// <summary>
        /// Segment start position in Source
        /// </summary>
        public virtual long Offset { get; init; }
        /// <summary>
        /// Segment size in bytes.
        /// </summary>
        protected virtual long Size { get; init; }
        protected virtual long SourcePosition { get; set; } = 0;
        // Stream
        public override long Length => Size;
        public override bool CanRead => SourceObject != null;
        public override bool CanSeek => SourceObject != null;
        public override bool CanWrite => false;
        public override bool CanTimeout => false;
        public override long Position { get => SourcePosition - Offset; set => SourcePosition = value + Offset; }
        public override void Flush() { }
        public override void SetLength(long value) => throw new NotImplementedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        public SegmentSource(long offset, long size)
        {
            Offset = offset;
            Size = size;
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.End:
                    Position = Length + offset;
                    break;
                case SeekOrigin.Current:
                    Position = Position + offset;
                    break;
            }
            return Position;
        }
        public virtual void Splice(long start, long replaceLength, params Stream[] addStreams)
        {
            var pos = Position;
            var streams = new List<Stream>();
            if (start > 0)
            {
                streams.Add(Slice(0, start));
                if (replaceLength < 0)
                {
                    Position = Length;
                }
                else
                {
                    Position = start + replaceLength;
                }
            }
            if (addStreams != null) streams.AddRange(addStreams);
            if (Position < Length)
            {
                streams.Add(Slice(Position, Length - Position));
            }
            Position = pos;
            //return new MultiStreamSegment(streams);
        }
        /// <summary>
        /// Returns a new instance of this type with the same source, representing a segment of this instance
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <param name="ownsSource">This instance's OwnsSource is used if true or false are not supplied</param>
        /// <returns></returns>
        public virtual SegmentSource Slice(long offset, long size)
        {
            var slice = (SegmentSource)Activator.CreateInstance(GetType(), SourceObject, Offset + offset, size)!;
            if (slice.Position != 0) slice.Position = 0;
            return slice;
        }
        /// <summary>
        /// Returns a new MultiStreamSegment based on this SegmentSource, optionally with data removed, replaced, or inserted as specified<br/>
        /// This streams Position is restored before this method returns
        /// </summary>
        /// <param name="start">The position start start adding the data from addStreams</param>
        /// <param name="replaceLength">The number of bytes to replace. -1 can be used to indicate no data from start on will be used</param>
        /// <param name="addStreams">Streams to add at start position</param>
        /// <returns>A new MultiStreamSegment</returns>
        public virtual MultiStreamSegment ToSpliced(long start, long replaceLength, params Stream[] addStreams)
        {
            var pos = Position;
            var streams = new List<Stream>();
            if (start > 0)
            {
                streams.Add(Slice(0, start));
                if (replaceLength < 0)
                {
                    Position = Length;
                }
                else
                {
                    Position = start + replaceLength;
                }
            }
            if (addStreams != null) streams.AddRange(addStreams);
            if (Position < Length)
            {
                streams.Add(Slice(Position, Length - Position));
            }
            Position = pos;
            return new MultiStreamSegment(streams);
        }
        /// <summary>
        /// Returns a new MultiStreamSegment based on this SegmentSource, optionally with data removed, replaced, or inserted as specified
        /// </summary>
        /// <param name="start"></param>
        /// <param name="addStreams"></param>
        /// <returns></returns>
        public virtual MultiStreamSegment ToSpliced(long start, params Stream[] addStreams) => ToSpliced(start, 0, addStreams);

        public SegmentSource Slice(long size) => Slice(Position, size);
        public override void CopyTo(Stream destination, int bufferSize)
        {
            base.CopyTo(destination, bufferSize);
        }
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return base.CopyToAsync(destination, bufferSize, cancellationToken);
        }
    }
    public abstract class SegmentSource<T> : SegmentSource
    {
        public T Source { get; private set; }
        public SegmentSource(T source, long offset, long size) : base(offset, size)
        {
            Source = source;
            SourceObject = source!;
        }
    }
}
