namespace TestsGenerator.Core.Models;

public record struct MethodInfo(string Name, string ReturnType, IEnumerable<ParameterInfo> Parameters);
