using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TestsGenerator.Core.Models;
namespace TestsGenerator.Core.TemplateGenerator.NUnit;

using static SyntaxFactory;
internal class NUnitTemplateGenerator : ITemplateGenerator
{
    private const string MoqUsing = "Moq";
    private const string MockClassName = "Mock";
    private const string MockObjectProperty = "Object";

    private const string NUnitUsing = "NUnit.Framework";
    private const string ClassAttribute = "TestFixture";
    private const string SetUpAttribute = "SetUp";
    private const string TestAttribute = "Test";

    private const string SetUpMethodName = "SetUp";

    private const string Actual = "actual";
    private const string Expected = "expected";

    public string GenerateTestFileContent(ClassInfo classInfo)
    {
        SyntaxTree tree = CSharpSyntaxTree.Create(GenerateCompilationUnit(classInfo));
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
        return root.ToFullString();
    }

    private static CompilationUnitSyntax GenerateCompilationUnit(ClassInfo classInfo) => CompilationUnit()
        .WithUsings(GetUsings(classInfo.Namespace))
        .WithMembers(SingletonList(GenerateNamespace(classInfo)))
        .NormalizeWhitespace();

    private static SyntaxList<UsingDirectiveSyntax> GetUsings(string? namespaceName)
    {
        var usings = GetDefaultUsings();
        return namespaceName != null ? usings.Add(UsingDirective(ParseName(namespaceName))) : usings;
    }

    private static SyntaxList<UsingDirectiveSyntax> GetDefaultUsings() => List(new[]
    {
        UsingDirective(IdentifierName("System")),
        UsingDirective(IdentifierName("System.Collections.Generic")),
        UsingDirective(IdentifierName("System.Linq")),
        UsingDirective(IdentifierName("System.Text")),
        UsingDirective(IdentifierName("System.Threading.Tasks")),
        UsingDirective(IdentifierName(MoqUsing)),
        UsingDirective(IdentifierName(NUnitUsing))
    });

    private static MemberDeclarationSyntax GenerateNamespace(ClassInfo classInfo) =>
        FileScopedNamespaceDeclaration(IdentifierName(GetNamespaceName(classInfo)))
            .WithMembers(SingletonList(GenerateClass(classInfo)));

    private static string GetNamespaceName(ClassInfo classInfo) =>
        classInfo.Namespace != null ? $"{classInfo.Namespace}.Tests" : $"{classInfo.Name}.Tests";

    private static MemberDeclarationSyntax GenerateClass(ClassInfo classInfo) => ClassDeclaration($"{classInfo.Name}Tests")
        .WithAttributeLists(GetClassAttribute())
        .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
        .WithMembers(List(GenerateClassMembers(classInfo)));

    private static SyntaxList<AttributeListSyntax> GetClassAttribute() => SingletonList(
        AttributeList(SingletonSeparatedList(Attribute(IdentifierName(ClassAttribute)))));

    private static IEnumerable<MemberDeclarationSyntax> GenerateClassMembers(ClassInfo classInfo)
    {
        if (classInfo.Constructor != null)
        {
            yield return GenerateClassField(classInfo.Name);

            foreach (ParameterInfo parameter in classInfo.Constructor.Value.Parameters)
                yield return GenerateMockField(parameter.Type, parameter.Name);

            yield return GenerateSetupMethod(classInfo.Name, classInfo.Constructor.Value.Parameters);
        }

        foreach (MethodInfo method in classInfo.Methods)
            yield return GenerateTestMethod(method, classInfo.Constructor != null ? ToPrivateFieldName(classInfo.Name) : null);
    }

    private static MemberDeclarationSyntax GenerateClassField(string className) => FieldDeclaration(
            VariableDeclaration(IdentifierName(className))
                .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier(ToPrivateFieldName(className))))))
        .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)));

    private static MemberDeclarationSyntax GenerateMockField(string className, string fieldName) => FieldDeclaration(
            VariableDeclaration(GenerateMockClassType(className))
                .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier(ToPrivateFieldName(fieldName))))))
        .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)));

    private static GenericNameSyntax GenerateMockClassType(string className) => GenericName(Identifier(MockClassName))
        .WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList<TypeSyntax>(IdentifierName(className))));

    private static string ToPrivateFieldName(string fieldName) => $"_{fieldName[..1].ToLower()}{fieldName[1..]}";

    private static MemberDeclarationSyntax GenerateSetupMethod(string className, IEnumerable<ParameterInfo> parameters) => MethodDeclaration(
            PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier(SetUpMethodName))
        .WithAttributeLists(SingletonList(
            AttributeList(SingletonSeparatedList(Attribute(IdentifierName(SetUpAttribute))))))
        .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
        .WithBody(Block(GenerateSetupMethodBody(className, parameters)));

    private static IEnumerable<StatementSyntax> GenerateSetupMethodBody(string className, IEnumerable<ParameterInfo> parameters)
    {
        var parametersArray = parameters as ParameterInfo[] ?? parameters.ToArray();

        foreach (ParameterInfo parameter in parametersArray)
            yield return GenerateParameterMockInitialization(parameter);

        yield return GenerateObjectInitialization(className, parametersArray);
    }

    private static ExpressionStatementSyntax GenerateParameterMockInitialization(ParameterInfo parameter) => ExpressionStatement(
        AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            IdentifierName(ToPrivateFieldName(parameter.Name)),
            ObjectCreationExpression(GenerateMockClassType(parameter.Type))
                .WithArgumentList(ArgumentList())));

    private static ExpressionStatementSyntax GenerateObjectInitialization(string className, IEnumerable<ParameterInfo> parameters) => ExpressionStatement(
        AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            IdentifierName(ToPrivateFieldName(className)),
            ObjectCreationExpression(IdentifierName(className))
                .WithArgumentList(ArgumentList(
                    SeparatedList(
                        parameters.Select(p => Argument(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(ToPrivateFieldName(p.Name)),
                                IdentifierName(MockObjectProperty)))))))));

    private static MemberDeclarationSyntax GenerateTestMethod(MethodInfo method, string? fieldName) => MethodDeclaration(
            PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier($"{method.Name}Test"))
        .WithAttributeLists(SingletonList(
            AttributeList(SingletonSeparatedList(Attribute(IdentifierName(TestAttribute))))))
        .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
        .WithBody(Block(GenerateTestMethodBody(method, fieldName)));

    private static IEnumerable<StatementSyntax> GenerateTestMethodBody(MethodInfo method, string? fieldName)
    {
        if (fieldName != null)
        {
            foreach (ParameterInfo parameter in method.Parameters)
                yield return GenerateLocalVariableDeclaration(parameter.Type, parameter.Name);
            

            if (method.ReturnType == "void")
            {
                yield return GenerateVoidMethodInvocation(method, fieldName);
            }
            else
            {
                yield return GenerateReturnMethodInvocation(method, fieldName);
                yield return GenerateLocalVariableDeclaration(method.ReturnType, Expected);
                yield return GenerateAssertEqual(Actual, Expected);
            }
        }

        yield return GenerateFailAssert();
    }

    private static LocalDeclarationStatementSyntax GenerateLocalVariableDeclaration(string type, string name) => LocalDeclarationStatement(
        VariableDeclaration(IdentifierName(type))
            .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier(name))
                .WithInitializer(EqualsValueClause(LiteralExpression(SyntaxKind.DefaultLiteralExpression))))));

    private static ExpressionStatementSyntax GenerateVoidMethodInvocation(MethodInfo method, string fieldName) => ExpressionStatement(
        GenerateMethodInvocation(method, fieldName));

    private static LocalDeclarationStatementSyntax GenerateReturnMethodInvocation(MethodInfo method, string fieldName) => LocalDeclarationStatement(VariableDeclaration(IdentifierName(method.ReturnType))
        .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier(Actual))
            .WithInitializer(EqualsValueClause(GenerateMethodInvocation(method, fieldName))))));

    private static InvocationExpressionSyntax GenerateMethodInvocation(MethodInfo method, string fieldName) => InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName(fieldName),
                IdentifierName(method.Name)))
        .WithArgumentList(ArgumentList(SeparatedList(method.Parameters.Select(p => Argument(IdentifierName(p.Name))))));

    private static ExpressionStatementSyntax GenerateAssertEqual(string actual, string expected) => ExpressionStatement(
        InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("Assert"),
                    IdentifierName("That")))
            .WithArgumentList(ArgumentList(SeparatedList(new[]
            {
                Argument(IdentifierName(actual)),
                Argument(InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("Is"),
                            IdentifierName("EqualTo")))
                    .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(IdentifierName(expected))))))
            }))));

    private static ExpressionStatementSyntax GenerateFailAssert() => ExpressionStatement(
        InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("Assert"),
                    IdentifierName("Fail")))
            .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal("Autogenerated")))))));
}
