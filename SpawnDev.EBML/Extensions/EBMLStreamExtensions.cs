using System.Drawing;
using System.IO;
using System.Text;

namespace SpawnDev.EBML.Extensions
{
    internal static partial class EBMLStreamExtensions
    {
        public static MemoryStream CreateEBMLHeader(ulong id, ulong size, int sizeMinOctets = 0)
        {
            var ret = new MemoryStream();
            ret.WriteEBMLElementHeader(id, size, sizeMinOctets);
            ret.Position = 0;
            return ret;
        }
        public static int WriteEBMLElementHeader(this Stream _this, ulong id, ulong size, int sizeMinOctets = 0)
        {
            var ret = _this.WriteEBMLElementIdRaw(id);
            ret += _this.WriteEBMLElementSize(size, sizeMinOctets);
            return ret;
        }
        public static int WriteEBMLElementIdRaw(this Stream _this, ulong id, int minOctets = 0)
        {
            var bytes = EBMLConverter.ToUIntBytes(id, minOctets);
            _this.Write(bytes, 0, bytes.Length);
            return bytes.Length;
        }
        public static int WriteEBMLElementId(this Stream _this, ulong id, int minOctets = 0)
        {
            var bytes = EBMLConverter.ToVINTBytes(id, minOctets);
            _this.Write(bytes, 0, bytes.Length);
            return bytes.Length;
        }
        public static int WriteEBMLElementSize(this Stream _this, ulong? size, int minOctets = 0)
        {
            ulong sizeL = size == null ? EBMLConverter.GetUnknownSizeValue(minOctets) : size.Value;
            var bytes = EBMLConverter.ToVINTBytes(sizeL, minOctets);
            _this.Write(bytes, 0, bytes.Length);
            return bytes.Length;
        }
        /// <summary>
        /// Returns the maximum number of bytes that can be read starting from the given offset<br />
        /// If the offset &gt;= length or offset &lt; 0 or !CanRead, 0 is returned
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static long MaxReadableCount(this Stream _this, long offset)
        {
            if (!_this.CanRead || offset < 0 || offset >= _this.Length || _this.Length == 0) return 0;
            return _this.Length - offset;
        }
        public static long MaxReadableCount(this Stream _this) => _this.MaxReadableCount(_this.Position);
        public static long GetReadableCount(this Stream _this, long maxCount)
        {
            return _this.GetReadableCount(_this.Position, maxCount);
        }
        public static long GetReadableCount(this Stream _this, long offset, long maxCount)
        {
            if (maxCount <= 0) return 0;
            var bytesLeft = _this.MaxReadableCount(offset);
            return Math.Min(bytesLeft, maxCount);
        }
        public static byte[] ReadBytes(this Stream _this)
        {
            var readCount = _this.MaxReadableCount();
            var bytes = new byte[readCount];
            _this.Read(bytes, 0, bytes.Length);
            return bytes;
        }
        public static byte[] ReadBytes(this Stream _this, long count, bool requireCountExact = false)
        {
            var readCount = _this.GetReadableCount(count);
            if (readCount != count && requireCountExact) throw new Exception("Not available");
            var bytes = new byte[readCount];
            if (readCount == 0) return bytes;
            _this.Read(bytes, 0, bytes.Length);
            return bytes;
        }
        public static byte[] ReadBytes(this Stream _this, long offset, long count, bool requireCountExact = false)
        {
            var origPosition = _this.Position;
            _this.Position = offset;
            try
            {
                var readCount = _this.GetReadableCount(offset, count);
                if (readCount != count && requireCountExact) throw new Exception("Not available");
                var bytes = new byte[readCount];
                if (readCount == 0) return bytes;
                _this.Read(bytes, 0, bytes.Length);
                return bytes;
            }
            finally
            {
                _this.Position = origPosition;
            }
        }
        public static int ReadByte(this Stream _this, long offset)
        {
            var origPosition = _this.Position;
            _this.Position = offset;
            try
            {
                var ret = _this.ReadByte();
                return ret;
            }
            finally
            {
                _this.Position = origPosition;
            }
        }
        public static byte ReadByteOrThrow(this Stream _this, long offset)
        {
            var origPosition = _this.Position;
            _this.Position = offset;
            try
            {
                var ret = _this.ReadByte();
                if (ret == -1) throw new EndOfStreamException();
                return (byte)ret;
            }
            finally
            {
                _this.Position = origPosition;
            }
        }
        public static async Task<int> ReadByteAsync(this Stream _this, CancellationToken cancellationToken)
        {
            var oneByteArray = new byte[1];
            int r = await _this.ReadAsync(oneByteArray, 0, 1, cancellationToken);
            return r == 0 ? -1 : oneByteArray[0];
        }
        public static async Task<byte> ReadByteOrThrowAsync(this Stream _this, CancellationToken cancellationToken)
        {
            var ret = await _this.ReadByteAsync(cancellationToken);
            if (ret == -1) throw new EndOfStreamException();
            return (byte)ret;
        }
        public static byte ReadByteOrThrow(this Stream _this)
        {
            var ret = _this.ReadByte();
            if (ret == -1) throw new EndOfStreamException();
            return (byte)ret;
        }

        #region EBML
        private static int GetFirstSetBitIndex(byte value, out byte leftover)
        {
            for (var i = 0; i < 8; i++)
            {
                var v = 1 << 7 - i;
                if ((value & v) != 0)
                {
                    leftover = (byte)(value - v);
                    return i;
                }
            }
            leftover = 0;
            return -1;
        }
        private static int GetFirstSetBitIndex(byte value)
        {
            for (var i = 0; i < 8; i++)
            {
                var v = 1 << 7 - i;
                if ((value & v) != 0) return i;
            }
            return -1;
        }
        public delegate bool ValidElementChildCheckDelegate(ulong[] parentIdChain, ulong childElementId);
        public static ulong DetermineEBMLElementSize(this Stream stream, ulong[] idChain, ValidElementChildCheckDelegate validChildCheck)
        {
            long startOffset = stream.Position;
            long pos = stream.Position;
            while (true)
            {
                pos = stream.Position;
                if (stream.Position >= stream.Length) break;
                var id = stream.ReadEBMLElementId();
                var len = stream.ReadEBMLElementSize(out var isUnknownSize);
                var isAllowedChild = validChildCheck(idChain, id);
                if (!isAllowedChild)
                {
                    break;
                }
                if (isUnknownSize)
                {
                    var childIdChain = idChain.Concat(new ulong[] { id }).ToArray();
                    len = stream.DetermineEBMLElementSize(childIdChain, validChildCheck);
                }
                stream.Seek((long)len, SeekOrigin.Current);
            }
            stream.Position = startOffset;
            return (ulong)(pos - startOffset);
        }
        public static Stream SkipEBMLVINT(this Stream data)
        {
            var firstByte = data.ReadByteOrThrow();
            var bitIndex = GetFirstSetBitIndex(firstByte, out var leftover);
            if (bitIndex > 0) data.Position += bitIndex;
            return data;
        }
        public static ulong ReadEBMLElementId(this Stream data, out bool isInvalid) => data.ReadEBMLVINT(out isInvalid);
        public static ulong ReadEBMLElementId(this Stream data) => data.ReadEBMLVINT(out var isInvalid);
        public static ulong ReadEBMLElementSize(this Stream data, out bool isUnknownSize) => data.ReadEBMLVINT(out isUnknownSize);
        public static ulong? ReadEBMLElementSizeN(this Stream data)
        {
            var ret = data.ReadEBMLElementSize(out var isUnknownSize);
            if (isUnknownSize) return null;
            return ret;
        }
        public static async Task<ulong?> ReadEBMLElementSizeNAsync(this Stream data, CancellationToken cancellationToken)
        {
            var ret = data.ReadEBMLElementSize(out var isUnknownSize);
            if (isUnknownSize) return null;
            return ret;
        }
        public static string ReadEBMLElementIdRawHex(this Stream data)
        {
            var id = data.ReadEBMLElementIdRaw();
            return EBMLConverter.ElementIdToHexId(id);
        }
        public static ElementHeader ReadEBMLElementHeader(this Stream data)
        {
            var id = data.ReadEBMLElementIdRaw();
            var pos = data.Position;
            var size = data.ReadEBMLElementSizeN();
            var sizeLength = data.Position - pos;
            return new ElementHeader(id, size, (int)sizeLength);
        }
        /// <summary>
        /// Reads a vint, but does not discard the size bit
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ulong ReadEBMLElementIdRaw(this Stream data)
        {
            var firstByte = data.ReadByteOrThrow();
            var bitIndex = GetFirstSetBitIndex(firstByte);
            if (bitIndex < 0)
            {
                ////vintDataAllOnes = false;
                // throw?
                return 0; // marker bit must be in first byte (verify correct response to this) 
            }
            var ulongBytes = new byte[8];
            var destIndex = 8 - bitIndex;
            ulongBytes[destIndex - 1] = firstByte;
            if (bitIndex > 0)
            {
                var bytesRead = data.Read(ulongBytes, destIndex, bitIndex);
                if (bitIndex != bytesRead)
                {
                    throw new Exception("End of EBML stream");
                }
            }
            var ret = BigEndian.ToUInt64(ulongBytes);
            return ret;
        }
        public static async Task<ulong> ReadEBMLElementIdRawAsync(this Stream data, CancellationToken cancellationToken)
        {
            var firstByte = await data.ReadByteOrThrowAsync(cancellationToken);
            var bitIndex = GetFirstSetBitIndex(firstByte);
            if (bitIndex < 0)
            {
                ////vintDataAllOnes = false;
                // throw?
                return 0; // marker bit must be in first byte (verify correct response to this) 
            }
            var ulongBytes = new byte[8];
            var destIndex = 8 - bitIndex;
            ulongBytes[destIndex - 1] = firstByte;
            if (bitIndex > 0)
            {
                var bytesRead = await data.ReadAsync(ulongBytes, destIndex, bitIndex, cancellationToken);
                if (bitIndex != bytesRead)
                {
                    throw new Exception("End of EBML stream");
                }
            }
            var ret = BigEndian.ToUInt64(ulongBytes);
            return ret;
        }
        public static ulong ReadEBMLVINT(this Stream data)
        {
            var firstByte = data.ReadByteOrThrow();
            var bitIndex = GetFirstSetBitIndex(firstByte, out var leftover);
            if (bitIndex < 0)
            {
                ////vintDataAllOnes = false;
                // throw?
                return 0; // marker bit must be in first byte (verify correct response to this) 
            }
            var ulongBytes = new byte[8];
            var destIndex = 8 - bitIndex;
            ulongBytes[destIndex - 1] = leftover;
            if (bitIndex > 0) data.Read(ulongBytes, destIndex, bitIndex);
            var ret = BigEndian.ToUInt64(ulongBytes);
            return ret;
        }
        public static ulong ReadEBMLVINT(this Stream data, out bool vintDataAllOnes)
        {
            var firstByte = data.ReadByteOrThrow();
            var bitIndex = GetFirstSetBitIndex(firstByte, out var leftover);
            if (bitIndex < 0)
            {
                vintDataAllOnes = false;
                // throw?
                return 0; // marker bit must be in first byte (verify correct response to this) 
            }
            var ulongBytes = new byte[8];
            var destIndex = 8 - bitIndex;
            ulongBytes[destIndex - 1] = leftover;
            if (bitIndex > 0) data.Read(ulongBytes, destIndex, bitIndex);
            var ret = BigEndian.ToUInt64(ulongBytes);
            vintDataAllOnes = EBMLConverter.IsUnknownSizeVINT(ret, bitIndex + 1);
            return ret;
        }
        public static ulong? ReadEBMLVINTN(this Stream data)
        {
            bool vintDataAllOnes = false;
            var firstByte = data.ReadByteOrThrow();
            var bitIndex = GetFirstSetBitIndex(firstByte, out var leftover);
            if (bitIndex < 0) throw new Exception("Invalid VINT");
            var ulongBytes = new byte[8];
            var destIndex = 8 - bitIndex;
            ulongBytes[destIndex - 1] = leftover;
            if (bitIndex > 0)
            {
                var bytesRead = data.Read(ulongBytes, destIndex, bitIndex);
                if (bytesRead != bitIndex) throw new Exception("End of stream");
            }
            var ret = BigEndian.ToUInt64(ulongBytes);
            vintDataAllOnes = EBMLConverter.IsUnknownSizeVINT(ret, bitIndex + 1);
            return vintDataAllOnes ? null : ret;
        }
        public static async Task<ulong?> ReadEBMLVINTNAsync(this Stream data, CancellationToken cancellationToken)
        {
            bool vintDataAllOnes = false;
            var firstByte = await data.ReadByteOrThrowAsync(cancellationToken);
            var bitIndex = GetFirstSetBitIndex(firstByte, out var leftover);
            if (bitIndex < 0) throw new Exception("Invalid VINT");
            var ulongBytes = new byte[8];
            var destIndex = 8 - bitIndex;
            ulongBytes[destIndex - 1] = leftover;
            if (bitIndex > 0)
            {
                var bytesRead = await data.ReadAsync(ulongBytes, destIndex, bitIndex, cancellationToken);
                if (bytesRead != bitIndex) throw new Exception("End of stream");
            }
            var ret = BigEndian.ToUInt64(ulongBytes);
            vintDataAllOnes = EBMLConverter.IsUnknownSizeVINT(ret, bitIndex + 1);
            return vintDataAllOnes ? null : ret;
        }
        public static ulong ReadEBMLUInt(this Stream stream, int size)
        {
            var bytes = new byte[8];
            var destIndex = 8 - size;
            if (size > 0)
            {
                var cnt = stream.Read(bytes, destIndex, size);
                if (cnt != size) throw new Exception("Not enough data");
            }
            return BigEndian.ToUInt64(bytes);
        }
        public static long ReadEBMLInt(this Stream stream, int size)
        {
            var bytes = new byte[8];
            var destIndex = 8 - size;
            if (size > 0)
            {
                var cnt = stream.Read(bytes, destIndex, size);
                if (cnt != size) throw new Exception("Not enough data");
            }
            return BigEndian.ToInt64(bytes);
        }
        public static double ReadEBMLFloat(this Stream stream, int size)
        {
            if (size == 4)
            {
                return BigEndian.ToSingle(stream.ReadBytes(size, true));
            }
            else if (size == 8)
            {
                return BigEndian.ToDouble(stream.ReadBytes(size, true));
            }
            return 0;
        }
        public static string ReadEBMLString(this Stream stream, int size, Encoding? encoding = null)
        {
            if (encoding == null) encoding = Encoding.UTF8;
            return encoding.GetString(stream.ReadBytes(size, true)).TrimEnd('\0');
        }
        public static string ReadEBMLStringUTF8(this Stream stream, int size) => stream.ReadEBMLString(size, Encoding.UTF8);
        public static string ReadEBMLStringASCII(this Stream stream, int size) => stream.ReadEBMLString(size, Encoding.ASCII);
        private static readonly double TimeScale = 1000000;
        public static readonly DateTime DateTimeReferencePoint = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static DateTime ReadEBMLDate(this Stream stream, int size)
        {
            if (size == 0) return DateTimeReferencePoint;
            var timeOffset = stream.ReadEBMLInt(size);
            return DateTimeReferencePoint + TimeSpan.FromMilliseconds(timeOffset / TimeScale);
        }
        #endregion
    }
}
