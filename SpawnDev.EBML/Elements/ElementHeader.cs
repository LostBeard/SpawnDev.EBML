using System.Threading;

namespace SpawnDev.EBML.Extensions
{
    /// <summary>
    /// EBML element header
    /// </summary>
    public class ElementHeader
    {
        /// <summary>
        /// Element Id
        /// </summary>
        public ulong Id { get; set; }
        /// <summary>
        /// Element size
        /// </summary>
        public ulong? Size { get; set; }
        /// <summary>
        /// The number of bytes Size occupied when read<br/>
        /// </summary>
        public int SizeLength { get; set; }
        /// <summary>
        /// The minimum number of bytes for Size to occupy when written<br/>
        /// This will default to SizeLength
        /// </summary>
        public int SizeMinLength { get; set; }
        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="id"></param>
        /// <param name="size"></param>
        /// <param name="sizeLength"></param>
        public ElementHeader(ulong id, ulong? size, int sizeLength = 0)
        {
            Id = id;
            Size = size;
            SizeLength = sizeLength;
            SizeMinLength = sizeLength;
        }
        /// <summary>
        /// Copy to a stream
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public int CopyTo(Stream data) => Write(data, Id, Size, SizeMinLength);
        /// <summary>
        /// Copy to a stream
        /// </summary>
        /// <param name="data"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<int> CopyToAsync(Stream data, CancellationToken cancellationToken) => WriteAsync(data, Id, Size, cancellationToken, SizeMinLength);
        /// <summary>
        /// To a byte array
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray() => ToArray(Id, Size, SizeMinLength);
        /// <summary>
        /// Write to a stream
        /// </summary>
        /// <param name="data"></param>
        /// <param name="id"></param>
        /// <param name="size"></param>
        /// <param name="sizeMinLength"></param>
        /// <returns></returns>
        public static int Write(Stream data, ulong id, ulong? size, int sizeMinLength = 1)
        {
            var bytes = ToArray(id, size, sizeMinLength);
            ulong sizeL = size == null ? EBMLConverter.GetUnknownSizeValue(Math.Min(sizeMinLength, 0)) : size.Value;
            var sizeBytes = EBMLConverter.ToVINTBytes(sizeL, sizeMinLength);
            bytes = bytes.Concat(sizeBytes).ToArray();
            data.Write(bytes);
            return bytes.Length;
        }
        /// <summary>
        /// Write to a stream
        /// </summary>
        /// <param name="data"></param>
        /// <param name="id"></param>
        /// <param name="size"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="sizeMinLength"></param>
        /// <returns></returns>
        public static async Task<int> WriteAsync(Stream data, ulong id, ulong? size, CancellationToken cancellationToken, int sizeMinLength = 1)
        {
            var bytes = ToArray(id, size, sizeMinLength);
            ulong sizeL = size == null ? EBMLConverter.GetUnknownSizeValue(Math.Min(sizeMinLength, 0)) : size.Value;
            var sizeBytes = EBMLConverter.ToVINTBytes(sizeL, sizeMinLength);
            bytes = bytes.Concat(sizeBytes).ToArray();
            await data.WriteAsync(bytes, cancellationToken);
            return bytes.Length;
        }
        /// <summary>
        /// To a byte array
        /// </summary>
        /// <param name="id"></param>
        /// <param name="size"></param>
        /// <param name="sizeMinLength"></param>
        /// <returns></returns>
        public static byte[] ToArray(ulong id, ulong? size, int sizeMinLength = 1)
        {
            var bytes = EBMLConverter.ToUIntBytes(id);
            ulong sizeL = size == null ? EBMLConverter.GetUnknownSizeValue(Math.Min(sizeMinLength, 0)) : size.Value;
            var sizeBytes = EBMLConverter.ToVINTBytes(sizeL, sizeMinLength);
            bytes = bytes.Concat(sizeBytes).ToArray();
            return bytes;
        }
        /// <summary>
        /// Read from a stream
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ElementHeader Read(Stream data)
        {
#if false
            // reads bytes as needed. possibly 4 reads. first is 1 byte....
            // chunk read alternative is faster
            var id = data.ReadEBMLElementIdRaw();
            var pos = data.Position;
            var size = data.ReadEBMLElementSizeN();
            var sizeLength = data.Position - pos;
#else
            var start = data.Position;
            var chunkSize = Math.Min(data.Length - start, 12);
            if (chunkSize < 2) throw new Exception("End of stream");
            var chunk = new byte[chunkSize];
            var bytesRead = data.Read(chunk, 0, (int)chunkSize);
            if (bytesRead != chunkSize) throw new Exception("End of stream");
            var headerSize = EBMLConverter.ReadElementHeader(chunk, out var id, out var size, out var sizeLength);
            if (bytesRead == 0) throw new Exception("Invalid data");
            data.Position = start + headerSize;
#endif
            return new ElementHeader(id, size, (int)sizeLength);
        }
        /// <summary>
        /// Read from a stream
        /// </summary>
        /// <param name="data"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<ElementHeader> ReadAsync(Stream data, CancellationToken cancellationToken)
        {
#if false
            // reads bytes as needed. possibly 4 reads. first is 1 byte....
            // chunk read alternative is faster
            var id = await data.ReadEBMLElementIdRawAsync(cancellationToken);
            var pos = data.Position;
            var size = await data.ReadEBMLElementSizeNAsync(cancellationToken);
            var sizeLength = data.Position - pos;
#else
            var start = data.Position;
            var chunkSize = Math.Min(data.Length - start, 12);
            if (chunkSize < 2) throw new Exception("End of stream");
            var chunk = new byte[chunkSize];
            var bytesRead = await data.ReadAsync(chunk, 0, (int)chunkSize, cancellationToken);
            if (bytesRead != chunkSize) throw new Exception("End of stream");
            var headerSize = EBMLConverter.ReadElementHeader(chunk, out var id, out var size, out var sizeLength);
            if (bytesRead == 0) throw new Exception("Invalid data");
            data.Position = start + headerSize;
#endif
            return new ElementHeader(id, size, (int)sizeLength);
        }
        /// <summary>
        /// Read from a stream
        /// </summary>
        /// <param name="data"></param>
        /// <param name="id"></param>
        /// <param name="size"></param>
        /// <param name="sizeLength"></param>
        /// <returns></returns>
        public static int Read(Stream data, out ulong id, out ulong? size, out int sizeLength)
        {
            var start = data.Position;
            id = data.ReadEBMLElementIdRaw();
            var pos = data.Position;
            size = data.ReadEBMLElementSizeN();
            sizeLength = (int)(data.Position - pos);
            return (int)(data.Position - start);
        }
    }
}
