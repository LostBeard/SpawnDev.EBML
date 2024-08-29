using SpawnDev.EBML.Schemas;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SpawnDev.EBML.Extensions
{
    /// <summary>
    /// Converter tools for EBML<br/>
    /// </summary>
    public static class EBMLConverter
    {
        static Regex PathPartRegex = new Regex(@"^(.*?)(?:,([0-9-]*))?$", RegexOptions.Compiled);
        static Regex PathFromInstancePathRegex = new Regex(@",[0-9-]*", RegexOptions.Compiled);
        /// <summary>
        /// Returns a name index pair fro ma name. Used for name matching with Find
        /// </summary>
        /// <param name="nameIn"></param>
        /// <param name="name"></param>
        /// <param name="index"></param>
        /// <param name="defaultIndexAll">If true, and no index is found in [name] then -1 is returned which maxes all indexes</param>
        public static void NameToNameIndex(string nameIn, out string name, out int index, bool defaultIndexAll = true)
        {
            var m = PathPartRegex.Match(nameIn);
            if (m.Success)
            {
                index = m.Groups.Count == 3 && m.Groups[2].Value.Length > 0 ? int.Parse(m.Groups[2].Value) : (defaultIndexAll ? -1 : 0);
                name = m.Groups[1].Value;
            }
            else
            {
                name = nameIn; 
                index = defaultIndexAll ? - 1 : 0;
            }
        }
        public static void PathToParentInstancePathNameIndex(string path, out string parentInstancePath, out string name, out int index, bool defaultIndexAll = true)
        {
            //if (!path.StartsWith("/")) path = $"/{path}";
            var parentPath = PathParent(path);
            parentInstancePath = PathToInstancePath(parentPath);
            var nameMaybeIndex= PathName(path);
            NameToNameIndex(nameMaybeIndex, out name, out index, defaultIndexAll);
        }
        /// <summary>
        /// Removes instance indexes fro mn instance path leaving only the element's non-indexed path
        /// </summary>
        /// <param name="instancePath"></param>
        /// <returns></returns>
        public static string PathFromInstancePath(string instancePath)
        {
            return PathFromInstancePathRegex.Replace(instancePath, "");
        }
        /// <summary>
        /// Returns the element name from the specified path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string PathName(string path)
        {
            return path.Substring(path.LastIndexOf("/") + 1);
        }
        /// <summary>
        /// Returns the path parent
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string PathParent(string path, bool prependDelimiter = true)
        {
            if (string.IsNullOrEmpty(path) || path == "/") return "";
            var pos = path.LastIndexOf("/");
            if (pos < 1) return "/";
            return path.Substring(0, pos);
        }
        /// <summary>
        /// Converts an EBML path 
        /// Ex: (/[ELEMENT_NAME]/[ELEMENT_NAME]/[ELEMENT_NAME]) 
        /// to an EBML instance path 
        /// Ex: (/[ELEMENT_NAME],[INDEX]/[ELEMENT_NAME],[INDEX]/[ELEMENT_NAME],[INDEX])
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string PathToInstancePath(string path, bool lastIndexDefaultsAll = false)
        {
            if (string.IsNullOrEmpty(path) || path == "/") return path;
            var parts = path.Split(EBMLParser.PathDelimiters);
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (part == "" && i == 0) continue;
                var m = PathPartRegex.Match(part);
                if (m.Success)
                {
                    var index = lastIndexDefaultsAll ? -1 : 0;
                    if (m.Groups.Count == 3 && !string.IsNullOrWhiteSpace(m.Groups[2].Value))
                    {
                        index = int.Parse(m.Groups[2].Value);
                        var nmttt = true;
                    }
                    var name = m.Groups[1].Value;
                    parts[i] = $"{name}{EBMLParser.IndexDelimiter}{index}";
                }
                else
                {
                    throw new Exception("Invalid path");
                }
            }
            var ret1 = string.Join(EBMLParser.PathDelimiter, parts);
            return ret1;
        }
        /// <summary>
        /// Returns the specified element id as a hex id string
        /// </summary>
        /// <param name="id"></param>
        /// <param name="prepend0x"></param>
        /// <returns></returns>
        public static string ElementIdToHexId(ulong id, bool prepend0x = true)
        {
            return prepend0x ? $"0x{Convert.ToHexString(EBMLConverter.ToUIntBytes(id))}" : Convert.ToHexString(EBMLConverter.ToUIntBytes(id));
        }
        /// <summary>
        /// Returns the element id from the specified hex id string
        /// </summary>
        /// <param name="hexId"></param>
        /// <returns></returns>
        public static ulong ElementIdFromHexId(string hexId)
        {
            if (hexId == null) return 0;
            if (hexId.StartsWith("0x")) hexId = hexId.Substring(2);
            var idBytes = Convert.FromHexString(hexId).ToList();
            idBytes.Reverse();
            while (idBytes.Count < 8) idBytes.Add(0);
            var id = BitConverter.ToUInt64(idBytes.ToArray());
            return id;
        }
        public static byte[] ToVINTBytes(ulong x, int minOctets = 0)
        {
            int bytes;
            ulong flag;
            for (bytes = 1, flag = 0x80; x >= flag && bytes < 8; bytes++, flag *= 0x80) { }
            if (IsUnknownSizeVINT(x, bytes))
            {
                bytes += 1;
                flag *= 0x80;
            }
            while (bytes < minOctets)
            {
                bytes += 1;
                flag *= 0x80;
            }
            var ret = new byte[bytes];
            var value = flag + x;
            for (var i = bytes - 1; i >= 0; i--)
            {
                var c = value % 256;
                ret[i] = (byte)c;
                value = (value - c) / 256;
            }
            return ret;
        }
        public static int ToVINTByteSize(ulong x, int minOctets = 0)
        {
            int bytes;
            ulong flag;
            for (bytes = 1, flag = 0x80; x >= flag && bytes < 8; bytes++, flag *= 0x80) { }
            if (IsUnknownSizeVINT(x, bytes))
            {
                bytes += 1;
            }
            bytes = Math.Max(minOctets, bytes);
            return bytes;
        }
        public static byte[] ToUIntBytes(ulong value, int minOctets = 0)
        {
            var bytes = BigEndian.GetBytes(value).ToList();
            while (bytes.Count > 1 && bytes[0] == 0 && (minOctets <= 0 || bytes.Count > minOctets)) bytes.RemoveAt(0);
            return bytes.ToArray();
        }
        public static byte[] ToIntBytes(long value, int minOctets = 0)
        {
            var bytes = BigEndian.GetBytes(value).ToList();
            while (bytes.Count > 1 && bytes[0] == 0 && (minOctets <= 0 || bytes.Count > minOctets)) bytes.RemoveAt(0);
            return bytes.ToArray();
        }
        public static byte[] ToFloatBytes(double value)
        {
            return BigEndian.GetBytes(value);
        }
        public static byte[] ToFloatBytes(float value)
        {
            return BigEndian.GetBytes(value);
        }
        private static readonly double TimeScale = 1000000;
        public static readonly DateTime DateTimeReferencePoint = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static byte[] ToDateBytes(DateTime value, int minOctets = 0)
        {
            var timeOffset = (long)((value - DateTimeReferencePoint).TotalMilliseconds * TimeScale);
            return ToIntBytes(timeOffset, minOctets);
        }
        public static int GetFirstSetBitIndex(byte value, out byte leftover)
        {
            for (int i = 0; i < 8; i++)
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
        public static ulong ToElementSize(byte[] data, out int vintSize, out bool isUnknownSize, int index = 0)
        {
            var size = ToVINT(data, out vintSize, index);
            isUnknownSize = IsUnknownSizeVINT(size, vintSize);
            return size;
        }
        public static ulong ToElementId(byte[] data, out int vintSize, out bool isInvalid, int index = 0)
        {
            var size = ToVINT(data, out vintSize, index);
            isInvalid = IsUnknownSizeVINT(size, vintSize);
            return size;
        }
        public static ulong ToElementId(byte[] data, out int vintSize, int index = 0) => ToVINT(data, out vintSize, index);
        public static ulong ToVINT(byte[] data, out int size, int index = 0)
        {
            var firstByte = data[index];
            var bitIndex = GetFirstSetBitIndex(firstByte, out var leftover);
            if (bitIndex < 0)
            {
                size = 0;
                return 0; // marker bit must be in first byte (verify correct response to this)
            }
            var ulongBytes = new byte[8];
            var destIndex = 8 - bitIndex;
            ulongBytes[destIndex - 1] = leftover;
            if (bitIndex > 0) Buffer.BlockCopy(data, index + 1, ulongBytes, destIndex, bitIndex);
            size = bitIndex + 1;
            return BigEndian.ToUInt64(ulongBytes);
        }
        public static ulong ToUInt(byte[] data)
        {
            var index = 0;
            var size = data.Length;
            var bytes = new byte[8];
            var destIndex = 8 - size;
            if (size > 0)
            {
                Buffer.BlockCopy(data, index, bytes, destIndex, size);
            }
            return BigEndian.ToUInt64(bytes);
        }
        public static ulong ToUInt(byte[] data, int size, int index = 0)
        {
            var bytes = new byte[8];
            var destIndex = 8 - size;
            if (size > 0)
            {
                Buffer.BlockCopy(data, index, bytes, destIndex, size);
            }
            return BigEndian.ToUInt64(bytes);
        }
        public static long ToInt(byte[] data, int size, int index = 0)
        {
            var bytes = new byte[8];
            var destIndex = 8 - size;
            if (size > 0)
            {
                Buffer.BlockCopy(data, index, bytes, destIndex, size);
            }
            return BigEndian.ToInt64(bytes);
        }
        public static long ToInt(byte[] data)
        {
            var size = data.Length;
            var bytes = new byte[8];
            var destIndex = 8 - size;
            if (size > 0)
            {
                Buffer.BlockCopy(data, 0, bytes, destIndex, size);
            }
            return BigEndian.ToInt64(bytes);
        }
        public static double ToFloat(byte[] data)
        {
            var size = data.Length;
            if (size == 4)
            {
                return BigEndian.ToSingle(data, 0);
            }
            else if (size == 8)
            {
                return BigEndian.ToDouble(data, 0);
            }
            return 0;
        }
        public static double ToFloat(byte[] data, int size, int index = 0)
        {
            if (size == 4)
            {
                return BigEndian.ToSingle(data, index);
            }
            else if (size == 8)
            {
                return BigEndian.ToDouble(data, index);
            }
            return 0;
        }
        public static string ToString(byte[] data, int count, int index = 0, Encoding? encoding = null)
        {
            if (encoding == null) encoding = Encoding.UTF8;
            return encoding.GetString(data, index, count).TrimEnd('\0');
        }
        public static DateTime ToDate(byte[] data, int size, int index = 0)
        {
            if (size == 0) return DateTimeReferencePoint;
            var timeOffset = ToInt(data, size, index);
            return DateTimeReferencePoint + TimeSpan.FromMilliseconds(timeOffset / TimeScale);
        }
        public const ulong UnknownSizeVINT8 = 72057594037927935ul;
        public static ulong GetUnknownSizeValue(int size) => (ulong)Math.Pow(2, size * 8 - size) - 1ul;
        public static bool IsUnknownSizeVINT(ulong value, int size) => value == GetUnknownSizeValue(size);
    }
}
