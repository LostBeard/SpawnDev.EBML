using SpawnDev.EBML;
using SpawnDev.EBML.Elements;
using SpawnDev.EBML.ElementTypes;
using SpawnDev.EBML.Matroska;

var file1 = @"TestData/Big_Buck_Bunny_180 10s.webm";
var file2 = @"k:\Video\matroska_audio_test.mka";

// input stream
using var fileStreamIn = File.Open(file1, FileMode.Open);

var newDoc = new Document("webm");
Console.WriteLine($"DocType: 'webm' == {newDoc.DocType}");

Console.WriteLine($"DocType: 'webm' == {newDoc.DocType}");
var segment = newDoc.Add<Segment>();
var cluster = segment.Add<Cluster>();
// Adds an auto updating CRC-32 element to /Segement,0
segment.AddCRC32();
newDoc.Add("/Segment/Cluster");
newDoc.Add("/Segment/Cluster");
var crc32 = newDoc.First<CRC32Element>("/Segment,0/CRC-32");
Console.WriteLine($"CRC: {crc32!.ToString()}");
newDoc.Add("/Segment/Cluster");
newDoc.Add("/Segment/Cluster");

newDoc.WriteString("/EBML/DocTypeExtensionName", "some ext");

newDoc.Add("/Segment/Cluster");

// get a specific element type by index
var docTypeEl = newDoc.First<StringElement>("/EBML/DocType");
var docType = docTypeEl.Data;

Console.WriteLine($"DocType: 'webm' == {docTypeEl.Data}");
docTypeEl.Data = "matroska";
Console.WriteLine($"DocType: 'matroska' == {docTypeEl.Data}");
docTypeEl.Document.Undo();
Console.WriteLine($"DocType: 'webm' == {docTypeEl.Data}");
docTypeEl.Document.Redo();
Console.WriteLine($"DocType: 'matroska' == {docTypeEl.Data}");
docTypeEl.Document.Undo();
Console.WriteLine($"DocType: 'webm' == {docTypeEl.Data}");
docTypeEl.Remove();
Console.WriteLine($"DocType: '' == {docTypeEl.Data}");
// docTypeEl.Data is now empty
docTypeEl.Document.Undo();
Console.WriteLine($"DocType: 'webm' == {docTypeEl.Data}");
var nmt = true;