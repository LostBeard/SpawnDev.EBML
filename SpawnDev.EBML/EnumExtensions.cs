using SpawnDev.EBML.Matroska;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpawnDev.EBML
{
    public static class EnumExtensions
    {
        public static string ToString(this Enum[] enumArray, string delimeter) => string.Join(delimeter, enumArray.Select(o => o.ToString()).ToArray());
        public static ulong[] ToUInt64(this Enum[] enumArray) => enumArray.Select(ToUInt64).ToArray();
        public static ulong ToUInt64(this Enum value) => (ulong)(object)value;
        public static Enum ToEnum(this Enum value, Type enumType) => (Enum)Enum.ToObject(enumType, value);
    }
}
