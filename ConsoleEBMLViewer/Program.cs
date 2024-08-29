using SpawnDev.EBML;
using SpawnDev.EBML.Elements;
using SpawnDev.EBML.ElementTypes;
using SpawnDev.EBML.Matroska;
using SpawnDev.PatchStreams;

var file1 = @"TestData/Big_Buck_Bunny_180 10s.webm";
var file2 = @"k:\Video\matroska_audio_test.mka";
// input stream
using var fileStreamIn = File.Open(file1, FileMode.Open);
// output stream
var fileStreamOut = File.Open(@"K:\Video\Big_Buck_Bunny_180 10s__apple.webm", FileMode.Create);

var newDoc = new Document();
void PatchStream_OnChanged(PatchStream sender, IEnumerable<Patch> overwrittenPatches, IEnumerable<ByteRange> affectedRegions)
{
    var i = 0;
    Console.WriteLine($"Regions changed: {affectedRegions.Count()}");
    foreach (var range in affectedRegions)
    {
        Console.WriteLine($"- {i} start: {range.Start} size: {range.Size}");
    }
    Console.WriteLine($"-- {sender.Length} > " + string.Join(" ", sender.ToArray(true)));
}
//newDoc.Stream.OnRestorePointsChanged += Stream_OnRestorePointsChanged;

void Stream_OnRestorePointsChanged(PatchStream sender)
{
    if (!sender.RestorePoint) return;
    Console.WriteLine($"--------------- : {sender.RestorePoint}");
}

newDoc.Add("/EBML", 0);
newDoc.WriteString("/EBML/DocType", "webm");
var segment = newDoc.Add<Segment>();
var cluster = segment.Add<Cluster>();
segment.AddCRC32();
newDoc.Add("/Segment/Cluster");
newDoc.Add("/Segment/Cluster");

var crc32 = newDoc.First<CRC32Element>("/Segment,0/CRC-32");
Console.WriteLine($"CRC: {crc32.ToString()}");
newDoc.Add("/Segment/Cluster");
newDoc.Add("/Segment/Cluster");

var tmpppp = newDoc.Find("/Segment/").ToList();
var ebmlEl = newDoc.First<MasterElement>("EBML");
Console.WriteLine("Writing: /EBML/DocTypeExtensionName");
var addded = newDoc.WriteString("/EBML/DocTypeExtensionName", "Apples!");

var hh33h = ebmlEl.Children.ToList();

newDoc.Add("/Segment/Cluster");

var ebmlEl2 = ebmlEl.First("EBML");
var ebmlEl3 = newDoc.First("/EBML");
var ebmlEl4 = newDoc.First("/EBML/DocType");
var ebmlEl5 = ebmlEl.First("/EBML/DocType");
var ttt = ebmlEl4 == ebmlEl5;
var tt1 = ebmlEl4 != ebmlEl5;
var tt2 = ebmlEl4.Equals(ebmlEl5);
newDoc.Stream.Patch.Description = "Added CRC";

var p = ebmlEl.Parent;

var elementsHex = newDoc.Find("/EBML/0x4282,0").ToList();

var docChildren = newDoc.Children.ToList();

var shhil = ebmlEl.Children.ToList();

var hhh = ebmlEl.DocType;

var segmentEl = newDoc.Add<MasterElement>("Segment", 4);
var docChildren1 = newDoc.Children.ToList();
var clusterNew = newDoc.Add<MasterElement>("Cluster", 4);
var docChildren2 = newDoc.Children.ToList();

var segmentEl1 = newDoc.Find("Segment");
var cluster1 = segmentEl.Find("Cluster");

var docTypeElqq = newDoc.Find<StringElement>("/EBML/DocType,0").FirstOrDefault();

var cluster111 = segmentEl.Add("Cluster");

//var ebmlStream = new MasterElement(fileStreamIn);
var hil = newDoc.Children.ToList();

var segmenl1 = newDoc.Find("Segment");
var clter1 = segmentEl.Find("Cluster");

var segment111 = newDoc.Find<UintElement>("/Segment/@uinteger").FirstOrDefault();
var elss = segment111?.Data;
var nmt11 = true;

var h2il = newDoc.Children.ToList();
//var masterElement = new MasterElement(patchStream);
// get all elements in a master
var ebml1 = newDoc.Find<MasterElement>("EBML");

var children = ebml1!.First().Children.ToList();

// get all elements of an ebml type
var elementsType = newDoc.Find<UintElement>("/EBML/@uinteger").ToList();
var eee = elementsType.FirstOrDefault();

var element1E = eee?.Exists ?? false;
// get a specific element type by index
var docTypeEl = newDoc.Find<StringElement>("/EBML/DocType,0").FirstOrDefault();
var docType = docTypeEl.Data;

docTypeEl.Data = "we323432bm";
var docType1 = docTypeEl.Data;
docTypeEl.Document.Undo();
docTypeEl.Document.Redo();
docTypeEl.Document.Undo();
docTypeEl.Remove();
docTypeEl.Document.Undo();
var exists = docTypeEl.Exists;
var docType2 = docTypeEl.Data;
var nmt = true;
