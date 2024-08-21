// See https://aka.ms/new-console-template for more information
using SpawnDev.EBML;

// Create the EBML schema set
var schemaSet = new SchemaSet();
// Load the embedded schema XMLs (ebml, matroska, and webm)
schemaSet.LoadDefaultSchemas();
schemaSet.RegisterDocumentEngine<MatroskaDocumentEngine>();

using var fileStream = File.Open(@"TestData\Big_Buck_Bunny_180 10s.webm", FileMode.Open);

var docs = schemaSet.ParseDocuments(fileStream).ToList();
var nmt = true;
