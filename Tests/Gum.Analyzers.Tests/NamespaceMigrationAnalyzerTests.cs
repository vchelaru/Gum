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
    public async Task EmptyMappingTable_NoDiagnostics()
    {
        // With an empty mapping table (Phase 1 state), the analyzer should never fire,
        // even on code that uses namespaces which will eventually be migrated.
        // We use only System namespaces here to avoid compiler errors in the test harness.
        string testCode = @"
using System;
using System.Collections.Generic;

namespace TestProject
{
    class MyClass { }
}
";

        // This should produce no diagnostics since the mapping table is empty
        await AnalyzerVerifier.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task UnrelatedUsing_NoDiagnostics()
    {
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

    [Fact]
    public void MappingTable_IsEmpty_InPhase1()
    {
        // Verify our Phase 1 assumption: no migrations are registered yet
        Assert.True(NamespaceMigrationMapping.Migrations.IsEmpty,
            "Migration table should be empty during Phase 1. " +
            "Entries should only be added when types are actually moved.");
    }

    [Fact]
    public void MappingTable_ByOldNamespace_IsEmpty_InPhase1()
    {
        Assert.True(NamespaceMigrationMapping.ByOldNamespace.IsEmpty,
            "ByOldNamespace lookup should be empty during Phase 1.");
    }
}
