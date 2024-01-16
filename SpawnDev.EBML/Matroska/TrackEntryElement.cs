namespace SpawnDev.EBML.Matroska
{
    public enum TrackType : byte
    {
        Video = 1,
        Audio = 2,
        Complex = 3,
        Logo = 0x10,
        Subtitle = 0x11,
        Buttons = 0x12,
        Control = 0x20,
    }

    public class TrackEntryElement : MasterElement
    {
        public TrackEntryElement(Enum id) : base(id)
        {
        }
        public byte TrackNumber
        {
            get => (byte)(ulong)GetElement<UintElement>(MatroskaId.TrackNumber);
        }
        public byte TrackUID
        {
            get => (byte)(ulong)GetElement<UintElement>(MatroskaId.TrackUID);
        }
        public TrackType TrackType
        {
            get => (TrackType)(byte)(ulong)GetElement<UintElement>(MatroskaId.TrackType);
        }
        public string CodecID
        {
            get => (string)GetElement<StringElement>(MatroskaId.CodecID)!;
        }
        public string Language
        {
            get => (string)GetElement<StringElement>(MatroskaId.Language)!;
        }
        public ulong DefaultDuration
        {
            get => (ulong)GetElement<UintElement>(MatroskaId.DefaultDuration);
        }
    }
}
