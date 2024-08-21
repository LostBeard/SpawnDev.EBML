// See https://aka.ms/new-console-template for more information
using SpawnDev.EBML;

// Create the EBML schema set
var schemaSet = new SchemaSet();
// Load the embedded schema XMLs (ebml, matroska, and webm)
schemaSet.LoadDefaultSchemas();
schemaSet.RegisterDocumentEngine<MatroskaDocumentEngine>();
// get a stream containing an EBML document (or multiple documents)
using var fileStream = File.Open(@"TestData\Big_Buck_Bunny_180 10s.webm", FileMode.Open);
// parse the EBML document stream (ParseDocuments can be used to parse all documents in the stream)
var document = schemaSet.ParseDocument(fileStream);
if (document != null)
{
    Console.WriteLine($"DocType: {document.DocType}");
}
var nmt = true;
