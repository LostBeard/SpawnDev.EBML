﻿using SpawnDev.EBML.Extensions;
using SpawnDev.EBML.Segments;

namespace SpawnDev.EBML.Elements
{
    public class BinaryElement : BaseElement<byte[]>
    {
        public const string TypeName = "binary";
        protected override bool EqualCheck(byte[] obj1, byte[] obj2) => obj1 == obj2 || obj1.SequenceEqual(obj2);
        public override string DataString => Data.Length <= 8 ? "0x" + Convert.ToHexString(Data) : "0x" + Convert.ToHexString(Data.Take(8).ToArray()) + "...";
        public BinaryElement(EBMLSchemaElement schemaElement, SegmentSource source, ElementHeader? header = null) : base(schemaElement, source, header) { }
        public BinaryElement(EBMLSchemaElement schemaElement, byte[] value) : base(schemaElement, value) { }
        public BinaryElement(EBMLSchemaElement schemaElement) : base(schemaElement, Array.Empty<byte>()) { }
        protected override byte[] DataFromSegmentSource() => SegmentSource.ReadBytes(0, SegmentSource.Length, true);
        protected override SegmentSource DataToSegmentSource() => new ByteSegment(Data);
    }
}
