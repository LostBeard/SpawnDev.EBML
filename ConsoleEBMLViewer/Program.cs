using SpawnDev.EBML;
using SpawnDev.EBML.Streams;
using SpawnDev.PatchStreams;



//var newDoc = new MasterElement(new PatchStream());
//var ebmlEl = newDoc.AddMasterElement("EBML");
//ebmlEl.AddStringElement("DocType", "webm");
//var segmentEl = newDoc.AddMasterElement("Segment", "webm");
//var cluster = segmentEl.AddMasterElement("Cluster");
//var nmty = true;


// Create the EBML parser with default configuration
// default configuration supports matroska and webm reading and modification
//var ebml = new EBMLParser();
var file1 = @"TestData/Big_Buck_Bunny_180 10s.webm";
var file2 = @"k:\Video\matroska_audio_test.mka";
// input stream
using var fileStreamIn = File.Open(file1, FileMode.Open);
// output stream
var fileStreamOut = File.Open(@"K:\Video\Big_Buck_Bunny_180 10s__apple.webm", FileMode.Create);
// create patch stream

var ebmlStream1 = new MasterElement(fileStreamIn);
var dd = ebmlStream1.Find<MasterElement>("EBML").FirstOrDefault();
var dt = dd!.Find("DocType");

var ebmlStream = new MasterElement(new PatchStream());
ebmlStream.Stream.OnChanged += PatchStream_OnChanged;
void PatchStream_OnChanged(PatchStream sender, IEnumerable<Patch> overwrittenPatches, IEnumerable<ByteRange> affectedRegions)
{
    var i = 0;
    Console.WriteLine($"Regions changed: {affectedRegions.Count()}");
    foreach (var range in affectedRegions)
    {
        Console.WriteLine($"- {i} start: {range.Start} size: {range.Size}");
    }
    Console.WriteLine("-- " + string.Join(" ", sender.ToArray(true)));
}
var ebmlEl = ebmlStream.AddMasterElement("EBML");
ebmlEl.WriteString("DocType", "webm1");
ebmlEl.WriteString("DocType,7", "webm2");
ebmlEl.WriteString("DocType,2", "webm3");
ebmlEl.WriteString("DocType", "webm");

var shhil = ebmlEl.Children.ToList();

var hhh = ebmlEl.DocType;

var segmentEl = ebmlStream.AddElement<MasterElement>(4, "Segment");


var segmentEl1 = ebmlStream.Find("Segment");
var cluster1 = segmentEl.Find("Cluster");

var docTypeElqq = ebmlStream.Find<StringElement>("/EBML/DocType,0").FirstOrDefault();

var cluster = segmentEl.AddMasterElement("Cluster");

//var ebmlStream = new MasterElement(fileStreamIn);
var hil = ebmlStream.Children.ToList();


var segmenl1 = ebmlStream.Find("Segment");
var clter1 = segmentEl.Find("Cluster");


var segment = ebmlStream.Find<UintElement>("/Segment/@uinteger").FirstOrDefault();
var elss = segment.Value;
var nmt11 = true;

var h2il = ebmlStream.Children.ToList();
//var masterElement = new MasterElement(patchStream);
// get all elements in a master
var ebml1 = ebmlStream.Find<MasterElement>("EBML");

var children = ebml1!.First().Children.ToList();

// get all elements of an ebml type
var elementsType = ebmlStream.Find<UintElement>("/EBML/@uinteger").ToList();
var eee = elementsType.First();
eee.Update();
var element1E = eee.Exists;
// get a specific element type by index
var docTypeEl = ebmlStream.Find<StringElement>("/EBML/DocType,0").FirstOrDefault();
var docType = docTypeEl.Value;

docTypeEl.Value = "webm";
var docType1 = docTypeEl.Value;
docTypeEl.Stream.Undo();
docTypeEl.Remove();
var exists = docTypeEl.Exists;
var docType2 = docTypeEl.Value;
// get an element by hex id and index
var elementsHex = ebmlStream.Find("/EBML/0x4282,0").ToList();
var nmt = true;

//// get all child elements (uses yield for performance gains when exiting iteration early) in /EBML
//var elements = ebml.GetElements(patchStream, "/EBML/").ToList();
//// get element /EBML/DocType
//var element = ebml.GetElement(patchStream, "/EBML/DocType");

//// get element 2nd Cluster element in the first Segment
//// (omitted index notation indicates ",0" which matches the first and "," matches all)
//var element = ebml.GetElement(patchStream, "/Segment/Cluster,1");

//// get all Cluster elements in Segment. Because the method returns a list it is assumed index notation defaults to "," which means all
//// unlike GetElement, an omitted index notation on the name filter indicates all. Missing index notation on the rest of the patch indicates first as it does elsewhere.
//var elements = ebml.GetElements(patchStream, "/Segment/Cluster").ToList();

//// Get all children elements of the the first Cluster in the first Segment
//var elements = ebml.GetElements(patchStream, "/Segment/Cluster/").ToList();

var nm1t = true;
//test.Insert(0, applesauceBytes, fileStream.Length);

//await test.CopyToAsync(fileStreamOut);
//fileStreamOut.Dispose();

//test.Write(applesauceBytes);
//test.Position = 0;
//test.Insert(Encoding.UTF8.GetBytes("Hello "));
//test.Insert(Encoding.UTF8.GetBytes("World!"), applesauceBytes.Length);
//test.Position = 0;
//Console.WriteLine(Encoding.UTF8.GetString(test.ToArray()));
//var nmt = true;
////segment.Splice(6, 5, new MemoryStream(applesauceBytes));

//segment.Position = 6;
//segment.Splice(segment.Position, 5, applesauceBytes);
//segment.Position = 0;

////modifiedVersion.Position = 2;

//Console.WriteLine(Encoding.UTF8.GetString(segment.ToArray()));
//Console.WriteLine(Encoding.UTF8.GetString(modifiedVersion.ToArray()));

//var nmttt = true;
//var ebml = new EBMLParser();
//// get a stream containing an EBML document (or multiple documents)
//using var fileStream = File.Open(@"TestData/Big_Buck_Bunny_180 10s.webm", FileMode.Open);
//// parse the EBML document stream (ParseDocuments can be used to parse all documents in the stream)
//var document = ebml.ParseDocument(fileStream);
//if (document != null)
//{
//    Console.WriteLine($"DocType: {document.DocType}");
//    // or using path
//    Console.WriteLine($"DocType: {document.ReadString("/EBML/DocType")}");

//    // Get an element using the path
//    var durationElement = document.GetElement<FloatElement>("/Segment/Info/Duration");
//    if (durationElement != null)
//    {
//        var duration = durationElement.Data;
//        var durationTime = TimeSpan.FromMilliseconds(duration);
//        Console.WriteLine($"Duration: {durationTime}");
//    }
//}

//// Create a new matroska EBML file
//var matroskaDoc = ebml.CreateDocument("matroska");
//Console.WriteLine($"DocType: {matroskaDoc.DocType}");

//// ...
