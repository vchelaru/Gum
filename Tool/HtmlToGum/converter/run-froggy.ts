// @ts-nocheck
// Flexbox Froggy batch: emit solution HTML → convert → MonoGame → pixel-diff.
// Levels from research/froggy/levels.js (thomaspark/flexboxfroggy). Layout-only frogs
// (solid color boxes) so residuals measure flex mapping, not SVG assets.
//
// Usage: npx tsx run-froggy.ts [--only=1,5,18] [--max-pct=2]
import { spawnSync } from 'node:child_process';
import { createRequire } from 'node:module';
import { resolve, dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  mkdirSync, writeFileSync, readFileSync, existsSync, copyFileSync,
} from 'node:fs';
import { createContext, runInContext } from 'node:vm';
import { nodeTsxArgs } from './tsx-run.js';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '..');
const froggyDir = join(repoRoot, 'research', 'froggy');
const levelsHtmlDir = join(froggyDir, 'levels');
const outDir = join(froggyDir, 'out');
const hostDir = join(repoRoot, 'host');
const convertTs = join(__dirname, 'convert.ts');

const COLORS = { g: 'green', r: 'red', y: 'yellow' };
const HEX = { green: '#78ba45', red: '#e74c3c', yellow: '#f1c40f' };
const W = 500;
const H = 500;

function parseFlags(argv) {
  let only = null;
  let maxPct = 2;
  for (const a of argv) {
    if (a.startsWith('--only=')) {
      only = new Set(a.slice('--only='.length).split(',').map((s) => parseInt(s, 10)));
    } else if (a.startsWith('--max-pct=')) {
      maxPct = parseFloat(a.slice('--max-pct='.length));
    }
  }
  return { only, maxPct };
}

function loadLevels() {
  const src = readFileSync(join(froggyDir, 'levels.js'), 'utf8');
  const ctx = createContext({});
  runInContext(`${src}\nthis.levels = levels;`, ctx);
  if (!Array.isArray(ctx.levels) || ctx.levels.length === 0) {
    throw new Error('failed to parse research/froggy/levels.js');
  }
  return ctx.levels;
}

function featureTags(level) {
  const tags = new Set();
  const style = level.style || {};
  for (const [k, v] of Object.entries(style)) {
    tags.add(k);
    if (typeof v === 'string') {
      for (const part of v.split(/\s+/)) tags.add(`${k}:${part}`);
    }
  }
  if (level.selector) tags.add('item-selector');
  if (level.classes) tags.add('wrap-class');
  const before = level.before || '';
  if (/flex-wrap/.test(before)) tags.add('flex-wrap');
  if (/align-items/.test(before)) tags.add('align-items(base)');
  return [...tags];
}

function buildHtml(level) {
  const frogs = [...level.board].map((c) => {
    const color = COLORS[c] || 'green';
    return `  <div class="frog ${color}"></div>`;
  }).join('\n');

  const codeLines = Object.entries(level.style || {})
    .map(([k, v]) => `  ${k}: ${v};\n`)
    .join('');
  const editorCss = `${level.before || ''}${codeLines}${level.after || ''}`;

  let wrapClass = '';
  if (level.classes) {
    for (const [rule, cls] of Object.entries(level.classes)) {
      if (rule.includes('#pond') && String(cls).split(/\s+/).includes('wrap')) {
        wrapClass = ' wrap';
      }
    }
  }

  return `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>Froggy — ${level.name}</title>
<style>
* { box-sizing: border-box; margin: 0; padding: 0; }
html, body {
  width: ${W}px;
  height: ${H}px;
  background: #1F5768;
  overflow: hidden;
}
#pond {
  display: flex;
  width: ${W}px;
  height: ${H}px;
  padding: 1em;
  background: #1F5768;
}
.frog {
  position: relative;
  width: 20%;
  height: 20%;
  overflow: hidden;
}
.frog.green { background: ${HEX.green}; }
.frog.red { background: ${HEX.red}; }
.frog.yellow { background: ${HEX.yellow}; }
.wrap { flex-wrap: wrap; }

/* Froggy editor solution (before + style + after) */
${editorCss}
</style>
</head>
<body>
<div id="pond" class="${wrapClass.trim()}">
${frogs}
</div>
</body>
</html>
`;
}

function run(cmd, args, cwd) {
  console.log(`\n> ${cmd} ${args.join(' ')}`);
  const r = spawnSync(cmd, args, { cwd, encoding: 'utf8', shell: false, maxBuffer: 32 * 1024 * 1024 });
  if (r.stdout) process.stdout.write(r.stdout);
  if (r.stderr) process.stderr.write(r.stderr);
  if (r.status !== 0) throw new Error(`${cmd} exited ${r.status}`);
  return r;
}

function pixelDiff(chromiumPath, gumPath, crop, sidePath) {
  const script = `
from PIL import Image, ImageChops
import json, sys
chrom = Image.open(sys.argv[1]).convert('RGB')
gum = Image.open(sys.argv[2]).convert('RGB')
x, y = int(sys.argv[4]), int(sys.argv[5])
w, h = chrom.size
gum_c = gum.crop((x, y, x + w, y + h))
if gum_c.size != chrom.size:
    print(json.dumps({"error": f"size {gum_c.size} vs {chrom.size}"}))
    sys.exit(0)
d = ImageChops.difference(gum_c, chrom)
diff = sum(1 for p in d.getdata() if p != (0, 0, 0))
total = w * h
side = Image.new('RGB', (w * 2, h))
side.paste(chrom, (0, 0))
side.paste(gum_c, (w, 0))
side.save(sys.argv[3])
print(json.dumps({"diff": diff, "total": total, "pct": round(100.0 * diff / total, 3), "w": w, "h": h}))
`;
  const r = spawnSync('python', [
    '-c', script, chromiumPath, gumPath, sidePath, String(crop.x), String(crop.y),
  ], { encoding: 'utf8', maxBuffer: 32 * 1024 * 1024 });
  if (r.status !== 0) throw new Error(`pixelDiff failed: ${r.stderr || r.stdout}`);
  return JSON.parse(r.stdout.trim());
}

function triageBucket(tags, pct, maxPct) {
  if (pct <= maxPct) return 'pass';
  // Prefer the most specific gap (order before wrap-class noise on L14/L15).
  if (tags.some((t) => t === 'order' || t.startsWith('order:') || t === 'align-self' || t.startsWith('align-self:'))) {
    return 'order/align-self';
  }
  if (tags.some((t) => t.startsWith('flex-wrap') || t.startsWith('align-content') || t.startsWith('flex-flow'))) {
    return 'wrap/align-content';
  }
  if (tags.some((t) => t === 'wrap-class')) return 'wrap/align-content';
  if (tags.some((t) => /reverse/.test(t))) return 'direction-reverse';
  if (tags.some((t) => t.startsWith('flex-direction') || t.startsWith('justify-content') || t.startsWith('align-items'))) {
    return 'flex basics';
  }
  return 'other';
}

const { only, maxPct } = parseFlags(process.argv.slice(2));
const levels = loadLevels();

mkdirSync(levelsHtmlDir, { recursive: true });
mkdirSync(outDir, { recursive: true });

const results = [];
let failed = 0;

for (let i = 0; i < levels.length; i++) {
  const n = i + 1;
  if (only && !only.has(n)) continue;
  const level = levels[i];
  const id = `L${String(n).padStart(2, '0')}`;
  const tag = `froggy-${id}`;
  const screen = `Froggy${id}Screen`;
  const tags = featureTags(level);
  const htmlRel = `../research/froggy/levels/${id}.html`;
  const htmlAbs = join(levelsHtmlDir, `${id}.html`);

  writeFileSync(htmlAbs, buildHtml(level));
  console.log(`\n========== ${id} ${level.name} ==========`);
  console.log(`  board=${level.board} style=${JSON.stringify(level.style)}`);
  console.log(`  tags: ${tags.join(', ')}`);

  const levelOut = join(outDir, id);
  mkdirSync(levelOut, { recursive: true });

  try {
    run(process.execPath, nodeTsxArgs(
      convertTs,
      htmlRel, '#pond', screen, String(W), String(H),
      `--tag=${tag}`,
      '--no-responsive',
    ), __dirname);

    const chromSrc = join(repoRoot, '.regress', `chromium-${tag}.png`);
    if (!existsSync(chromSrc)) throw new Error(`missing ${chromSrc}`);
    copyFileSync(chromSrc, join(levelOut, 'chromium.png'));

    const tree = JSON.parse(readFileSync(
      join(hostDir, 'Content', 'generated', 'boxtree.json'), 'utf8'));
    const crop = {
      x: Math.max(0, Math.floor(tree.rect.x)),
      y: Math.max(0, Math.floor(tree.rect.y)),
    };

    const gumFull = join(levelOut, 'gum-full.png');
    run('dotnet', [
      'run', '-c', 'Release', '--project', join(hostDir, 'HtmlToGumHost.csproj'),
      '--', gumFull, 'generated/Generated.gumx', screen, String(W), String(H),
    ], hostDir);

    const stats = pixelDiff(chromSrc, gumFull, crop, join(levelOut, 'side.png'));
    if (stats.error) throw new Error(stats.error);

    const ok = stats.pct <= maxPct;
    const bucket = triageBucket(tags, stats.pct, maxPct);
    if (!ok) failed++;
    console.log(`  → ${stats.pct}% (${stats.diff}/${stats.total}) [${ok ? 'PASS' : 'FAIL'}] bucket=${bucket}`);

    results.push({
      id, n, name: level.name, board: level.board, style: level.style,
      selector: level.selector || '', tags, ...stats, maxPct,
      status: ok ? 'pass' : 'fail', bucket, crop,
    });
  } catch (err) {
    failed++;
    console.error(`  ERROR: ${err.message || err}`);
    results.push({
      id, n, name: level.name, board: level.board, style: level.style,
      selector: level.selector || '', tags, status: 'error',
      bucket: 'error', error: String(err.message || err),
    });
  }
}

writeFileSync(join(froggyDir, 'results.json'), JSON.stringify(results, null, 2));

const passes = results.filter((r) => r.status === 'pass');
const fails = results.filter((r) => r.status === 'fail');
const errors = results.filter((r) => r.status === 'error');
const byBucket = {};
for (const r of results) {
  byBucket[r.bucket] = byBucket[r.bucket] || [];
  byBucket[r.bucket].push(r);
}

const lines = [];
lines.push('# Flexbox Froggy triage');
lines.push('');
lines.push(`Ran **${results.length}** / 24 levels · gate **≤ ${maxPct}%** residual · viewport ${W}×${H}.`);
lines.push('');
lines.push(`| Result | Count |`);
lines.push(`|--------|------:|`);
lines.push(`| PASS | ${passes.length} |`);
lines.push(`| FAIL | ${fails.length} |`);
lines.push(`| ERROR | ${errors.length} |`);
lines.push('');
lines.push('## By level');
lines.push('');
lines.push('| # | Name | Residual | Status | Bucket | Style |');
lines.push('|--:|------|--------:|:------:|--------|-------|');
for (const r of results) {
  const pct = r.pct != null ? `${r.pct}%` : '—';
  const style = JSON.stringify(r.style || {}).replace(/\|/g, '\\|');
  lines.push(`| ${r.n} | ${r.name} | ${pct} | ${r.status} | ${r.bucket} | \`${style}\` |`);
}
lines.push('');
lines.push('## Failure buckets');
lines.push('');
for (const [bucket, items] of Object.entries(byBucket)) {
  if (bucket === 'pass') continue;
  lines.push(`### ${bucket} (${items.length})`);
  for (const r of items) {
    const detail = r.error || `${r.pct}% · tags: ${(r.tags || []).join(', ')}`;
    lines.push(`- **${r.id}** ${r.name}: ${detail}`);
  }
  lines.push('');
}
lines.push('## Gaps implied');
lines.push('');
lines.push('- **flex-wrap / align-content / flex-flow**: still no multi-line Gum stack — wrap falls back to Absolute from Chromium boxes (pixel-accurate snapshot, not resize-stable).');
lines.push('- **order / align-self**: extracted and applied for single-line stacks (`order` sort; `align-self` overrides parent align-items).');
lines.push('- ***-reverse** directions: reverse child emit order + invert start/end justify (Gum has no RightToLeft/BottomToTop stack).');
lines.push('- **space-around**: Ratio spacers use 1∶2∶1 end/between gutters; **space-evenly** stays equal 1s.');
lines.push('');
lines.push('Artifacts: `research/froggy/levels/`, `research/froggy/out/L*/{chromium,side}.png`, `results.json`.');
lines.push('');

writeFileSync(join(froggyDir, 'TRIAGE.md'), lines.join('\n'));

console.log(`\n=== Froggy done: ${passes.length} pass / ${fails.length} fail / ${errors.length} error ===`);
console.log(`Wrote ${join(froggyDir, 'results.json')}`);
console.log(`Wrote ${join(froggyDir, 'TRIAGE.md')}`);
process.exit(failed ? 1 : 0);
