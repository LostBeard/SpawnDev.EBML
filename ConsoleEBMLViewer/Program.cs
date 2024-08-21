using SpawnDev.EBML;

// Create the EBML parser with default configuration
// default configuration supports matroska and webm reading and modification
var ebml = new EBMLParser();
// get a stream containing an EBML document (or multiple documents)
using var fileStream = File.Open(@"TestData\Big_Buck_Bunny_180 10s.webm", FileMode.Open);
// parse the EBML document stream (ParseDocuments can be used to parse all documents in the stream)
var document = ebml.ParseDocument(fileStream);
if (document != null)
{
    Console.WriteLine($"DocType: {document.DocType}");
}

// Create a new matroska EBML file
var matroskaDoc = ebml.CreateDocument("matroska");
Console.WriteLine($"DocType: {matroskaDoc.DocType}");

// ...
