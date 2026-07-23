// @ts-nocheck
// Grid Garden batch: emit solution HTML → convert → MonoGame → pixel-diff.
// Levels from research/garden/levels.js (thomaspark/gridgarden). Layout-only
// treatments (solid carrot/weed fills) so residuals measure grid mapping.
//
// Usage: npx tsx run-garden.ts [--only=1,5,20] [--max-pct=2]
import { spawnSync } from 'node:child_process';
import { resolve, dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  mkdirSync, writeFileSync, readFileSync, existsSync, copyFileSync,
} from 'node:fs';
import { createContext, runInContext } from 'node:vm';
import { nodeTsxArgs } from './tsx-run.js';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '..');
const gardenDir = join(repoRoot, 'research', 'garden');
const levelsHtmlDir = join(gardenDir, 'levels');
const outDir = join(gardenDir, 'out');
const hostDir = join(repoRoot, 'host');
const convertTs = join(__dirname, 'convert.ts');

const KIND = { c: 'carrot', w: 'weed' };
const HEX = { carrot: '#518cb3', weed: '#63393E' };
const W = 500;
const H = 500;

/** Utility class rules copied from Grid Garden style.css (class names used by levels). */
const UTILITY_CSS = `
.grid-column-start-1 { grid-column-start: 1; }
.grid-column-start-2 { grid-column-start: 2; }
.grid-column-start-5 { grid-column-start: 5; }
.grid-column-end-6 { grid-column-end: 6; }
.grid-column-4 { grid-column: 4; grid-row: 1 / 6; }
.grid-template-columns-repeat-8-12 { grid-template-columns: repeat(8, 12.5%) !important; }
.grid-template-columns-100px-3em-40p { grid-template-columns: 100px 3em 40% !important; }
.grid-template-columns-1fr-5fr { grid-template-columns: 1fr 5fr !important; }
.grid-template-columns-50px-1fr-1fr-1fr-50px { grid-template-columns: 50px 1fr 1fr 1fr 50px !important; }
.grid-template-columns-6 { grid-template-columns: 75px 3fr 2fr !important; grid-template-rows: 100% !important; }
.grid-template-rows-100p { grid-template-rows: 100% !important; }
.grid-template-rows-50px-0-0-0-1fr { grid-template-rows: 50px 0 0 0 1fr !important; }
.grid-area-1-1-6-2 { grid-area: 1 / 1 / 6 / 2; }
.grid-area-5-1-6-6 { grid-area: 5 / 1 / 6 / 6; }
.grid-area-1-5-6-6 { grid-area: 1 / 5 / 6 / 6; }
.grid-template-2 { grid-template: 1fr 50px / 20% 1fr !important; }
`;

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
  const src = readFileSync(join(gardenDir, 'levels.js'), 'utf8');
  const ctx = createContext({});
  runInContext(`${src}\nthis.levels = levels;`, ctx);
  if (!Array.isArray(ctx.levels) || ctx.levels.length === 0) {
    throw new Error('failed to parse research/garden/levels.js');
  }
  return ctx.levels;
}

function featureTags(level) {
  const tags = new Set();
  const style = level.style || {};
  for (const [k, v] of Object.entries(style)) {
    tags.add(k);
    if (typeof v === 'string') {
      for (const part of v.split(/[\s/]+/)) {
        if (part) tags.add(`${k}:${part}`);
      }
    }
  }
  if (level.selector) tags.add('item-selector');
  if (level.classes) tags.add('has-classes');
  return [...tags];
}

/**
 * Game applies solution via `$('#garden ' + selector).attr('style', code)`.
 * Editor before/after often use fake #water ids — ignore those; mirror applyStyles.
 * Classes from level.classes that touch #garden are applied to matching nodes.
 */
function applyGardenClasses(level, boardLen) {
  const childClasses = Array.from({ length: boardLen }, () => []);
  const gardenClasses = [];
  if (!level.classes) return { childClasses, gardenClasses };

  for (const [rule, cls] of Object.entries(level.classes)) {
    const classes = String(cls).split(/\s+/).filter(Boolean);
    // Only garden-side selectors matter for our #garden-only fixture.
    const gardenPart = rule.split(',').map((s) => s.trim()).find((s) => s.startsWith('#garden'));
    if (!gardenPart) continue;

    if (gardenPart === '#garden' || gardenPart.startsWith('#garden,')) {
      gardenClasses.push(...classes);
      continue;
    }
    // #garden > *
    if (/^#garden\s*>\s*\*$/.test(gardenPart)) {
      for (const arr of childClasses) arr.push(...classes);
      continue;
    }
    // #garden > :nth-child(N)
    const nth = gardenPart.match(/^#garden\s*>\s*:nth-child\((\d+)\)$/);
    if (nth) {
      const i = parseInt(nth[1], 10) - 1;
      if (i >= 0 && i < boardLen) childClasses[i].push(...classes);
      continue;
    }
    // #garden > :nth-child(odd) — not used on garden classes currently
  }
  return { childClasses, gardenClasses };
}

function buildHtml(level) {
  const board = String(level.board || '');
  const { childClasses, gardenClasses } = applyGardenClasses(level, board.length);

  const items = [...board].map((c, i) => {
    const kind = KIND[c] || 'carrot';
    const extra = childClasses[i]?.length ? ` ${childClasses[i].join(' ')}` : '';
    return `  <div class="treatment ${kind}${extra}"></div>`;
  }).join('\n');

  const decls = Object.entries(level.style || {})
    .map(([k, v]) => `  ${k}: ${String(v).replace(/;/g, '').trim()};`)
    .join('\n');

  const sel = (level.selector || '').trim();
  // Empty selector → properties on #garden (grid-template-* levels).
  const solutionCss = sel
    ? `#garden ${sel} {\n${decls}\n}`
    : (decls ? `#garden {\n${decls}\n}` : '');

  const gClass = gardenClasses.length ? ` ${gardenClasses.join(' ')}` : '';

  return `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>Garden — ${level.name}</title>
<style>
* { box-sizing: border-box; margin: 0; padding: 0; }
html, body {
  width: ${W}px;
  height: ${H}px;
  background: #523D1F;
  overflow: hidden;
}
#garden {
  display: grid;
  grid-template-columns: 20% 20% 20% 20% 20%;
  grid-template-rows: 20% 20% 20% 20% 20%;
  width: ${W}px;
  height: ${H}px;
  background: #523D1F;
}
.treatment {
  position: relative;
  width: 100%;
  height: 100%;
  overflow: hidden;
}
.treatment.carrot { background: ${HEX.carrot}; }
.treatment.weed { background: ${HEX.weed}; }
${UTILITY_CSS}

/* Solution (mirrors game.applyStyles on #garden) */
${solutionCss}
</style>
</head>
<body>
<div id="garden" class="${gClass.trim()}">
${items}
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
  if (tags.some((t) => t.startsWith('grid-template') || t.startsWith('grid-template-columns') || t.startsWith('grid-template-rows'))) {
    return 'template-tracks';
  }
  if (tags.some((t) => t.includes('span') || t.includes('grid-area') || t.includes('grid-column') || t.includes('grid-row'))) {
    return 'placement/span';
  }
  if (tags.some((t) => t === 'order' || t.startsWith('order:'))) return 'order';
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
  const tag = `garden-${id}`;
  const screen = `Garden${id}Screen`;
  const tags = featureTags(level);
  const htmlRel = `../research/garden/levels/${id}.html`;
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
      htmlRel, '#garden', screen, String(W), String(H),
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

writeFileSync(join(gardenDir, 'results.json'), JSON.stringify(results, null, 2));

const passes = results.filter((r) => r.status === 'pass');
const fails = results.filter((r) => r.status === 'fail');
const errors = results.filter((r) => r.status === 'error');
const byBucket = {};
for (const r of results) {
  byBucket[r.bucket] = byBucket[r.bucket] || [];
  byBucket[r.bucket].push(r);
}

const lines = [];
lines.push('# Grid Garden triage');
lines.push('');
lines.push(`Ran **${results.length}** / 28 levels · gate **≤ ${maxPct}%** residual · viewport ${W}×${H}.`);
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
lines.push('- **Cell spans / grid-area / line-based placement / order**: AutoGrid is equal cells in DOM order only — these fall back to Absolute from Chromium boxes (pixel-accurate snapshot, not resize-stable).');
lines.push('- **Mixed / fr / % / px tracks**: non-uniform `grid-template-*` → same Absolute fallback.');
lines.push('- **Uniform equal % tracks** (e.g. `repeat(5, 20%)`, `50% 50%`): stay on Gum AutoGrid.');
lines.push('');
lines.push('Artifacts: `research/garden/levels/`, `research/garden/out/L*/{chromium,side}.png`, `results.json`.');
lines.push('');

writeFileSync(join(gardenDir, 'TRIAGE.md'), lines.join('\n'));

console.log(`\n=== Garden done: ${passes.length} pass / ${fails.length} fail / ${errors.length} error ===`);
console.log(`Wrote ${join(gardenDir, 'results.json')}`);
console.log(`Wrote ${join(gardenDir, 'TRIAGE.md')}`);
process.exit(failed ? 1 : 0);
