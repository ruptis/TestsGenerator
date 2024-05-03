using System.Threading.Tasks.Dataflow;
using TestsGenerator.Core;
namespace TestsGenerator.App;

public class GenerationPipeline
{
    private readonly string _outputDirectory;
    private readonly TransformBlock<string, string> _readBlock;
    private readonly ActionBlock<IAsyncEnumerable<TestFile>> _writeBlock;

    public GenerationPipeline(Core.TestsGenerator generator, string outputDirectory, int maxDegreeOfParallelism = 4)
    {
        _outputDirectory = outputDirectory;
        
        var options = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };
        
        _readBlock = new TransformBlock<string, string>(async path => await File.ReadAllTextAsync(path), options);
        _writeBlock = new ActionBlock<IAsyncEnumerable<TestFile>>(async testFiles => await WriteFiles(testFiles), options);
        var analyzeBlock = new TransformBlock<string, IAsyncEnumerable<TestFile>>(generator.GenerateTests, options);

        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
        
        _readBlock.LinkTo(analyzeBlock, linkOptions);
        _readBlock.Completion.ContinueWith(_ => analyzeBlock.Complete());
        analyzeBlock.LinkTo(_writeBlock, linkOptions);
        analyzeBlock.Completion.ContinueWith(_ => _writeBlock.Complete());
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