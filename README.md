# SpawnDev.EBML

| Name | Package | Description |
|---------|-------------|-------------|
|**SpawnDev.EBML**|[![NuGet version](https://badge.fury.io/nu/SpawnDev.EBML.svg)](https://www.nuget.org/packages/SpawnDev.EBML)| An extendable .Net library for reading and writing Extensible Binary Meta Language (aka EBML) documents. Includes schema for Matroska and WebM. | 


## Demo
[Blazor EBML Editor](https://lostbeard.github.io/SpawnDev.EBML/)

# Version 2
Version 2 supports EBML schema XML documents and uses string paths instead of the Enums found in version 1.

- Version 2 docs coming soon.

```cs
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

```