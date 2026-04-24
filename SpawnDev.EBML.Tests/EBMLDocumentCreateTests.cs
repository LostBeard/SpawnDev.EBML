// Tests that exercise the CREATE path on EBMLDocument / EBMLParser.
// This surface is known to crash in the v3-preview WIP; the tests encode the
// intended behaviour so fixes can be validated.

using NUnit.Framework;
using SpawnDev.EBML;
using SpawnDev.EBML.Elements;
using SpawnDev.EBML.Schemas;

namespace SpawnDev.EBML.Tests;

[TestFixture]
public class EBMLDocumentCreateTests
{
    [Test]
    public void CreateDocument_ViaParser_WebM_ReturnsNonNull()
    {
        var parser = new EBMLParser();
        var doc = parser.CreateDocument("webm");
        Assert.That(doc, Is.Not.Null);
    }

    [Test]
    public void CreateDocument_ViaParser_WebM_HasDocTypeWebm()
    {
        var parser = new EBMLParser();
        var doc = parser.CreateDocument("webm");
        Assert.That(doc.DocType, Is.EqualTo("webm"));
    }

    [Test]
    public void CreateDocument_ViaParser_Matroska_HasDocTypeMatroska()
    {
        var parser = new EBMLParser();
        var doc = parser.CreateDocument("matroska");
        Assert.That(doc.DocType, Is.EqualTo("matroska"));
    }

    [Test]
    public void CreateDocument_NewWebM_Has_EBMLHeader()
    {
        var parser = new EBMLParser();
        var doc = parser.CreateDocument("webm");
        Assert.That(doc.Header, Is.Not.Null, "Freshly-created WebM should have the /EBML header.");
    }

    [Test]
    public void CreateDocument_NewWebM_HeaderContains_DocTypeChildWithValueWebm()
    {
        var parser = new EBMLParser();
        var doc = parser.CreateDocument("webm");
        var docType = doc.Header?.First<StringElement>("DocType");
        Assert.That(docType, Is.Not.Null);
        Assert.That(docType!.Data, Is.EqualTo("webm"));
    }

    [Test]
    public void CreateDocument_ReadStringOnDocTypePath_ReturnsWebm()
    {
        var parser = new EBMLParser();
        var doc = parser.CreateDocument("webm");
        Assert.That(doc.ReadString("/EBML/DocType"), Is.EqualTo("webm"));
    }

    [Test]
    public void CreateDocument_DirectConstructor_Equivalent()
    {
        // Constructor form `new EBMLDocument("webm")` must be equivalent to parser.CreateDocument.
        using var doc = new EBMLDocument("webm");
        Assert.That(doc.DocType, Is.EqualTo("webm"));
    }
}
