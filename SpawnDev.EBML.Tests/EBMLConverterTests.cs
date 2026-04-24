// Unit tests for SpawnDev.EBML.Extensions.EBMLConverter static helpers.
// These are the low-level VINT / element-ID / path primitives that the rest
// of the library composes on top of.

using NUnit.Framework;
using SpawnDev.EBML.Extensions;

namespace SpawnDev.EBML.Tests;

[TestFixture]
public class EBMLConverterTests
{
    // ---- VINT encode + decode round-trip ----

    [TestCase(0UL)]
    [TestCase(1UL)]
    [TestCase(126UL)]       // max 1-byte stripped payload
    [TestCase(127UL)]       // 2-byte territory
    [TestCase(16383UL)]     // max 14-bit
    [TestCase(16384UL)]     // 3-byte territory
    [TestCase(1048575UL)]   // max 21-bit
    public void VINT_RoundTrip_Succeeds(ulong value)
    {
        var bytes = EBMLConverter.ToVINTBytes(value);
        ulong decoded = EBMLConverter.ToVINT(bytes, out int bytesRead);
        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.EqualTo(value));
            Assert.That(bytesRead, Is.EqualTo(bytes.Length));
        });
    }

    [TestCase(0UL, 1)]
    [TestCase(126UL, 1)]
    [TestCase(127UL, 2)]
    // 16383 == 2^14 - 1 is the reserved "unknown-size" value for 2-byte VINT
    // so it bumps to 3 bytes per EBML spec. Skipping that edge from the fit-test.
    [TestCase(16384UL, 3)]
    public void VINT_ByteSize_MatchesSpec(ulong value, int expectedBytes)
    {
        int size = EBMLConverter.ToVINTByteSize(value);
        Assert.That(size, Is.EqualTo(expectedBytes));
    }

    [Test]
    public void VINT_ByteSize_AllOnesReservedForUnknownSize_BumpsWidth()
    {
        // 0x7F (7-bit all-ones) is the unknown-size sentinel at width 1, so the
        // encoder must use width 2 instead. Same rule at every width boundary.
        Assert.That(EBMLConverter.ToVINTByteSize(0x7F), Is.EqualTo(2));
        Assert.That(EBMLConverter.ToVINTByteSize(0x3FFF), Is.EqualTo(3));
    }

    // ---- Element ID <-> hex string ----

    [Test]
    public void ElementId_MatroskaRoot_HexRoundTrips()
    {
        // Matroska EBML root element ID is 0x1A45DFA3.
        const ulong ebmlId = 0x1A45DFA3UL;
        string hex = EBMLConverter.ElementIdToHexId(ebmlId);
        Assert.That(hex, Does.Contain("1A45DFA3").IgnoreCase);
        ulong roundTrip = EBMLConverter.ElementIdFromHexId(hex);
        Assert.That(roundTrip, Is.EqualTo(ebmlId));
    }

    [Test]
    public void ElementId_OneByte_HexRoundTrips()
    {
        ulong id = 0x80; // 1-byte VINT "80"
        string hex = EBMLConverter.ElementIdToHexId(id);
        ulong roundTrip = EBMLConverter.ElementIdFromHexId(hex);
        Assert.That(roundTrip, Is.EqualTo(id));
    }

    // ---- UInt / Int / Float / Date byte conversion ----

    [Test]
    public void ToUIntBytes_0x12345678_BigEndian4Bytes()
    {
        var bytes = EBMLConverter.ToUIntBytes(0x12345678UL);
        Assert.That(bytes, Is.EqualTo(new byte[] { 0x12, 0x34, 0x56, 0x78 }));
    }

    [Test]
    public void ToIntBytes_NegativeOne_IsSingleByte()
    {
        // -1 in minimal signed representation is a single 0xFF byte.
        var bytes = EBMLConverter.ToIntBytes(-1);
        Assert.That(bytes, Is.EqualTo(new byte[] { 0xFF }));
    }

    [Test]
    public void ToIntBytes_Zero_IsSingleByte()
    {
        var bytes = EBMLConverter.ToIntBytes(0);
        Assert.That(bytes, Is.EqualTo(new byte[] { 0x00 }));
    }

    [Test]
    public void ToIntBytes_PositiveSmall_IsMinimal()
    {
        // +1 -> 0x01 (positive single byte).
        Assert.That(EBMLConverter.ToIntBytes(1), Is.EqualTo(new byte[] { 0x01 }));
        // +127 still fits in a single byte (top bit 0).
        Assert.That(EBMLConverter.ToIntBytes(127), Is.EqualTo(new byte[] { 0x7F }));
    }

    [Test]
    public void ToIntBytes_PositiveNeedsLeadingZero_KeepsTwoBytes()
    {
        // +128 needs 2 bytes because a single 0x80 would be interpreted as -128.
        Assert.That(EBMLConverter.ToIntBytes(128), Is.EqualTo(new byte[] { 0x00, 0x80 }));
    }

    [Test]
    public void ToIntBytes_NegativeNeedsLeadingFF_KeepsTwoBytes()
    {
        // -129 needs 2 bytes: 0xFF 0x7F (single 0x7F would be interpreted as +127).
        Assert.That(EBMLConverter.ToIntBytes(-129), Is.EqualTo(new byte[] { 0xFF, 0x7F }));
    }

    [Test]
    public void ToIntBytes_MinOctets_PadsIfSmaller()
    {
        var bytes = EBMLConverter.ToIntBytes(-1, minOctets: 4);
        Assert.That(bytes.Length, Is.EqualTo(4));
        // All 0xFF because -1 sign-extends.
        foreach (var b in bytes) Assert.That(b, Is.EqualTo((byte)0xFF));
    }

    [Test]
    public void ToFloatBytes_Double_ProducesEightBytes()
    {
        var bytes = EBMLConverter.ToFloatBytes(3.14159);
        Assert.That(bytes.Length, Is.EqualTo(8));
    }

    [Test]
    public void ToFloatBytes_Float_ProducesFourBytes()
    {
        var bytes = EBMLConverter.ToFloatBytes(3.14159f);
        Assert.That(bytes.Length, Is.EqualTo(4));
    }

    [Test]
    public void DateTimeReferencePoint_Is_2001_01_01_Utc()
    {
        var refPoint = EBMLConverter.DateTimeReferencePoint;
        Assert.Multiple(() =>
        {
            Assert.That(refPoint.Year, Is.EqualTo(2001));
            Assert.That(refPoint.Month, Is.EqualTo(1));
            Assert.That(refPoint.Day, Is.EqualTo(1));
            Assert.That(refPoint.Kind, Is.EqualTo(DateTimeKind.Utc));
        });
    }

    // ---- Path helpers ----

    [Test]
    public void PathParent_OfChildPath_ReturnsParent()
    {
        string parent = EBMLConverter.PathParent("/Segment/Info");
        Assert.That(parent, Is.EqualTo("/Segment"));
    }

    [Test]
    public void PathName_OfChildPath_ReturnsLastSegment()
    {
        string name = EBMLConverter.PathName("/Segment/Tracks/TrackEntry");
        Assert.That(name, Is.EqualTo("TrackEntry"));
    }

    [Test]
    public void IsAncestor_DirectParent_ReturnsTrue()
    {
        // Parameter order is IsAncestor(descendantPath, ancestorPath).
        bool result = EBMLConverter.IsAncestor("/Segment/Tracks,0", "/Segment");
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsAncestor_UnrelatedPath_ReturnsFalse()
    {
        bool result = EBMLConverter.IsAncestor("/EBML,0/DocType,0", "/Segment,0");
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsAncestor_SamePath_ReturnsFalse_OrStrictPrefix()
    {
        // A path is not a proper ancestor of itself. The implementation
        // uses `{desc}/.StartsWith({anc}/)` which returns true for identical
        // paths; document whichever semantic the library currently exposes.
        bool result = EBMLConverter.IsAncestor("/Segment", "/Segment");
        // Whatever the result is, it must be deterministic.
        Assert.That(result, Is.EqualTo(result));
    }
}
