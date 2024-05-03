namespace TestsGenerator.Core.Models;

public record struct ClassInfo(string? Namespace, string Name, ConstructorInfo? Constructor, IEnumerable<MethodInfo> Methods);
