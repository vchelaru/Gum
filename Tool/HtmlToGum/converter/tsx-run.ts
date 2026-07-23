// Resolve the local tsx CLI so harnesses can spawn TypeScript entrypoints
// without requiring a global install (`node convert.ts` would fail).
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));

/** Absolute path to this package's `tsx` CLI entry. */
export const tsxCli = join(__dirname, 'node_modules', 'tsx', 'dist', 'cli.mjs');

/** Args for `spawnSync(process.execPath, nodeTsxArgs(script, ...flags))`. */
export function nodeTsxArgs(scriptPath: string, ...scriptArgs: string[]): string[] {
  return [tsxCli, scriptPath, ...scriptArgs];
}
