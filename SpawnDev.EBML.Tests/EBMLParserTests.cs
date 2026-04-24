// End-to-end tests for EBMLParser + EBMLDocument against the bundled
// Big_Buck_Bunny_180 10s.webm file. Exercises the real schema-driven parser
// path that production Matroska/WebM consumers depend on.

using NUnit.Framework;
using SpawnDev.EBML;
using SpawnDev.EBML.Elements;
using SpawnDev.EBML.Schemas;

namespace SpawnDev.EBML.Tests;

[TestFixture]
public class EBMLParserTests
{
    private const string WebMPath = "TestData/Big_Buck_Bunny_180 10s.webm";

    private static Stream OpenWebM()
    {
        if (!File.Exists(WebMPath))
            throw new FileNotFoundException(
                $"Test asset missing: {WebMPath} (should be copied via the csproj None Include).");
        return File.OpenRead(WebMPath);
    }

    [Test]
    public void EBMLParser_LoadsDefaultSchemas_WithAtLeastEbmlMatroskaWebm()
    {
        var parser = new EBMLParser();
        var keys = parser.Schemas.Keys.ToList();
        // Expected: at minimum the "ebml" core schema plus one or more body schemas.
        Assert.That(keys.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(keys.Any(k => k.Equals("ebml", StringComparison.OrdinalIgnoreCase)), Is.True,
            $"Expected an 'ebml' schema in {string.Join(",", keys)}");
    }

    [Test]
    public void EBMLParser_IsEBML_OnBundledWebM_ReturnsTrue()
    {
        using var stream = OpenWebM();
        var parser = new EBMLParser();
        Assert.That(parser.IsEBML(stream), Is.True);
    }

    [Test]
    public void EBMLParser_IsEBML_OnGarbageBytes_ReturnsFalse()
    {
        var garbage = new MemoryStream(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 });
        var parser = new EBMLParser();
        Assert.That(parser.IsEBML(garbage), Is.False);
    }

    [Test]
    public void EBMLParser_ParseDocument_OnBundledWebM_ReturnsNonNull()
    {
        using var stream = OpenWebM();
        var parser = new EBMLParser();
        var doc = parser.ParseDocument(stream);
        Assert.That(doc, Is.Not.Null);
    }

    [Test]
    public void EBMLDocument_Header_IsEbmlMasterElement()
    {
        using var stream = OpenWebM();
        var parser = new EBMLParser();
        var doc = parser.ParseDocument(stream);
        Assert.That(doc, Is.Not.Null);
        Assert.That(doc!.Header, Is.Not.Null, "/EBML master element expected in a valid WebM.");
    }

    [Test]
    public void EBMLDocument_DocType_IsWebm()
    {
        using var stream = OpenWebM();
        var parser = new EBMLParser();
        var doc = parser.ParseDocument(stream);
        Assert.That(doc, Is.Not.Null);
        // DocType element lives at /EBML/DocType per the EBML spec.
        var docTypeElem = doc!.Header?.First<StringElement>("DocType");
        Assert.That(docTypeElem, Is.Not.Null, "Missing /EBML/DocType element.");
        Assert.That(docTypeElem!.Data, Is.EqualTo("webm"));
    }

    [Test]
    public void EBMLDocument_Body_HasAtLeastOneElement()
    {
        using var stream = OpenWebM();
        var parser = new EBMLParser();
        var doc = parser.ParseDocument(stream);
        Assert.That(doc, Is.Not.Null);
        var body = doc!.Body.ToList();
        Assert.That(body.Count, Is.GreaterThan(0), "WebM body should contain at least a Segment element.");
    }

    [Test]
    public void EBMLDocument_ReadString_DocType_ReturnsWebm()
    {
        using var stream = OpenWebM();
        var parser = new EBMLParser();
        var doc = parser.ParseDocument(stream);
        Assert.That(doc, Is.Not.Null);
        // Path-based navigation from the README example.
        var docType = doc!.ReadString("/EBML/DocType");
        Assert.That(docType, Is.EqualTo("webm"));
    }

    [Test]
    public void EBMLDocument_HasSegment_ElementId0x18538067()
    {
        using var stream = OpenWebM();
        var parser = new EBMLParser();
        var doc = parser.ParseDocument(stream);
        Assert.That(doc, Is.Not.Null);
        // Segment is the second top-level element after /EBML.
        var segment = doc!.Body.FirstOrDefault();
        Assert.That(segment, Is.Not.Null);
        // Canonical Segment FourCC.
        Assert.That(segment!.Id, Is.EqualTo(0x18538067UL));
    }
}
