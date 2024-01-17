namespace SpawnDev.EBML.Matroska
{
    public class SimpleBlockElement : BinaryElement
    {
        public SimpleBlockElement(Enum id) : base(id) { }
        public byte TrackId => (byte)(Stream!.ReadByteOrThrow(0) & ~0x80);
        public uint Timecode => BigEndian.ToUInt16(Stream!.ReadBytes(1, 2));
        public override string ToString() => $"{Index} {Id} - IdChain: [ {IdChain.ToString(", ")} ] Type: {GetType().Name} Length: {Length} bytes TrackId: {TrackId} Timecode: {Timecode}";
    }
}
