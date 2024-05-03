using TestsGenerator.App;
using TestsGenerator.Core;

if (args.Length < 2)
{
    Console.WriteLine("Usage: TestsGenerator.App <sourceDirectories> | <sourceFiles> <outputDirectory>");
    return;
}

var files = args[..^1]
    .SelectMany(path => Directory.Exists(path)
        ? Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
        : [path])
    .ToArray();

var outputDirectory = args[^1];

var pipeline = new GenerationPipeline(TestsGeneratorsFactory.CreateNUnitTestsGenerator(), outputDirectory);
await pipeline.GenerateTests(files);
