// @ts-nocheck
// Curated canary suite: local layout zoo + frozen captures + live CS/general sites.
//
// Catches converter overfitting: a fix that greens a new site must not push a prior
// canary over its absolute maxPct or more than maxDeltaPct above baselinePct.
//
// Usage:
//   npm run canaries                  # local + frozen + live
//   npm run canaries -- --tier=local  # layout zoo only (~1–2 min)
//   npm run canaries -- --tier=frozen
//   npm run canaries -- --tier=live   # ~5–8 min
//   npm run canaries -- --update-baselines   # rewrite baselinePct after a good run
//   npm run canaries -- --id=spacejam
//
// Output (gitignored): Tool/HtmlToGum/.canaries/
import { spawnSync } from 'node:child_process';
import { dirname, join, resolve, isAbsolute } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  existsSync, mkdirSync, writeFileSync, readFileSync, copyFileSync, rmSync,
} from 'node:fs';
import { nodeTsxArgs } from '../converter/tsx-run.js';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoHtmlToGum = resolve(__dirname, '..');
const converterDir = join(repoHtmlToGum, 'converter');
const convertTs = join(converterDir, 'convert.ts');
const gumcliTs = join(converterDir, 'gumcli.ts');
const diffPy = join(__dirname, 'diff_screenshots.py');
const canariesPath = join(__dirname, 'canaries.json');

function parseArgs(argv) {
  const flags = {
    tier: 'all', // all | local | frozen | live
    updateBaselines: false,
    id: null,
  };
  for (const a of argv) {
    if (a.startsWith('--tier=')) flags.tier = a.slice(7);
    else if (a === '--update-baselines') flags.updateBaselines = true;
    else if (a.startsWith('--id=')) flags.id = a.slice(5);
    else if (a.startsWith('--')) {
      console.error(`Unknown flag: ${a}`);
      process.exit(2);
    }
  }
  return flags;
}

function run(cmd, args, opts = {}) {
  console.log(`\n> ${cmd} ${args.join(' ')}`);
  const r = spawnSync(cmd, args, {
    encoding: 'utf8',
    shell: false,
    env: {
      ...process.env,
      HTMLTOGUM_GOTO_TIMEOUT_MS: process.env.HTMLTOGUM_GOTO_TIMEOUT_MS || '20000',
    },
    ...opts,
  });
  if (r.stdout) process.stdout.write(r.stdout);
  if (r.stderr) process.stderr.write(r.stderr);
  return r;
}

function pixelDiff(refPng, candPng, outDir) {
  mkdirSync(outDir, { recursive: true });
  const r = run('python', [diffPy, refPng, candPng, outDir, '--top=8']);
  if (r.status !== 0) {
    return { error: r.stderr || `diff_screenshots exited ${r.status}`, pct: 100 };
  }
  const summaryPath = join(outDir, 'diff-summary.json');
  if (!existsSync(summaryPath)) return { error: 'diff-summary.json not written', pct: 100 };
  const summary = JSON.parse(readFileSync(summaryPath, 'utf8'));
  return {
    pct: summary.diffPixelPercent ?? summary.pct ?? 100,
    differing: summary.differingPixels,
    total: summary.totalPixels,
  };
}

function alignGumToChromiumClip(pageDir, gumPng, alignedOut) {
  const boxPath = join(pageDir, 'boxtree.json');
  if (!existsSync(boxPath) || !existsSync(gumPng)) return gumPng;
  const tree = JSON.parse(readFileSync(boxPath, 'utf8'));
  const r = tree?.rect;
  if (!r) return gumPng;
  const x = Math.max(0, Math.floor(r.x || 0));
  const y = Math.max(0, Math.floor(r.y || 0));
  const script = `
from PIL import Image
import sys
gum = Image.open(r'''${gumPng.replace(/\\/g, '/')}''')
chrom = Image.open(r'''${join(pageDir, 'chromium.png').replace(/\\/g, '/')}''')
w, h = chrom.size
crop = gum.crop((${x}, ${y}, ${x}+w, ${y}+h))
crop.save(r'''${alignedOut.replace(/\\/g, '/')}''')
print('aligned', crop.size)
`;
  const py = spawnSync('python', ['-c', script], { encoding: 'utf8' });
  if (py.status !== 0 || !existsSync(alignedOut)) {
    if (py.stderr) process.stderr.write(py.stderr);
    return gumPng;
  }
  return alignedOut;
}

function resolveSource(entry, cfg) {
  if (entry.url) return { kind: 'url', source: entry.url };
  if (entry.html) {
    const p = isAbsolute(entry.html)
      ? entry.html
      : resolve(__dirname, entry.html);
    if (!existsSync(p)) throw new Error(`missing html for ${entry.id}: ${p}`);
    return { kind: 'file', source: p };
  }
  throw new Error(`canary ${entry.id} needs url or html`);
}

/**
 * @returns {{ status: string, pct: number|null, reason?: string, delta?: number|null }}
 */
function runOne(entry, cfg, outRoot) {
  const width = entry.width ?? cfg.viewport.width;
  const height = entry.height ?? cfg.viewport.height;
  const selector = entry.selector || 'body';
  const screen = (entry.id || 'Canary').replace(/[^a-zA-Z0-9]+/g, '');
  const pageDir = join(outRoot, entry.id);
  rmSync(pageDir, { recursive: true, force: true });
  mkdirSync(pageDir, { recursive: true });

  const { source } = resolveSource(entry, cfg);
  const convertArgs = [
    convertTs, source, selector, screen,
    String(width), String(height),
    `--out=${pageDir}`,
    `--tag=${screen}`,
    '--no-responsive',
    '--no-forms',
  ];
  const conv = run(process.execPath, nodeTsxArgs(...convertArgs), { cwd: converterDir });
  if (conv.status !== 0) {
    return { status: 'convert-fail', pct: null, reason: `convert exit ${conv.status}` };
  }

  const chromiumPng = join(pageDir, 'chromium.png');
  const chromiumTagged = join(pageDir, `chromium-${screen}.png`);
  if (!existsSync(chromiumPng) && existsSync(chromiumTagged)) {
    copyFileSync(chromiumTagged, chromiumPng);
  }
  if (!existsSync(chromiumPng)) {
    return { status: 'missing-chromium', pct: null };
  }

  const gumPng = join(pageDir, 'gum.png');
  const gumx = join(pageDir, 'Generated.gumx');
  const shot = run(process.execPath, nodeTsxArgs(
    gumcliTs, 'screenshot', gumx, screen,
    '--output', gumPng,
    '--width', String(width),
    '--height', String(height),
  ), { cwd: converterDir });
  if (shot.status !== 0 || !existsSync(gumPng)) {
    return { status: 'screenshot-fail', pct: null };
  }

  const gumAligned = alignGumToChromiumClip(pageDir, gumPng, join(pageDir, 'gum-aligned.png'));
  const diff = pixelDiff(chromiumPng, gumAligned, join(pageDir, 'diff'));
  if (diff.error) return { status: 'diff-fail', pct: null, reason: diff.error };

  const maxPct = entry.maxPct ?? cfg.defaults.maxPct;
  const maxDelta = entry.maxDeltaPct ?? cfg.defaults.maxDeltaPct;
  const baseline = entry.baselinePct;
  const pct = diff.pct;
  const delta = baseline == null ? null : pct - baseline;

  let status = 'pass';
  let reason;
  if (pct > maxPct) {
    status = 'fail';
    reason = `pct ${pct.toFixed(2)} > maxPct ${maxPct}`;
  } else if (baseline != null && delta > maxDelta) {
    status = 'fail';
    reason = `delta +${delta.toFixed(2)} pts > maxDeltaPct ${maxDelta} (baseline ${baseline})`;
  }

  return { status, pct, delta, reason, maxPct, baseline };
}

function collectEntries(cfg, tier, idFilter) {
  const buckets = [];
  if (tier === 'all' || tier === 'local') {
    for (const e of cfg.local || []) buckets.push({ ...e, tier: 'local' });
  }
  if (tier === 'all' || tier === 'frozen') {
    for (const e of cfg.frozen || []) buckets.push({ ...e, tier: 'frozen' });
  }
  if (tier === 'all' || tier === 'live') {
    for (const e of cfg.live || []) buckets.push({ ...e, tier: 'live' });
  }
  if (idFilter) return buckets.filter((e) => e.id === idFilter);
  return buckets;
}

function updateBaselines(cfg, results) {
  const byId = new Map(results.filter((r) => r.pct != null).map((r) => [r.id, r.pct]));
  for (const key of ['local', 'frozen', 'live']) {
    for (const e of cfg[key] || []) {
      if (byId.has(e.id)) e.baselinePct = Math.round(byId.get(e.id) * 100) / 100;
    }
  }
  writeFileSync(canariesPath, `${JSON.stringify(cfg, null, 2)}\n`);
  console.log(`\nupdated baselines in ${canariesPath}`);
}

async function main() {
  const flags = parseArgs(process.argv.slice(2));
  if (!existsSync(canariesPath)) {
    console.error(`missing ${canariesPath}`);
    process.exit(2);
  }
  const cfg = JSON.parse(readFileSync(canariesPath, 'utf8'));
  const entries = collectEntries(cfg, flags.tier, flags.id);
  if (entries.length === 0) {
    console.error(`no canaries for tier=${flags.tier}` + (flags.id ? ` id=${flags.id}` : ''));
    process.exit(2);
  }

  // Skip frozen entries whose HTML has not been frozen yet.
  const runnable = [];
  for (const e of entries) {
    if (e.html) {
      const p = resolve(__dirname, e.html);
      if (!existsSync(p)) {
        console.warn(`skip ${e.id}: frozen/local html missing (${p}) — run npm run freeze`);
        continue;
      }
    }
    runnable.push(e);
  }
  if (runnable.length === 0) {
    console.error('nothing to run (freeze pages first for --tier=frozen)');
    process.exit(2);
  }

  const outRoot = join(repoHtmlToGum, '.canaries');
  mkdirSync(outRoot, { recursive: true });
  console.log(`canaries: ${runnable.length} entr(y/ies)  tier=${flags.tier}`);
  console.log(`out: ${outRoot}`);

  const results = [];
  let failed = 0;
  for (let i = 0; i < runnable.length; i++) {
    const e = runnable[i];
    console.log(`\n========== [${i + 1}/${runnable.length}] ${e.tier}/${e.id} ==========`);
    if (e.landmine) console.log(`landmine: ${e.landmine}`);
    let result;
    try {
      result = runOne(e, cfg, outRoot);
    } catch (err) {
      result = { status: 'error', pct: null, reason: String(err?.message || err) };
    }
    const ok = result.status === 'pass';
    if (!ok) failed++;
    const pctStr = result.pct == null ? '—' : `${result.pct.toFixed(2)}%`;
    const deltaStr = result.delta == null ? '' : `  Δ${result.delta >= 0 ? '+' : ''}${result.delta.toFixed(2)}`;
    console.log(
      `${e.id}: ${pctStr}${deltaStr}  [${ok ? 'PASS' : 'FAIL'}]`
      + (result.reason ? `  (${result.reason})` : ''),
    );
    results.push({ id: e.id, tier: e.tier, ...result });
  }

  const report = {
    generatedAt: new Date().toISOString(),
    tier: flags.tier,
    failed,
    passed: results.length - failed,
    results,
  };
  const reportPath = join(outRoot, 'report.json');
  writeFileSync(reportPath, JSON.stringify(report, null, 2));

  if (flags.updateBaselines) updateBaselines(cfg, results);

  console.log('\n=== canaries summary ===');
  console.log(`passed ${report.passed}/${results.length}  failed ${failed}  report: ${reportPath}`);
  for (const r of results) {
    const pctStr = r.pct == null ? '—' : `${r.pct.toFixed(2)}%`;
    console.log(`  [${r.status}] ${r.tier}/${r.id}: ${pctStr}`);
  }
  process.exit(failed > 0 ? 1 : 0);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
