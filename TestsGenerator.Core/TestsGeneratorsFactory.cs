using TestsGenerator.Core.Analyzer.RoslynAnalyzer;
using TestsGenerator.Core.TemplateGenerator.NUnit;
namespace TestsGenerator.Core;

public static class TestsGeneratorsFactory
{
    public static TestsGenerator CreateNUnitTestsGenerator() => new(new RoslynCodeAnalyzer(), new NUnitTemplateGenerator());
}
