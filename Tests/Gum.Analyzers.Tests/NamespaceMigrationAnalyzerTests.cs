using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using AnalyzerVerifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Gum.Analyzers.NamespaceMigrationAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Gum.Analyzers.Tests;

public class NamespaceMigrationAnalyzerTests
{
    [Fact]
    public async Task UnrelatedUsing_NoDiagnostics()
    {
        // Using directives that aren't in the migration table should never raise GUM001.
        string testCode = @"
using System;
using System.Collections.Generic;

namespace TestProject
{
    class MyClass { }
}
";

        await AnalyzerVerifier.VerifyAnalyzerAsync(testCode);
    }
}
