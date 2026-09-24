<#
.SYNOPSIS
	Fails if a packed .nupkg ships a lib assembly whose AssemblyVersion doesn't match the package version.

.DESCRIPTION
	The release workflow passes -p:Version to build and pack, so every assembly and every package in a
	wave carries the same version. Anything that rebuilds a project without that property (a nested
	`dotnet build` from a target, a stale bin folder, a partial rebuild) drops back to the csproj's
	literal <Version> and overwrites the assembly the rest of the build already compiled against.
	Pack is --no-build, so the mismatched file ships, and consumers get a FileNotFoundException for a
	version that exists nowhere. That is how GumCommon.dll 2026.4.10.1 shipped inside
	FlatRedBall.GumCommon 2026.9.24.1-preview.1 (#4974).

	Assemblies stamped 0.0.0.0 are skipped: RaylibGum and KniGum set GenerateAssemblyInfo=false, so
	they carry no version attributes at all and there is nothing to compare.

.PARAMETER PackageDirectory
	Folder holding the .nupkg files to check. Defaults to ./nupkgs.
#>
[CmdletBinding()]
param(
	[string]$PackageDirectory = './nupkgs'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.IO.Compression.FileSystem

if (-not (Test-Path $PackageDirectory)) {
	throw "Package directory '$PackageDirectory' does not exist."
}

# 2026.9.24.1-preview.1 -> 2026.9.24.1, and 1.2.3 -> 1.2.3.0, so it compares against a 4-part AssemblyVersion.
function ConvertTo-AssemblyVersion([string]$packageVersion) {
	$numeric = ($packageVersion -split '[-+]')[0]
	$parts = @($numeric -split '\.')
	while ($parts.Count -lt 4) { $parts += '0' }
	return [version]::Parse(($parts[0..3] -join '.'))
}

$packages = @(Get-ChildItem -Path $PackageDirectory -Filter '*.nupkg' |
	Where-Object { $_.Name -notlike '*.snupkg' } | Sort-Object Name)

if ($packages.Count -eq 0) {
	throw "No .nupkg files found in '$PackageDirectory'."
}

$scratch = Join-Path ([System.IO.Path]::GetTempPath()) ("gum-pkg-check-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $scratch | Out-Null

$mismatches = @()
$checked = 0

try {
	foreach ($package in $packages) {
		$archive = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)
		try {
			$nuspecEntry = $archive.Entries | Where-Object { $_.FullName -notlike '*/*' -and $_.FullName -like '*.nuspec' } | Select-Object -First 1
			if ($null -eq $nuspecEntry) {
				$mismatches += [pscustomobject]@{ Package = $package.Name; Assembly = '(nuspec)'; Expected = ''; Actual = 'no .nuspec in package' }
				continue
			}

			$reader = New-Object System.IO.StreamReader($nuspecEntry.Open())
			try { $nuspecXml = [xml]$reader.ReadToEnd() } finally { $reader.Dispose() }

			$packageVersion = $nuspecXml.package.metadata.version
			$expected = ConvertTo-AssemblyVersion $packageVersion

			foreach ($entry in $archive.Entries) {
				if ($entry.FullName -notlike 'lib/*' -or $entry.FullName -notlike '*.dll') { continue }

				$extracted = Join-Path $scratch ([guid]::NewGuid().ToString('N') + '.dll')
				[System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $extracted, $true)

				try {
					$actual = [System.Reflection.AssemblyName]::GetAssemblyName($extracted).Version
				} catch {
					# Not a managed assembly (a native lib riding along); nothing to compare.
					Remove-Item $extracted -Force
					continue
				}
				Remove-Item $extracted -Force

				# GenerateAssemblyInfo=false projects carry no version attributes.
				if ($actual -eq [version]'0.0.0.0') { continue }

				$checked++
				if ($actual -ne $expected) {
					$mismatches += [pscustomobject]@{
						Package  = "$($package.Name.Replace('.nupkg', ''))"
						Assembly = $entry.FullName
						Expected = $expected.ToString()
						Actual   = $actual.ToString()
					}
				}
			}
		} finally {
			$archive.Dispose()
		}
	}
} finally {
	Remove-Item $scratch -Recurse -Force -ErrorAction SilentlyContinue
}

if ($mismatches.Count -gt 0) {
	Write-Host "Packed assemblies do not match their package version:" -ForegroundColor Red
	$mismatches | Format-Table -AutoSize | Out-String | Write-Host
	Write-Host "A project was rebuilt without the release -p:Version and overwrote the assembly the rest of the build compiled against. Do not publish this wave; see #4974."
	exit 1
}

Write-Host "$checked packed assemblies across $($packages.Count) packages match their package version."
