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

const result = spawnSync(
  'dotnet',
  ['run', '--project', gumCliCsproj, '--', cmd, ...rest],
  { stdio: 'inherit', shell: true },
);
process.exit(result.status ?? 1);
