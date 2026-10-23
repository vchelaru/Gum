# Shared by fast-test.ps1 and verify.ps1: which csprojs are test projects.

# True when the csproj is a test project: IsTestProject true, or a Microsoft.NET.Test.Sdk reference
# (which sets IsTestProject). The name says nothing: MonoGameGum.TestsCommon and SkiaHost.TestsCommon
# are libraries, Gum.Layout.Benchmarks and KniGumInCode.PlaywrightTests are test projects.
function Test-IsTestProject([string]$csproj) {
    $text = Get-Content -Raw -LiteralPath $csproj -ErrorAction SilentlyContinue
    if (-not $text) { return $false }
    return $text -match '<IsTestProject>\s*true\s*</IsTestProject>' -or $text -match 'Include="Microsoft\.NET\.Test\.Sdk"'
}

# The frozen WPF head's tests (CLAUDE.md, ADR-0017): CI does not run them, so they run only when
# named, never because a class name also appears in them.
$frozenTestProjects = @('GumToolUnitTests')
