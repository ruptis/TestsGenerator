using System.Threading.Tasks.Dataflow;
using TestsGenerator.Core;
namespace TestsGenerator.App;

public class GenerationPipeline
{
    private readonly string _outputDirectory;
    private readonly TransformBlock<string, string> _readBlock;
    private readonly ActionBlock<IAsyncEnumerable<TestFile>> _writeBlock;

    public GenerationPipeline(Core.TestsGenerator generator, string outputDirectory, int maxParallelReads = 10, int maxParallelWrites = 10, int maxParallelAnalyzes = 10)
    {
        _outputDirectory = outputDirectory;
        
        var readOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = maxParallelReads };
        var writeOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = maxParallelWrites };
        var analyzeOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = maxParallelAnalyzes };
        
        _readBlock = new TransformBlock<string, string>(async path => await File.ReadAllTextAsync(path), readOptions);
        _writeBlock = new ActionBlock<IAsyncEnumerable<TestFile>>(async testFiles => await WriteFiles(testFiles), writeOptions);
        var analyzeBlock = new TransformBlock<string, IAsyncEnumerable<TestFile>>(generator.GenerateTests, analyzeOptions);

        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
        
        _readBlock.LinkTo(analyzeBlock, linkOptions);
        analyzeBlock.LinkTo(_writeBlock, linkOptions);
    }
    
    public async Task GenerateTests(IEnumerable<string> paths)
    {
        foreach (var path in paths)
            await _readBlock.SendAsync(path);
        
        _readBlock.Complete();
        await _writeBlock.Completion;
    }
    
    private async Task WriteFiles(IAsyncEnumerable<TestFile> testFiles)
    {
        await foreach (TestFile testFile in testFiles)
            await File.WriteAllTextAsync(Path.Combine(_outputDirectory, testFile.FileName), testFile.Content);
    }
}