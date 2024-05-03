using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace TestsGenerator.Core.Analyzer.RoslynAnalyzer;

internal class ClassesCollector : CSharpSyntaxWalker
{
    private readonly List<ClassDeclarationSyntax> _classes = [];

    public IEnumerable<ClassDeclarationSyntax> Classes => _classes;
    public string? FileScopeNamespace { get; private set; }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        _classes.Add(node);
        base.VisitClassDeclaration(node);
    }

    public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
    {
        FileScopeNamespace = node.Name.ToString();
        base.VisitFileScopedNamespaceDeclaration(node);
    }
}
