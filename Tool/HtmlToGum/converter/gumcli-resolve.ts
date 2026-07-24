// @ts-nocheck
import { existsSync, statSync } from 'node:fs';
import { dirname, join } from 'node:path';

const CONFIGS = ['Release', 'Debug'];

/**
 * Gum.Cli is already a project in GumFull.sln, so building the Gum Tool (the ordinary dev
 * workflow, and what CI/packaging already do) produces gumcli.dll as a side effect —
 * there's no reason for the HtmlToGum converter to rebuild it itself on every call.
 * `dotnet run --project ...` re-evaluates the whole MSBuild graph and does an up-to-date
 * check on every single invocation — measured at 7-16s per call versus ~0.3s for invoking
 * an already-built DLL directly (`dotnet <dll> ...`, which skips MSBuild entirely). Prefer
 * whichever prebuilt config is newest; fall back to `dotnet run` (slow, but self-building)
 * only when neither config has ever been built.
 */
export function resolveGumCliEntrypoint(gumCliCsprojPath) {
  const projectDir = dirname(gumCliCsprojPath);
  const candidates = CONFIGS
    .map((config) => join(projectDir, 'bin', config, 'net8.0', 'gumcli.dll'))
    .filter((path) => existsSync(path))
    .map((path) => ({ path, mtimeMs: statSync(path).mtimeMs }));

  if (candidates.length === 0) {
    return { mode: 'run', csprojPath: gumCliCsprojPath };
  }
  candidates.sort((a, b) => b.mtimeMs - a.mtimeMs);
  return { mode: 'dll', dllPath: candidates[0].path };
}
