
using System.Buffers;

namespace SpawnDev.EBML.Segments
{
    public class MultiStreamSegment : SegmentSource
    {
        public List<Stream> Source { get; } = new List<Stream>();
        #region Constructors
        public override long Length => Source.Sum(x => x.Length);
        public MultiStreamSegment(IEnumerable<byte[]> source) : base(0, source.Sum(o => o.Length))
        {
            Source.AddRange(source.Select(o => new MemoryStream(o)));
            SourceObject = source;
        }
        public MultiStreamSegment(IEnumerable<Stream> source) : base(0, source.Sum(o => o.Length))
        {
            Source.AddRange(source);
            SourceObject = source;
        }
        public MultiStreamSegment(Stream source) : base(0, source.Length)
        {
            Source.Add(source);
            SourceObject = source;
        }
        public MultiStreamSegment(byte[] source) : base(0, source.Length)
        {
            Source.Add(new MemoryStream(source));
            SourceObject = source;
        }
        //
        public MultiStreamSegment(IEnumerable<byte[]> source, long offset) : base(offset, source.Sum(o => o.Length) - offset)
        {
            Source.AddRange(source.Select(o => new MemoryStream(o)));
            SourceObject = source;
        }
        public MultiStreamSegment(IEnumerable<Stream> source, long offset) : base(0, source.Sum(o => o.Length) - offset)
        {
            Source.AddRange(source);
            SourceObject = source;
        }
        public MultiStreamSegment(Stream source, long offset) : base(0, source.Length - offset)
        {
            Source.Add(source);
            SourceObject = source;
        }
        public MultiStreamSegment(byte[] source, long offset) : base(0, source.Length - offset)
        {
            Source.Add(new MemoryStream(source));
            SourceObject = source;
        }
        //
        //
        public MultiStreamSegment(IEnumerable<byte[]> source, long offset, long size) : base(offset, Math.Min(size, source.Sum(o => o.Length) - offset))
        {
            Source.AddRange(source.Select(o => new MemoryStream(o)));
            SourceObject = source;
        }
        public MultiStreamSegment(IEnumerable<Stream> source, long offset, long size) : base(0, Math.Min(size, source.Sum(o => o.Length) - offset))
        {
            Source.AddRange(source);
            SourceObject = source;
        }
        public MultiStreamSegment(Stream source, long offset, long size) : base(0, source.Length - offset)
        {
            Source.Add(source);
            SourceObject = source;
        }
        public MultiStreamSegment(byte[] source, long offset, long size) : base(0, source.Length - offset)
        {
            Source.Add(new MemoryStream(source));
            SourceObject = source;
        }
        //
        public MultiStreamSegment() : base(0, 0)
        {

        }
        public virtual SegmentSource Slice(long offset, long size)
        {
            var slice = (SegmentSource)Activator.CreateInstance(GetType(), SourceObject, Offset + offset, size)!;
            if (slice.Position != 0) slice.Position = 0;
            return slice;
        }
        #endregion
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var bytesLeftInSegment = Length - Position;
            count = (int)Math.Min(count, bytesLeftInSegment);
            if (count <= 0) return 0;
            var sourceIndex = 0;
            var source = Source[sourceIndex];
            var currentOffset = Position;
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
            Position += bytesReadTotal;
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
            var currentOffset = Position;
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
            Position += bytesReadTotal;
            // restore stream positions
            for (var i = 0; i < Source.Count; i++)
            {
                Source[i].Position = positions[i];
            }
            return bytesReadTotal;
        }
    }
}
