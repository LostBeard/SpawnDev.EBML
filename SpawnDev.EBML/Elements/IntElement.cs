﻿using SpawnDev.EBML.Extensions;
using SpawnDev.EBML.Segments;

namespace SpawnDev.EBML.Elements
{
    public class IntElement : BaseElement<long>
    {
        public const string TypeName = "integer";
        public override string DataString
        {
            get => Data.ToString();
            set
            {
                if (long.TryParse(value, out var v))
                {
                    Data = v;
                }
            }
        }
        public IntElement(EBMLSchemaElement schemaElement , SegmentSource source, ElementHeader? header = null) : base(schemaElement, source, header) { }
        public IntElement(EBMLSchemaElement schemaElement, long value) : base(schemaElement, value) { }
        public IntElement(EBMLSchemaElement schemaElement) : base(schemaElement, default) { }
        protected override long DataFromSegmentSource() => EBMLConverter.ReadEBMLInt(SegmentSource.ReadBytes(0, SegmentSource.Length, true));
        protected override SegmentSource DataToSegmentSource() => new ByteSegment(EBMLConverter.ToIntBytes(Data));
    }
}
