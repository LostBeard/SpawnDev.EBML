namespace SpawnDev.EBML.Segments
{
    /// <summary>
    /// A PatchStream is a writable stream that, when written, does not modify the underlying data, but instead creates patches to represent data changes<br/>
    /// This allows
    /// </summary>
    public class PatchStream : Stream
    {
        protected List<Stream> Source { get; } = new List<Stream>();
        /// <summary>
        /// Segment start position in Source
        /// </summary>
        public virtual long Offset { get; protected set; }
        /// <summary>
        /// Segment size in bytes.
        /// </summary>
        protected virtual long Size { get; set; }
        protected virtual long SourcePosition { get => Position + Offset; set => Position = value - Offset; }
        /// <summary>
        /// The length of the available data
        /// </summary>
        public override long Length => Size;
        public override bool CanRead => Source != null;
        public override bool CanSeek => Source != null;
        public override bool CanWrite => true;
        public override bool CanTimeout => false;
        public override long Position { get; set; } = 0;
        public override void Flush()
        {
            // 
        }
        public override void SetLength(long value) => throw new NotImplementedException();
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (Position > Length) throw new Exception("Write past end of file");
            var bytes = buffer.Skip(offset).Take(count).ToArray();
            Splice(Position, count, new MemoryStream(bytes));
            Position += count;
        }
        public PatchStream(IEnumerable<byte[]> source)
        {
            Source.AddRange(source.Select(o => new MemoryStream(o)));
            Size = Source.Sum(o => o.Length);
        }
        public PatchStream(IEnumerable<Stream> source)
        {
            Source.AddRange(source);
            Size = Source.Sum(o => o.Length);
        }
        public PatchStream(Stream source)
        {
            Source.Add(source);
            Size = Source.Sum(o => o.Length);
        }
        public PatchStream(byte[] source)
        {
            Source.Add(new MemoryStream(source));
            Size = Source.Sum(o => o.Length);
        }
        public PatchStream(IEnumerable<byte[]> source, long offset)
        {
            Source.AddRange(source.Select(o => new MemoryStream(o)));
            Offset = offset;
            Size = Source.Sum(o => o.Length) - Offset;
        }
        public PatchStream(IEnumerable<Stream> source, long offset)
        {
            Source.AddRange(source);
            Offset = offset;
            Size = Source.Sum(o => o.Length) - Offset;
        }
        public PatchStream(Stream source, long offset)
        {
            Source.Add(source);
            Offset = offset;
            Size = Source.Sum(o => o.Length) - Offset;
        }
        public PatchStream(byte[] source, long offset)
        {
            Source.Add(new MemoryStream(source));
            Offset = offset;
            Size = Source.Sum(o => o.Length) - Offset;
        }
        public PatchStream(IEnumerable<byte[]> source, long offset, long size)
        {
            Source.AddRange(source.Select(o => new MemoryStream(o)));
            Offset = offset;
            Size = Math.Min(size, Source.Sum(o => o.Length) - Offset);
        }
        public PatchStream(IEnumerable<Stream> source, long offset, long size)
        {
            Source.AddRange(source);
            Offset = offset;
            Size = Math.Min(size, Source.Sum(o => o.Length) - Offset);
        }
        public PatchStream(Stream source, long offset, long size)
        {
            Source.Add(source);
            Offset = offset;
            Size = Math.Min(size, Source.Sum(o => o.Length) - Offset);
        }
        public PatchStream(byte[] source, long offset, long size)
        {
            Source.Add(new MemoryStream(source));
            Offset = offset;
            Size = Math.Min(size, Source.Sum(o => o.Length) - Offset);
        }
        protected void UpdateSource(IEnumerable<Stream> source, long offset, long size)
        {
            var atEndOfStream = Position == Length;
            Source.Clear();
            Source.AddRange(source);
            Offset = offset;
            Size = Math.Min(size, Source.Sum(o => o.Length) - Offset);
            if (atEndOfStream) Position = Length;
            OnChanged?.Invoke();
        }
        public event Action OnChanged;
        protected void UpdateSource(IEnumerable<Stream> source, long offset) => UpdateSource(source, offset, source.Sum(o => o.Length) - offset);
        protected void UpdateSource(IEnumerable<Stream> source) => UpdateSource(source, 0, source.Sum(o => o.Length));
        /// <summary>
        /// Creates an empty SegmentSource
        /// </summary>
        public PatchStream() { }
        /// <summary>
        /// Returns an empty SegmentSource
        /// </summary>
        public static PatchStream Empty => new();
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
        /// <summary>
        /// Returns a new instance of this type with as few of the Source streams needed as possible, to represent the given slice
        /// </summary>
        /// <param name="start"></param>
        /// <param name="size"></param
        /// <returns></returns>
        public virtual PatchStream Slice(long start, long size)
        {
            var streams = new List<Stream>();
            var streamsOffset = Offset + start;
            long coveredSize = 0;
            foreach (var stream in Source)
            {
                var streamLength = stream.Length;
                if (streamsOffset >= streamLength)
                {
                    // skip
                    streamsOffset -= streamLength;
                }
                else if (streamLength > 0)
                {
                    streams.Add(stream);
                    coveredSize += streamLength;
                    if (coveredSize >= size)
                    {
                        break;
                    }
                }
            }
            return new PatchStream(streams, streamsOffset, size);
        }
        /// <summary>
        /// Returns a new instance of this type with the same source, representing a segment of this instance
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public PatchStream Slice(long size) => Slice(Position, size);
        /// <summary>
        /// Returns a new MultiStreamSegment based on this SegmentSource, optionally with data removed, replaced, or inserted as specified<br/>
        /// This streams Position is restored before this method returns
        /// </summary>
        /// <param name="start">The position start start adding the data from addStreams, and the position to repalce the specified amount of data</param>
        /// <param name="replaceLength">The number of bytes to replace. -1 can be used to indicate no data from start on will be used</param>
        /// <param name="addStreams">Streams to add at start position</param>
        /// <returns>A new MultiStreamSegment</returns>
        public virtual PatchStream ToSpliced(long start, long replaceLength, params Stream[] addStreams)
        {
            long pos = 0;
            var streams = new List<Stream>();
            if (start > 0)
            {
                var preSlice = Slice(0, start);
                streams.Add(preSlice);
                if (replaceLength < 0)
                {
                    pos = Length;
                }
            }
            pos = start + replaceLength;
            // add insert streams if any
            if (addStreams != null) streams.AddRange(addStreams);
            // add anything left in this source
            if (pos < Length)
            {
                streams.Add(Slice(pos, Length - pos));
            }
            return new PatchStream(streams);
        }
        // Insert sequential 
        public virtual long Insert(Stream data, long replaceLength = 0)
        {
            var dataSize = data.Length;
            Splice(Position, replaceLength, data);
            Position += dataSize;
            return dataSize;
        }
        public virtual long Insert(byte[] data, long replaceLength = 0) => Insert(new MemoryStream(data), replaceLength);
        public virtual long Insert(IEnumerable<Stream> streams, long replaceLength = 0)
        {
            var data = streams.ToArray();
            var dataSize = data.Sum(o => o.Length);
            Splice(Position, replaceLength, data);
            Position += dataSize;
            return dataSize;
        }
        public virtual long Insert(IEnumerable<byte[]> data, long replaceLength = 0) => Insert(data.Select(o => new MemoryStream(o)), replaceLength);
        // Insert random access
        public virtual long Insert(long start, Stream data, long replaceLength = 0)
        {
            var dataSize = data.Length;
            Splice(start, replaceLength, data);
            return dataSize;
        }
        public virtual long Insert(long start, byte[] data, long replaceLength = 0) => Insert(start, new MemoryStream(data), replaceLength);
        public virtual long Insert(long start, IEnumerable<Stream> streams, long replaceLength = 0)
        {
            var data = streams.ToArray();
            var dataSize = data.Sum(o => o.Length);
            Splice(start, replaceLength, data);
            return dataSize;
        }
        public virtual long Insert(long start, IEnumerable<byte[]> data, long replaceLength = 0) => Insert(start, data.Select(o => new MemoryStream(o)), replaceLength);
        // Splice
        public virtual long Splice(long start, long replaceLength, params byte[][] addBytes) => Splice(start, replaceLength, addBytes.Select(o => new MemoryStream(o)).ToArray());
        public virtual long Splice(long start, long replaceLength, params Stream[] addStreams)
        {
            long bytesWritten = 0;
            long pos = 0;
            var streams = new List<Stream>();
            if (start > 0)
            {
                var preSlice = Slice(0, start);
                streams.Add(preSlice);
                if (replaceLength < 0)
                {
                    pos = Length;
                }
            }
            pos = start + replaceLength;
            // add insert streams if any
            if (addStreams != null)
            {
                streams.AddRange(addStreams);
                bytesWritten = addStreams.Sum(o => o.Length);
            }
            // add anything left in this source
            if (pos < Length)
            {
                streams.Add(Slice(pos, Length - pos));
            }
            UpdateSource(streams);
            return bytesWritten;
        }
        /// <summary>
        /// Read data from the underlying source
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var bytesLeftInSegment = Length - Position;
            count = (int)Math.Min(count, bytesLeftInSegment);
            if (count <= 0) return 0;
            var sourceIndex = 0;
            var source = Source[sourceIndex];
            var currentOffset = SourcePosition;
            while (source.Length < currentOffset)
            {
                if (sourceIndex >= source.Length - 1) return 0;
                sourceIndex++;
                currentOffset = currentOffset - source.Length;
                source = Source[sourceIndex];
            }
            int bytesRead = 0;
            int bytesLeft = count;
            var bytesReadTotal = 0;
            var positions = Source.Select(o => o.Position).ToArray();
            source.Position = currentOffset;
            while (sourceIndex < Source.Count && bytesLeft > 0)
            {
                var sourceBytesLeft = source.Length - source.Position;
                while (sourceBytesLeft <= 0)
                {
                    if (sourceIndex >= Source.Count - 1) goto LoopEnd;
                    sourceIndex++;
                    source = Source[sourceIndex];
                    source.Position = 0;
                    sourceBytesLeft = source.Length;
                }
                var readByteCount = (int)Math.Min(bytesLeft, sourceBytesLeft);
                bytesRead = await source.ReadAsync(buffer, bytesReadTotal + offset, readByteCount, cancellationToken);
                bytesReadTotal += (int)bytesRead;
                bytesLeft -= bytesRead;
                if (bytesRead <= 0 || bytesLeft <= 0) break;
            }
        LoopEnd:
            SourcePosition += bytesReadTotal;
            // restore stream positions
            for (var i = 0; i < Source.Count; i++)
            {
                Source[i].Position = positions[i];
            }
            return bytesReadTotal;
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesLeftInSegment = Length - Position;
            count = (int)Math.Min(count, bytesLeftInSegment);
            if (count <= 0) return 0;
            var sourceIndex = 0;
            var source = Source[sourceIndex];
            var currentOffset = SourcePosition;
            while (source.Length < currentOffset)
            {
                if (sourceIndex >= source.Length - 1) return 0;
                sourceIndex++;
                currentOffset = currentOffset - source.Length;
                source = Source[sourceIndex];
            }
            int bytesRead = 0;
            int bytesLeft = count;
            var bytesReadTotal = 0;
            var positions = Source.Select(o => o.Position).ToArray();
            source.Position = currentOffset;
            while (sourceIndex < Source.Count && bytesLeft > 0)
            {
                var sourceBytesLeft = source.Length - source.Position;
                while (sourceBytesLeft <= 0)
                {
                    if (sourceIndex >= Source.Count - 1) goto LoopEnd;
                    sourceIndex++;
                    source = Source[sourceIndex];
                    source.Position = 0;
                    sourceBytesLeft = source.Length;
                }
                var readByteCount = (int)Math.Min(bytesLeft, sourceBytesLeft);
                bytesRead = source.Read(buffer, bytesReadTotal + offset, readByteCount);
                bytesReadTotal += (int)bytesRead;
                bytesLeft -= bytesRead;
                if (bytesRead <= 0 || bytesLeft <= 0) break;
            }
        LoopEnd:
            SourcePosition += bytesReadTotal;
            // restore stream positions
            for (var i = 0; i < Source.Count; i++)
            {
                Source[i].Position = positions[i];
            }
            return bytesReadTotal;
        }
        public byte[] ToArray()
        {
            var ret = new byte[Length - Position];
            _ = Read(ret, 0, ret.Length);
            return ret;
        }
    }
}