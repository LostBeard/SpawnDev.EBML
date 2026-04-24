// Diagnostic tests isolating each step of EBMLDocument's CreateDocument
// path. The goal is to identify the exact phase that fails between
// PatchStream.Insert and MasterElement.Add's post-insert Find call.

using NUnit.Framework;
using SpawnDev.EBML.Elements;
using SpawnDev.EBML.Extensions;
using SpawnDev.EBML.Schemas;
using SpawnDev.PatchStreams;

namespace SpawnDev.EBML.Tests;

[TestFixture]
public class EBMLDocumentCreateDiagnosticsTests
{
    // Step 1. Raw: does a fresh PatchStream + Insert of an EBML header let
    // IsEBML identify it as EBML?
    [Test]
    public void FreshPatchStream_InsertEbmlHeader_IsEBMLReturnsTrue()
    {
        var ps = new PatchStream(new MemoryStream());
        ps.RestorePoint = true;
        // Manually build the EBML header bytes: 4-byte element ID + 1-byte VINT size=0.
        using var header = new MemoryStream(new byte[] { 0x1A, 0x45, 0xDF, 0xA3, 0x80 });
        ps.Position = 0;
        ps.Insert(header);
        ps.RestorePoint = true;

        Assert.That(ps.Length, Is.EqualTo(5));
        ps.Position = 0;
        var parser = new EBMLParser();
        Assert.That(parser.IsEBML(ps), Is.True, "IsEBML must recognise the inserted header");
    }

    // Step 2. What about LatestStable specifically (that is what FindInfo reads)?
    [Test]
    public void LatestStable_AfterInsertAndRestorePoint_IsRecognizedAsEBML()
    {
        var ps = new PatchStream(new MemoryStream());
        ps.RestorePoint = true;
        // Manually build the EBML header bytes: 4-byte element ID + 1-byte VINT size=0.
        using var header = new MemoryStream(new byte[] { 0x1A, 0x45, 0xDF, 0xA3, 0x80 });
        ps.Position = 0;
        ps.Insert(header);
        ps.RestorePoint = true;

        var stable = ps.LatestStable;
        Assert.That(stable.Length, Is.EqualTo(5), "LatestStable length must match");
        stable.Position = 0;
        var parser = new EBMLParser();
        Assert.That(parser.IsEBML(stable), Is.True);
    }

    // Step 3. Reproduce the full Add() sequence: seed + DisableDocumentEngines +
    // Insert + DataChanged + RestorePoint. This is the shape of what
    // EBMLDocument.CreateDocument triggers through MasterElement.Add.
    [Test]
    public void CreateDocument_ViaParser_PrintsInternalState()
    {
        var parser = new EBMLParser();
        // This test will FAIL today; we tee-up detailed state for triage.
        EBMLDocument? doc = null;
        Exception? thrown = null;
        try
        {
            doc = parser.CreateDocument("webm");
        }
        catch (Exception ex)
        {
            thrown = ex;
        }
        TestContext.WriteLine($"Thrown: {thrown?.GetType().Name} {thrown?.Message}");
        TestContext.WriteLine($"StackTrace: {thrown?.StackTrace}");
        Assert.That(doc, Is.Not.Null, thrown?.Message ?? "no exception but doc is null");
    }

    // Step 3b. Verify the exact failure hypothesis.
    //
    // Hypothesis: EBMLDocument.FindInfo reassigns `Info.Stream` to
    // `Stream.LatestStable` during its first invocation (fired from
    // OnChanged while the new patch is not yet a RestorePoint). LatestStable
    // at that moment is a SNAPSHOT of the previous stable patch (empty).
    // After that reassignment, `base.Stream` points at the snapshot, and
    // further `Stream.LatestStable` lookups just return the snapshot itself.
    // The live stream's eventual restore-point becomes invisible.
    [Test]
    public void PatchStream_LatestStable_InOnChangedHandler_BeforeRestorePoint_ReturnsPreviousSnapshot()
    {
        var ps = new PatchStream(new MemoryStream());
        ps.RestorePoint = true;
        PatchStream? stableDuringHandler = null;
        bool stableWasRestorePoint = false;
        long stableLengthDuringHandler = -1;
        ps.OnChanged += (sender, _, _) =>
        {
            stableDuringHandler = sender.LatestStable;
            stableWasRestorePoint = stableDuringHandler.RestorePoint;
            stableLengthDuringHandler = stableDuringHandler.Length;
        };
        ps.Position = 0;
        ps.Insert(new byte[] { 0x1A, 0x45, 0xDF, 0xA3, 0x80 });
        // Inside the handler, the inserted patch has NOT yet been marked
        // RestorePoint. Therefore LatestStable walked backwards to patch 0
        // (empty). So stableLengthDuringHandler should be 0.
        Assert.Multiple(() =>
        {
            Assert.That(stableDuringHandler, Is.Not.Null);
            Assert.That(stableLengthDuringHandler, Is.EqualTo(0),
                "during OnChanged, LatestStable should still be patch 0 (empty)");
            Assert.That(stableWasRestorePoint, Is.True);
        });
        // Now mark the current patch as a restore point.
        ps.RestorePoint = true;
        // A FRESH call to the LIVE stream's LatestStable must now return 5 bytes.
        Assert.That(ps.LatestStable.Length, Is.EqualTo(5),
            "after RestorePoint, live stream's LatestStable advances to include the insert");
        // But a LatestStable taken on the EARLIER snapshot (the one captured
        // in the handler) still has length 0 and cannot advance. This is
        // the exact EBML bug.
        Assert.That(stableDuringHandler!.LatestStable.Length, Is.EqualTo(0),
            "snapshot captured during the handler cannot see later insertions");
    }

    // Step 4. Confirm the fixed PatchStream feeds raw bytes through Read()
    // when stream is positioned post-Insert and multi-source is in play.
    [Test]
    public void PatchStream_PostInsert_Read_At_Zero_ReturnsHeaderBytes()
    {
        var ps = new PatchStream(new MemoryStream());
        ps.RestorePoint = true;
        ps.Position = 0;
        // Manually build the EBML header bytes: 4-byte element ID + 1-byte VINT size=0.
        using var header = new MemoryStream(new byte[] { 0x1A, 0x45, 0xDF, 0xA3, 0x80 });
        ps.Insert(header);
        ps.RestorePoint = true;

        ps.Position = 0;
        var buf = new byte[5];
        var read = 0;
        while (read < buf.Length)
        {
            int got = ps.Read(buf, read, buf.Length - read);
            if (got <= 0) break;
            read += got;
        }
        Assert.Multiple(() =>
        {
            Assert.That(read, Is.EqualTo(5));
            Assert.That(buf, Is.EqualTo(new byte[] { 0x1A, 0x45, 0xDF, 0xA3, 0x80 }));
        });
    }
}
