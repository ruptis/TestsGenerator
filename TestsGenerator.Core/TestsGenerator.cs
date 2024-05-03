using TestsGenerator.Core.Analyzer;
using TestsGenerator.Core.Models;
using TestsGenerator.Core.TemplateGenerator;
namespace TestsGenerator.Core;

public class TestsGenerator(ICodeAnalyzer codeAnalyzer, ITemplateGenerator templateGenerator)
{
    public async IAsyncEnumerable<TestFile> GenerateTests(string sourceCode)
    {
        var classes = codeAnalyzer.Analyze(sourceCode);

        await foreach (ClassInfo classInfo in classes)
        {
            var testFileContent = templateGenerator.GenerateTestFileContent(classInfo);
            var testFileName = $"{classInfo.Name}Tests.cs";
            yield return new TestFile(testFileName, testFileContent);
        }
    }
}
