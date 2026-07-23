// @ts-nocheck
// Thin wrapper: dual-viewport A/B with an explicit training pair + naive control.
// Prefer convert.ts directly — responsive is now the default there.
//   npx tsx convert.ts <html> <sel> <screen> <w> <h> --responsive=<narrow>,<wide> --compare-naive
import { spawnSync } from 'node:child_process';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { nodeTsxArgs } from './tsx-run.js';

const __dirname = dirname(fileURLToPath(import.meta.url));
const html = process.argv[2] || 'input/responsive-sidebar.html';
const sel = process.argv[3] || '#layout';
const screen = process.argv[4] || 'ResponsiveScreen';
const narrow = process.argv[5] || '400';
const wide = process.argv[6] || '1200';
const h = process.argv[7] || '400';

const r = spawnSync(process.execPath, nodeTsxArgs(
  join(__dirname, 'convert.ts'), html, sel, screen, wide, h,
  `--responsive=${narrow},${wide}`, '--compare-naive', `--tag=${screen}`,
), { cwd: __dirname, stdio: 'inherit' });
process.exit(r.status ?? 1);
