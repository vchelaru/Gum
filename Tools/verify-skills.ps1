# Fails when a .claude/skills/*/SKILL.md has no usable description.
# Claude Code silently drops an unquoted "description: ... Triggers: ..." (value contains ": ") when the file
# has CRLF line endings, and the skill then lists with only its H1 title. Fix: LF (see .gitattributes) or quote the value.
param([string]$Root = (Join-Path $PSScriptRoot '..\.claude\skills'))
$bad = 0
foreach ($f in Get-ChildItem -Path $Root -Filter SKILL.md -Recurse) {
    $raw = [IO.File]::ReadAllText($f.FullName)
    $m = [regex]::Match($raw, '\A---\r?\n(.*?)\r?\n---\r?\n', 'Singleline')
    if (-not $m.Success) { Write-Host "NO FRONTMATTER: $($f.FullName)"; $bad++; continue }
    $d = [regex]::Match($m.Groups[1].Value, '(?m)^description:[ \t]*(.*?)\r?$')
    if (-not $d.Success -or $d.Groups[1].Value.Trim() -eq '') { Write-Host "NO DESCRIPTION (single line required): $($f.FullName)"; $bad++; continue }
    $v = $d.Groups[1].Value.Trim()
    $quoted = $v.StartsWith('"') -or $v.StartsWith("'")
    if (-not $quoted -and $v.Contains(': ') -and $m.Groups[1].Value.Contains("`r")) {
        Write-Host "CRLF + unquoted ': ' in description (listed without description): $($f.FullName)"; $bad++
    }
}
if ($bad) { Write-Host "$bad skill(s) failed"; exit 1 }
Write-Host 'All skill descriptions OK'
