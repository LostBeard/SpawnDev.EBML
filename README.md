# SpawnDev.EBML

[![NuGet version](https://badge.fury.io/nu/SpawnDev.EBML.svg)](https://www.nuget.org/packages/SpawnDev.EBML)

An extensible .NET library for reading, editing, and writing Extensible Binary Meta Language (EBML) documents — the container format family that underlies Matroska (`.mkv`) and WebM. Schemas for `ebml`, `matroska`, and `webm` ship in the box; custom schemas can be loaded via `ParseSchemas(xml)`.

Targets: `net10.0`, `net9.0`, `net8.0`.

## Demo

[Blazor EBML Editor](https://lostbeard.github.io/SpawnDev.EBML/)

## Why this exists

The original motivation: when Chrome's MediaRecorder produces a WebM stream from JavaScript, the resulting container is missing the `Duration` element. Players can still read it, but every seek bar is broken. Rewriting the file to insert a single missing element — without re-encoding and without copying hundreds of megabytes — needs a library that can do non-destructive edits on an EBML document. That's what SpawnDev.EBML does.

It is built on top of [`SpawnDev.PatchStreams`](https://www.nuget.org/packages/SpawnDev.PatchStreams), which provides the patch-based stream that keeps every edit O(1) in space until the final flush.

## Quick tour

```cs
using SpawnDev.EBML;
using SpawnDev.EBML.Elements;

// EBML + Matroska + WebM schemas are loaded by default.
var parser = new EBMLParser();

// --- Parse an existing WebM file ---
using var fileStream = File.OpenRead("TestData/Big_Buck_Bunny_180 10s.webm");
var document = parser.ParseDocument(fileStream);
if (document != null)
{
    Console.WriteLine($"DocType: {document.DocType}");
    // Path navigation.
    Console.WriteLine($"DocType: {document.ReadString("/EBML/DocType")}");

    var duration = document.GetElement<FloatElement>("/Segment/Info/Duration");
    if (duration != null)
        Console.WriteLine($"Duration: {TimeSpan.FromMilliseconds(duration.Data)}");
}

// --- Create a new Matroska document from scratch ---
var mkv = parser.CreateDocument("matroska");
Console.WriteLine($"DocType: {mkv.DocType}");
```

## Key types

| Type | Purpose |
|---|---|
| `EBMLParser` | Entry point. Loads schemas, parses or creates documents. |
| `EBMLDocument` | A parsed document. Extends `MasterElement`. |
| `MasterElement` | Container element with child elements. |
| `FloatElement`, `UintElement`, `IntElement`, `StringElement`, `BinaryElement`, `DateElement` | Typed data elements. |

## Path navigation

Paths look like filesystem paths: `/Segment/Info/Duration`. Append `,N` to an element name to pick a specific occurrence: `/Segment/Cluster,3/Timecode`. Without an index, the path matches all occurrences.

## Schemas

Three XML schemas ship embedded:

- `ebml.xml` — the EBML core header (shared by every EBML-family format)
- `ebml_matroska.xml` — Matroska
- `ebml_webm.xml` — WebM

Load a custom schema with `parser.ParseSchemas(xml)`.

## Editing

EBML documents use `SpawnDev.PatchStreams` under the hood, so edits are non-destructive: changing a `Duration`, inserting a `SeekHead`, or rewriting a `Title` does not rewrite the underlying file. When the document is finally written out, only the affected bytes are emitted.

## Dependencies

- [`SpawnDev.PatchStreams`](https://www.nuget.org/packages/SpawnDev.PatchStreams) — patch-based stream primitive.

## The SpawnDev Crew

This library is built and maintained by a collaborative human + AI crew. Every commit represents real work by everyone listed below.

- **LostBeard** (Todd Tanner) - Captain, library author, keeper of the vision
- **Riker** (Claude CLI #1) - First Officer, implementation lead on consuming projects
- **Data** (Claude CLI #2) - Operations Officer, deep-library work, test rigor, root-cause analysis
- **Tuvok** (Claude CLI #3) - Security/Research Officer, design planning, documentation, code review
- **Geordi** (Claude CLI #4) - Chief Engineer, library internals, GPU kernels, backend work

🖖

## License

MIT. See `LICENSE.txt`.
