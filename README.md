# SpawnDev.EBML

| Name | Package | Description |
|---------|-------------|-------------|
|**SpawnDev.EBML**|[![NuGet version](https://badge.fury.io/nu/SpawnDev.EBML.svg)](https://www.nuget.org/packages/SpawnDev.EBML)| An extendable .Net library for reading and writing Extensible Binary Meta Language (aka EBML) documents. Includes schema for Matroska and WebM. | 

## Demo
[Blazor EBML Editor](https://lostbeard.github.io/SpawnDev.EBML/)


## Version 2 changes
- The library now uses string paths instead of the Enums found in version 1.
- The library now uses (and includes) XML schema files:  
- [EBML](https://github.com/ietf-wg-cellar/ebml-specification/blob/master/ebml.xml)
- [Matroska](https://github.com/ietf-wg-cellar/matroska-specification/blob/master/ebml_matroska.xml)  

Note: The Matroska schema xml is currently also used for WebM ebml documents.

## Usage

```cs
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
```