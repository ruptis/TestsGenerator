using TestsGenerator.Core.Models;
namespace TestsGenerator.Core.Analyzer;

public interface ICodeAnalyzer
{
    IAsyncEnumerable<ClassInfo> Analyze(string sourceCode);
}