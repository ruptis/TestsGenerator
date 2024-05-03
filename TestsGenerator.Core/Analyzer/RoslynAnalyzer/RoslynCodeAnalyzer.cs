using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TestsGenerator.Core.Models;
namespace TestsGenerator.Core.Analyzer.RoslynAnalyzer;

internal class RoslynCodeAnalyzer : ICodeAnalyzer
{
    public async IAsyncEnumerable<ClassInfo> Analyze(string sourceCode)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        SyntaxNode root = await syntaxTree.GetRootAsync();

        var classesCollector = new ClassesCollector();

        classesCollector.Visit(root);

        foreach (ClassDeclarationSyntax classDeclaration in classesCollector.Classes)
        {
            var namespaceName = classesCollector.FileScopeNamespace ??
                classDeclaration.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString();
            var className = classDeclaration.Identifier.ToString();

            var constructor = TryGetPrimaryConstructorInfo(classDeclaration) ?? TryGetConstructorInfo(classDeclaration);
            var methods = GetMethodsInfo(classDeclaration);

            yield return new ClassInfo(namespaceName, className, constructor, methods);
        }
    }

    private ConstructorInfo? TryGetConstructorInfo(ClassDeclarationSyntax classDeclaration)
    {
        ConstructorDeclarationSyntax? constructorDeclaration = classDeclaration
            .DescendantNodes()
            .OfType<ConstructorDeclarationSyntax>()
            .FirstOrDefault();

        return constructorDeclaration != null && constructorDeclaration.ParameterList.Parameters.All(p => p.Type != null && IsInterfaceIdentifier(p.Type.ToString()))
            ? new ConstructorInfo(GetParametersInfo(constructorDeclaration.ParameterList.Parameters))
            : null;
    }
    
    private ConstructorInfo? TryGetPrimaryConstructorInfo(ClassDeclarationSyntax classDeclaration)
    {
        ParameterListSyntax? parameterList = classDeclaration
            .DescendantNodes()
            .OfType<ParameterListSyntax>()
            .FirstOrDefault();
        
        return parameterList != null && parameterList.Parameters.All(p => p.Type != null && IsInterfaceIdentifier(p.Type.ToString()))
            ? new ConstructorInfo(GetParametersInfo(parameterList.Parameters))
            : null;
    }

    private bool IsInterfaceIdentifier(string identifier) =>
        identifier.StartsWith('I') && identifier.Length > 1 && char.IsUpper(identifier[1]);

    private IEnumerable<MethodInfo> GetMethodsInfo(ClassDeclarationSyntax classDeclaration) => classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>().Select(methodDeclaration =>
    {
        var methodName = methodDeclaration.Identifier.ToString();
        var returnType = methodDeclaration.ReturnType.ToString();
        var parameters = GetParametersInfo(methodDeclaration.ParameterList.Parameters);
        return new MethodInfo(methodName, returnType, parameters);
    });

    private IEnumerable<ParameterInfo> GetParametersInfo(IEnumerable<ParameterSyntax> parameters) =>
        parameters.Select(parameter => new ParameterInfo(parameter.Type?.ToString() ?? string.Empty, parameter.Identifier.ToString()));
}
