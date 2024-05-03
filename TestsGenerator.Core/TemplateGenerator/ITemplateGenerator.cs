using TestsGenerator.Core.Models;
namespace TestsGenerator.Core.TemplateGenerator;

public interface ITemplateGenerator
{
    string GenerateTestFileContent(ClassInfo classInfo);
}
