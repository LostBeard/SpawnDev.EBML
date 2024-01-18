namespace SpawnDev.EBML.Matroska
{
    public class SimpleBlockElement : BinaryElement
    {
        public SimpleBlockElement(Enum id) : base(id) { }
        public ulong? _TrackId = null;
        public ulong TrackId
        {
            get
            {
                if (_TrackId == null)
                {
                    Stream!.Position = 0;
                    _TrackId = Stream!.ReadEBMLVINT(out var vintDataAllOnes);
                }
                return _TrackId.Value;
            }
        }
        public short Timecode
        {
            get
            {
                Stream!.Position = 0;
                // skip track id (variable sized uint
                Stream.SkipEBMLVINT();
                return BigEndian.ToInt16(Stream!.ReadBytes(2));
            }
        }
        public override string ToString() => $"{Index} {Id} - IdChain: [ {IdChain.ToString(", ")} ] Type: {GetType().Name} Length: {Length} bytes TrackId: {TrackId} Timecode: {Timecode}";
    }
}
