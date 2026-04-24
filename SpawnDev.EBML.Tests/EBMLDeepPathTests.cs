// Probe tests for deep-path Find: when /A/B/C returns a master element,
// that element's InstancePath MUST be the full chain "A,0/B,0/C,0" so
// that subsequent relative Find on the returned element works. Also its
// Children must enumerate its declared-size body.

using NUnit.Framework;
using SpawnDev.EBML;
using SpawnDev.EBML.Elements;
using SpawnDev.EBML.Schemas;

namespace SpawnDev.EBML.Tests;

[TestFixture]
public class EBMLDeepPathTests
{
    private const string WebMPath = "TestData/Big_Buck_Bunny_180 10s.webm";

    private static Stream OpenWebM()
    {
        if (!File.Exists(WebMPath))
            throw new FileNotFoundException(
                $"Test asset missing: {WebMPath} (copied via the csproj None Include).");
        return File.OpenRead(WebMPath);
    }

    // Hypothesis: an element returned via `Find<MasterElement>("/A/B/C")`
    // has InstancePath "C,0" (only the last segment) rather than
    // "A,0/B,0/C,0". This is why Children / relative Find don't work on
    // it from a consumer's perspective.

    [Test]
    public void First_SegmentTracks_ReturnsMasterWithFullInstancePath()
    {
        using var stream = OpenWebM();
        var parser = new EBMLParser();
        var doc = parser.ParseDocument(stream);
        Assert.That(doc, Is.Not.Null);

        var tracks = doc!.First<MasterElement>("/Segment/Tracks");
        Assert.That(tracks, Is.Not.Null, "Tracks master must exist in a WebM file");

        // InstancePath should include the parent chain from the document root.
        // "/Segment/Tracks" as a path becomes instance path "Segment,0/Tracks,0"
        // (or "/Segment,0/Tracks,0") - NOT just "Tracks,0".
        Assert.That(tracks!.InstancePath,
            Does.Contain("Segment"),
            $"Tracks InstancePath should include 'Segment', got '{tracks.InstancePath}'");
    }

    [Test]
    public void First_SegmentTracks_ReturnedMasterEnumeratesChildren()
    {
        using var stream = OpenWebM();
        var parser = new EBMLParser();
        var doc = parser.ParseDocument(stream);
        Assert.That(doc, Is.Not.Null);

        var tracks = doc!.First<MasterElement>("/Segment/Tracks");
        Assert.That(tracks, Is.Not.Null);

        var children = tracks!.Children.ToList();
        Assert.That(children.Count, Is.GreaterThan(0),
            "/Segment/Tracks must have at least one TrackEntry child");
    }

    [Test]
    public void First_SegmentTracksTrackEntry_HasWorkingRelativeFind()
    {
        using var stream = OpenWebM();
        var parser = new EBMLParser();
        var doc = parser.ParseDocument(stream);
        Assert.That(doc, Is.Not.Null);

        // Navigate one level deeper.
        var entry = doc!.First<MasterElement>("/Segment/Tracks/TrackEntry");
        Assert.That(entry, Is.Not.Null, "at least one TrackEntry must exist");

        // Relative Find on the returned entry SHOULD find its children.
        var codecId = entry!.First<StringElement>("CodecID")?.Data;
        Assert.That(codecId, Is.Not.Null.And.Not.Empty,
            "TrackEntry.First<StringElement>(\"CodecID\") must return the codec string");
    }

    [Test]
    public void AbsolutePath_Find_IsConsistentWithDeepReturnedMaster()
    {
        // Absolute-path query returns a known-correct answer. The value
        // we get from a relative Find on a deep master MUST match it.
        using var stream = OpenWebM();
        var parser = new EBMLParser();
        var doc = parser.ParseDocument(stream);
        Assert.That(doc, Is.Not.Null);

        var codecAbsolute = doc!.Find<StringElement>(
            "/Segment/Tracks/TrackEntry/CodecID").FirstOrDefault()?.Data;
        Assert.That(codecAbsolute, Is.Not.Null.And.Not.Empty);

        var entry = doc.First<MasterElement>("/Segment/Tracks/TrackEntry");
        Assert.That(entry, Is.Not.Null);
        var codecRelative = entry!.First<StringElement>("CodecID")?.Data;
        Assert.That(codecRelative, Is.EqualTo(codecAbsolute));
    }
}
