# Fails when a .claude/skills/**/SKILL.md has a CRLF, no frontmatter, or no single-line description.
# Claude Code drops an unquoted "description: ... Triggers: ..." (value contains ": ") from CRLF files, and the
# skill then lists with only its H1 title. .gitattributes forces LF for these files; this catches a checkout that did not.
# Manual (not part of verify.ps1): pwsh Tools/verify-skills.ps1   |   self-test: pwsh Tools/verify-skills.ps1 -SelfTest
param([string]$Root = (Join-Path $PSScriptRoot '..\.claude\skills'), [switch]$SelfTest)

function Test-SkillFile([string]$Path) {
    $raw = [IO.File]::ReadAllText($Path)
    if ($raw.Contains("`r`n")) { return "CRLF line endings" }
    $m = [regex]::Match($raw, '\A---\n(.*?)\n---\n', 'Singleline')
    if (-not $m.Success) { return "no frontmatter" }
    $d = [regex]::Match($m.Groups[1].Value, '(?m)^description:[ \t]*(.*)$')
    if (-not $d.Success) { return "no description" }
    $v = $d.Groups[1].Value.Trim()
    if ($v -eq '' -or $v -match '^[>|]') { return "description is not a single-line string" }
    return $null
}

if ($SelfTest) {
    $tmp = Join-Path ([IO.Path]::GetTempPath()) ("skillselftest" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory $tmp | Out-Null
    $cases = [ordered]@{
        'lf-unquoted'         = @("---`nname: a`ndescription: Thing. Triggers: x, y.`n---`n# A`n", $false)
        'lf-quoted'           = @("---`nname: a`ndescription: `"Thing. Triggers: x.`"`n---`n# A`n", $false)
        'crlf-unquoted'       = @("---`r`nname: a`r`ndescription: Thing. Triggers: x, y.`r`n---`r`n# A`r`n", $true)
        'crlf-folded'         = @("---`r`nname: a`r`ndescription: >`r`n  Thing. Triggers: x.`r`n---`r`n# A`r`n", $true)
        'crlf-quoted'         = @("---`r`nname: a`r`ndescription: `"Thing. Triggers: x.`"`r`n---`r`n# A`r`n", $true)
        'no-frontmatter'      = @("# A`nbody`n", $true)
        'missing-description' = @("---`nname: a`n---`n# A`n", $true)
    }
    $fail = 0
    foreach ($k in $cases.Keys) {
        $p = Join-Path $tmp "$k.md"
        [IO.File]::WriteAllText($p, $cases[$k][0])
        $failed = $null -ne (Test-SkillFile $p)
        if ($failed -ne $cases[$k][1]) { Write-Host "SELFTEST WRONG: $k (expected fail=$($cases[$k][1]))"; $fail++ }
    }
    Remove-Item -Recurse -Force $tmp
    if ($fail) { exit 1 }
    Write-Host 'Self-test OK'
    exit 0
}

$bad = 0
foreach ($f in Get-ChildItem -Path $Root -Filter SKILL.md -Recurse) {
    $err = Test-SkillFile $f.FullName
    if ($err) { Write-Host "${err}: $($f.FullName)"; $bad++ }
}
if ($bad) { Write-Host "$bad skill(s) failed"; exit 1 }
Write-Host 'All skill descriptions OK'
