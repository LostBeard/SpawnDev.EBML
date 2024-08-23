using SpawnDev.EBML;
using SpawnDev.EBML.Elements;

// Create the EBML parser with default configuration
// default configuration supports matroska and webm reading and modification
var ebml = new EBMLParser();
// get a stream containing an EBML document (or multiple documents)
using var fileStream = File.Open(@"TestData/Big_Buck_Bunny_180 10s.webm", FileMode.Open);
// parse the EBML document stream (ParseDocuments can be used to parse all documents in the stream)
var document = ebml.ParseDocument(fileStream);
if (document != null)
{
    Console.WriteLine($"DocType: {document.DocType}");
    // or using path
    Console.WriteLine($"DocType: {document.ReadString(@"/EBML/DocType")}");

    // Get an element using the path
    var durationElement = document.GetElement<FloatElement>(@"/Segment/Info/Duration");
    if (durationElement != null)
    {
        var duration = durationElement.Data;
        var durationTime = TimeSpan.FromMilliseconds(duration);
        Console.WriteLine($"Duration: {durationTime}");
    }
}

// Create a new matroska EBML file
var matroskaDoc = ebml.CreateDocument("matroska");
Console.WriteLine($"DocType: {matroskaDoc.DocType}");

// ...
