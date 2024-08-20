// See https://aka.ms/new-console-template for more information
using SpawnDev.EBML;

Console.WriteLine("Hello, World!");


var schemaSet = new EBMLSchemaSet();
schemaSet.LoadExecutingAssemblyEmbeddedSchemaXMLs();
schemaSet.RegisterDocumentEngine<MatroskaDocumentEngine>();


using var fileStream = File.Open(@"k:\Video\matroska_audio_test.mka", FileMode.Open);

var docs = schemaSet.Parse(fileStream).ToList();
var nmt = true;
