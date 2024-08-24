using SpawnDev.EBML;
using SpawnDev.EBML.Segments;
using System.Text;

// Create the EBML parser with default configuration
// default configuration supports matroska and webm reading and modification


//var segment = new SegmentSource(Encoding.UTF8.GetBytes("bHello world!p;"), 1, 12);
using var fileStream = File.Open(@"TestData/Big_Buck_Bunny_180 10s.webm", FileMode.Open);
var fileStreamOut = File.Open(@"K:\Video\Big_Buck_Bunny_180 10s__apple.webm", FileMode.Create);
var ebml = new EBMLParser();
var applesauceBytes = Encoding.UTF8.GetBytes("applesauce");
//// this modified version references the source data. No copies are made.
//var modifiedVersion = segment.ToSpliced(6, 5, new MemoryStream(applesauceBytes));


var test = new PatchStream(fileStream);

await test.CopyToAsync(fileStreamOut);
fileStreamOut.Dispose();

test.Write(applesauceBytes);
test.Position = 0;
test.Insert(Encoding.UTF8.GetBytes("Hello "));
test.Insert(Encoding.UTF8.GetBytes("World!"), applesauceBytes.Length);
test.Position = 0;
Console.WriteLine(Encoding.UTF8.GetString(test.ToArray()));
var nmt = true;
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
