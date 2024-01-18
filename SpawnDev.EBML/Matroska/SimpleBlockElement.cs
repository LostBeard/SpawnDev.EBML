namespace SpawnDev.EBML.Matroska
{
    public class SimpleBlockElement : BinaryElement
    {
        public SimpleBlockElement(Enum id) : base(id) { }
        public ulong TrackId { get; private set; }
        public short Timecode { get; private set; }
        public override string ToString() => $"{Index} {Id} - IdChain: [ {IdChain.ToString(", ")} ] Type: {GetType().Name} Length: {Length} bytes TrackId: {TrackId} Timecode: {Timecode}";
        public override void UpdateBySource()
        {
            Stream!.Position = 0;
            TrackId = Stream!.ReadEBMLVINT(out var vintDataAllOnes);
            Timecode = BigEndian.ToInt16(Stream!.ReadBytes(1, 2));
        }
    }
}
