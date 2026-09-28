using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DeadshotModAPI.Tests.TestHelpers;

/// <summary>
/// Dynamically compiles test C# code into genuine .NET assembly DLLs for ModLoader testing.
/// </summary>
public static class TestAssemblyBuilder
{
    /// <summary>
    /// Compiles the provided C# source code into a DLL assembly at the specified path.
    /// </summary>
    public static string CompileAssembly(string sourceCode, string outputPath, string assemblyName = "DynamicTestMod")
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IDeadshotMod).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Action).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release)
        );

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var peStream = File.Create(outputPath);
        var emitResult = compilation.Emit(peStream);

        if (!emitResult.Success)
        {
            var errors = string.Join(Environment.NewLine, emitResult.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.GetMessage()));
            throw new InvalidOperationException($"Dynamic assembly compilation failed: {errors}");
        }

        return outputPath;
    }
}
