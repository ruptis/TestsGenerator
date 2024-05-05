using TestsGenerator.App;
using TestsGenerator.Core;

if (args.Length < 2)
{
    Console.WriteLine("Usage: TestsGenerator.App <sourceDirectories> | <sourceFiles> <outputDirectory>");
    return;
}

var maxParallelReadsString = TryGetArgString(args, "--maxParallelReads=");
var maxParallelAnalyzesString = TryGetArgString(args, "--maxParallelAnalyzes=");
var maxParallelWritesString = TryGetArgString(args, "--maxParallelWrites=");

var maxParallelReads = maxParallelReadsString != null ? int.Parse(maxParallelReadsString) : 10;
var maxParallelAnalyzes = maxParallelAnalyzesString != null ? int.Parse(maxParallelAnalyzesString) : 10;
var maxParallelWrites = maxParallelWritesString != null ? int.Parse(maxParallelWritesString) : 10;

var firstOptionIndex = args.FirstOrDefault(arg => arg.StartsWith("--")) is { } option
    ? Array.IndexOf(args, option)
    : args.Length;

var files = args[..(firstOptionIndex-1)]
    .SelectMany(path => Directory.Exists(path)
        ? Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
        : [path])
    .ToArray();

var outputDirectory = args[firstOptionIndex-1];

var pipeline = new GenerationPipeline(
    TestsGeneratorsFactory.CreateNUnitTestsGenerator(),
    outputDirectory,
    maxParallelReads,
    maxParallelWrites,
    maxParallelAnalyzes);

await pipeline.GenerateTests(files);
return;

static string? TryGetArgString(IEnumerable<string> args, string prefix)
{
    var arg = args.FirstOrDefault(arg => arg.StartsWith(prefix));
    return arg?[prefix.Length..];
}