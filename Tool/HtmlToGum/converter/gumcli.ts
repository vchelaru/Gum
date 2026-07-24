#!/usr/bin/env node
// @ts-nocheck
// Thin wrapper around Gum.Cli so convert/regress don't hard-code `dotnet run` flags.
// Usage: npx tsx gumcli.ts fonts <project.gumx>
//        npx tsx gumcli.ts check <project.gumx>
//        npx tsx gumcli.ts new <project.gumx> --template empty
import { spawnSync } from 'node:child_process';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { existsSync } from 'node:fs';
import { resolveGumCliEntrypoint } from './gumcli-resolve.js';

const __dirname = dirname(fileURLToPath(import.meta.url));
const inRepoGumCliCsproj = resolve(__dirname, '..', '..', '..', 'Tools', 'Gum.Cli', 'Gum.Cli.csproj');
const legacyGumCliCsproj = resolve(__dirname, '..', '..', 'Gum', 'Tools', 'Gum.Cli', 'Gum.Cli.csproj');
const gumCliCsproj = existsSync(inRepoGumCliCsproj) ? inRepoGumCliCsproj : legacyGumCliCsproj;

if (!existsSync(gumCliCsproj)) {
  console.error(`gumcli project not found at ${gumCliCsproj}`);
  process.exit(1);
}

const [cmd, ...rest] = process.argv.slice(2);
if (!cmd || !['fonts', 'check', 'new'].includes(cmd)) {
  console.error('Usage: gumcli.ts <fonts|check|new> <project.gumx> [extra args…]');
  process.exit(1);
}

const entrypoint = resolveGumCliEntrypoint(gumCliCsproj);
const result = entrypoint.mode === 'dll'
  ? spawnSync('dotnet', [entrypoint.dllPath, cmd, ...rest], { stdio: 'inherit', shell: true })
  : (() => {
    // Gum.Cli is part of GumFull.sln — building the Gum Tool normally already produces
    // gumcli.dll. This path only runs before that's ever happened, and `dotnet run` pays
    // a full MSBuild re-evaluation (several seconds) on every call as a result.
    console.warn('gumcli.dll not found in Tools/Gum.Cli/bin — falling back to `dotnet run` (slow). Build GumFull.sln (or Tools/Gum.Cli/Gum.Cli.csproj) once to skip this.');
    return spawnSync('dotnet', ['run', '--project', entrypoint.csprojPath, '--', cmd, ...rest], { stdio: 'inherit', shell: true });
  })();
process.exit(result.status ?? 1);
