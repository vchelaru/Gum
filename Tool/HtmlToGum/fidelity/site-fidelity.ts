// @ts-nocheck
// Site fidelity harness: crawl → convert → gumcli screenshot → pixel diff.
//
// Dev loop for improving HtmlToGum accuracy against real multi-page sites.
// Exit 0 when every page is under --max-pct (default 5%); else exit 1.
//
// Usage:
//   cd Tool/HtmlToGum/fidelity && npx tsx site-fidelity.ts https://www.spacejam.com/1996/jam.htm
//   npm run site-fidelity -- <url>   # from converter/ (forwards here)
//   npx tsx site-fidelity.ts <url> --max-pages=15 --max-pct=5 --width=800 --height=900
//   npx tsx site-fidelity.ts <url> --pages=https://.../a.htm,https://.../b.htm  # skip crawl
//
// Convert pipeline lives in ../converter/; this folder is gate/harness only.
// Output (gitignored): Tool/HtmlToGum/.site-fidelity/<slug>/
//   report.json, pages/<Screen>/ { convert out, chromium.png, gum.png, diff/ }
import { spawnSync } from 'node:child_process';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  existsSync, mkdirSync, writeFileSync, copyFileSync, readFileSync, rmSync,
} from 'node:fs';
import { crawlSite, screenNameFromUrl } from './crawl.js';
import { detectPageRejection } from './rejection.js';
import { nodeTsxArgs } from '../converter/tsx-run.js';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoHtmlToGum = resolve(__dirname, '..');
const converterDir = join(repoHtmlToGum, 'converter');
const convertTs = join(converterDir, 'convert.ts');
const gumcliTs = join(converterDir, 'gumcli.ts');
const diffPy = join(__dirname, 'diff_screenshots.py');

function parseArgs(argv) {
  const positional = [];
  const flags = {
    maxPages: 15,
    maxPct: 5,
    width: 800,
    height: 900,
    selector: 'body',
    pathPrefix: null,
    pages: null, // comma-separated absolute URLs — skip crawl
    out: null,
    noResponsive: true, // classic sites: Absolute geometry is stabler for pixel gates
    tag: null,
  };
  for (const a of argv) {
    if (a.startsWith('--max-pages=')) flags.maxPages = parseInt(a.slice(12), 10);
    else if (a.startsWith('--max-pct=')) flags.maxPct = parseFloat(a.slice(10));
    else if (a.startsWith('--width=')) flags.width = parseInt(a.slice(8), 10);
    else if (a.startsWith('--height=')) flags.height = parseInt(a.slice(9), 10);
    else if (a.startsWith('--selector=')) flags.selector = a.slice(11);
    else if (a.startsWith('--path-prefix=')) flags.pathPrefix = a.slice(14);
    else if (a.startsWith('--pages=')) flags.pages = a.slice(8).split(',').map((s) => s.trim()).filter(Boolean);
    else if (a.startsWith('--out=')) flags.out = a.slice(6);
    else if (a === '--responsive') flags.noResponsive = false;
    else if (a === '--no-responsive') flags.noResponsive = true;
    else if (a.startsWith('--')) {
      console.error(`Unknown flag: ${a}`);
      process.exit(2);
    } else positional.push(a);
  }
  return { seed: positional[0], flags };
}

function slugFromUrl(url) {
  const u = new URL(url);
  const path = u.pathname.replace(/\/+/g, '/').replace(/^\//, '').replace(/\/$/, '') || 'root';
  return `${u.hostname}-${path}`.replace(/[^a-zA-Z0-9._-]+/g, '-').slice(0, 80);
}

function run(cmd, args, opts = {}) {
  console.log(`\n> ${cmd} ${args.join(' ')}`);
  const { env: optsEnv, ...rest } = opts;
  const r = spawnSync(cmd, args, {
    encoding: 'utf8',
    shell: false,
    env: {
      ...process.env,
      // Fail fast on never-idle / blocked hosts during fidelity sweeps.
      HTMLTOGUM_GOTO_TIMEOUT_MS: process.env.HTMLTOGUM_GOTO_TIMEOUT_MS || '20000',
      ...(optsEnv || {}),
    },
    ...rest,
  });
  if (r.stdout) process.stdout.write(r.stdout);
  if (r.stderr) process.stderr.write(r.stderr);
  return r;
}

function pixelDiff(refPng, candPng, outDir) {
  if (!existsSync(diffPy)) throw new Error(`missing ${diffPy}`);
  mkdirSync(outDir, { recursive: true });
  const r = run('python', [diffPy, refPng, candPng, outDir, '--top=8']);
  if (r.status !== 0) {
    return { error: r.stderr || `diff_screenshots exited ${r.status}`, pct: 100 };
  }
  const summaryPath = join(outDir, 'diff-summary.json');
  if (!existsSync(summaryPath)) {
    return { error: 'diff-summary.json not written', pct: 100 };
  }
  const summary = JSON.parse(readFileSync(summaryPath, 'utf8'));
  return {
    pct: summary.diffPixelPercent ?? summary.pct ?? 100,
    differing: summary.differingPixels,
    total: summary.totalPixels,
    regions: summary.regions || [],
    summaryPath,
  };
}

/**
 * convert.ts's chromium.png is clipped to the root box ∩ viewport (boxtree.json rect).
 * gumcli screenshot is the full viewport — crop gum to that same intersection so we don't
 * score Body's (x,y) margin as a total failure (Space Jam body at 8,8 → ~39%), and so a
 * negative root y (mdbook sticky) doesn't keep full height past where BodyBg paints.
 */
function alignGumToChromiumClip(pageDir, gumPng, alignedOut) {
  const boxPath = join(pageDir, 'boxtree.json');
  if (!existsSync(boxPath) || !existsSync(gumPng)) return gumPng;
  const tree = JSON.parse(readFileSync(boxPath, 'utf8'));
  const rx = Number(tree.rect?.x) || 0;
  const ry = Number(tree.rect?.y) || 0;
  const rw = Number(tree.rect?.width) || 0;
  const rh = Number(tree.rect?.height) || 0;
  const script = `
from PIL import Image
import math
im = Image.open(r'''${gumPng.replace(/\\/g, '/')}''').convert('RGB')
rx, ry, rw, rh = ${rx}, ${ry}, ${rw}, ${rh}
x0 = max(0, math.floor(rx))
y0 = max(0, math.floor(ry))
x1 = min(im.width, math.ceil(rx + rw))
y1 = min(im.height, math.ceil(ry + rh))
if x1 <= x0 or y1 <= y0:
    raise SystemExit('empty align clip')
im.crop((x0, y0, x1, y1)).save(r'''${alignedOut.replace(/\\/g, '/')}''')
print(f'aligned gum crop ({x0},{y0})-({x1},{y1}) from {im.size}')
`;
  const r = spawnSync('python', ['-c', script], { encoding: 'utf8' });
  if (r.stdout) process.stdout.write(r.stdout);
  if (r.stderr) process.stderr.write(r.stderr);
  if (r.status !== 0 || !existsSync(alignedOut)) {
    console.warn('  ! gum align crop failed — diffing full viewport');
    return gumPng;
  }
  return alignedOut;
}

async function main() {
  const { seed, flags } = parseArgs(process.argv.slice(2));
  if (!seed || !/^https?:\/\//i.test(seed)) {
    console.error('Usage: npx tsx site-fidelity.ts <https://seed-url> [--max-pages=15] [--max-pct=5]');
    process.exit(2);
  }

  const slug = flags.tag || slugFromUrl(seed);
  const rootOut = flags.out
    ? resolve(flags.out)
    : join(repoHtmlToGum, '.site-fidelity', slug);
  mkdirSync(rootOut, { recursive: true });

  console.log(`site-fidelity seed: ${seed}`);
  console.log(`out: ${rootOut}`);
  console.log(`viewport: ${flags.width}×${flags.height}  maxPct: ${flags.maxPct}%  maxPages: ${flags.maxPages}`);

  let urls;
  /** @type {string|undefined} */
  let abortReason;
  if (flags.pages?.length) {
    urls = flags.pages;
    console.log(`using ${urls.length} explicit --pages (crawl skipped)`);
  } else {
    console.log('crawling…');
    const crawl = await crawlSite(seed, {
      maxPages: flags.maxPages,
      pathPrefix: flags.pathPrefix || undefined,
      viewport: { width: flags.width, height: flags.height },
    });
    urls = crawl.urls;
    if (crawl.rejected) {
      abortReason = crawl.reason || 'crawl rejected';
      console.warn(`crawl aborted: ${abortReason}`);
    }
    console.log(`discovered ${urls.length} page(s):`);
    for (const u of urls) console.log(`  - ${u}`);
  }

  writeFileSync(join(rootOut, 'urls.json'), JSON.stringify(urls, null, 2));

  const usedNames = new Set();
  /** @type {object[]} */
  const results = [];
  let failed = 0;

  if ((abortReason && urls.length === 0) || (!flags.pages?.length && urls.length === 0)) {
    const reason = abortReason || 'no pages discovered';
    const report = {
      seed,
      generatedAt: new Date().toISOString(),
      viewport: { width: flags.width, height: flags.height },
      maxPct: flags.maxPct,
      status: 'rejected',
      abortReason: reason,
      failed: 0,
      passed: 0,
      pages: [],
    };
    const reportPath = join(rootOut, 'report.json');
    writeFileSync(reportPath, JSON.stringify(report, null, 2));
    console.log(`\n=== site-fidelity rejected ===\n${reason}\nreport: ${reportPath}`);
    // Exit 0 — host is unsuitable, not a converter regression.
    process.exit(0);
  }

  for (let i = 0; i < urls.length; i++) {
    const pageUrl = urls[i];
    let screen = screenNameFromUrl(pageUrl);
    let n = 2;
    while (usedNames.has(screen)) {
      screen = `${screenNameFromUrl(pageUrl)}${n++}`;
    }
    usedNames.add(screen);

    const pageDir = join(rootOut, 'pages', screen);
    rmSync(pageDir, { recursive: true, force: true });
    mkdirSync(pageDir, { recursive: true });

    console.log(`\n========== [${i + 1}/${urls.length}] ${screen} ==========`);
    console.log(pageUrl);

    const convertArgs = [
      convertTs, pageUrl, flags.selector, screen,
      String(flags.width), String(flags.height),
      `--out=${pageDir}`,
      `--tag=${screen}`,
    ];
    if (flags.noResponsive) convertArgs.push('--no-responsive');
    // Pixel gate compares against Chromium's native form chrome; default Forms visuals
    // would inflate diffs. Keep visual-only mapping for fidelity runs.
    convertArgs.push('--no-forms');

    const conv = run(process.execPath, nodeTsxArgs(...convertArgs), { cwd: converterDir });
    if (conv.status !== 0) {
      failed++;
      const errText = `${conv.stderr || ''}\n${conv.stdout || ''}`;
      const navReject = /TimeoutError|NS_ERROR_ABORT|net::ERR_|ERR_TOO_MANY_REDIRECTS|Navigation failed/i.test(errText);
      results.push({
        url: pageUrl, screen, status: 'convert-fail', exit: conv.status, pct: null,
      });
      // First page hard-nav failure → host is blocked; do not burn the remaining pages.
      if (navReject && i === 0) {
        abortReason = `convert rejected on seed: ${errText.replace(/\s+/g, ' ').slice(0, 200)}`;
        console.warn(`aborting remaining pages — ${abortReason}`);
        break;
      }
      continue;
    }

    let captureMeta = null;
    try {
      const metaPath = join(pageDir, 'capture-meta.json');
      if (existsSync(metaPath)) {
        captureMeta = JSON.parse(readFileSync(metaPath, 'utf8'));
        if (captureMeta?.suspectedRotatingMedia) {
          console.log(
            `  note: rotating media stabilized`
            + ` (pinned=${captureMeta.pinnedSlideGroups}, pausedAnims=${captureMeta.pausedAnimations})`
            + ` — do not debug carousel timing; fix mapping or move on`,
          );
        }
      }
    } catch { /* ignore */ }

    // Soft rejection after a successful convert (challenge HTML painted into Gum).
    try {
      const boxPath = join(pageDir, 'boxtree.json');
      if (existsSync(boxPath) && i === 0) {
        const tree = JSON.parse(readFileSync(boxPath, 'utf8'));
        const soft = detectPageRejection({
          status: 200,
          title: tree?.title || '',
          html: JSON.stringify(tree).slice(0, 40_000),
        });
        if (soft.rejected) {
          abortReason = `converted page looks rejected: ${soft.reason}`;
          console.warn(`aborting remaining pages — ${abortReason}`);
          results.push({
            url: pageUrl, screen, status: 'rejected', pct: null, reason: soft.reason,
          });
          break;
        }
      }
    } catch {
      /* ignore probe errors */
    }

    const chromiumPng = join(pageDir, 'chromium.png');
    const chromiumTagged = join(pageDir, `chromium-${screen}.png`);
    if (!existsSync(chromiumPng) && existsSync(chromiumTagged)) {
      copyFileSync(chromiumTagged, chromiumPng);
    }
    if (!existsSync(chromiumPng)) {
      failed++;
      results.push({
        url: pageUrl, screen, status: 'missing-chromium', pct: null,
      });
      continue;
    }

    const gumPng = join(pageDir, 'gum.png');
    const gumx = join(pageDir, 'Generated.gumx');
    const shot = run(process.execPath, nodeTsxArgs(
      gumcliTs, 'screenshot', gumx, screen,
      '--output', gumPng,
      '--width', String(flags.width),
      '--height', String(flags.height),
    ), { cwd: converterDir });
    if (shot.status !== 0 || !existsSync(gumPng)) {
      failed++;
      results.push({
        url: pageUrl, screen, status: 'screenshot-fail', exit: shot.status, pct: null,
      });
      continue;
    }

    const gumAligned = alignGumToChromiumClip(pageDir, gumPng, join(pageDir, 'gum-aligned.png'));
    const diffDir = join(pageDir, 'diff');
    const diff = pixelDiff(chromiumPng, gumAligned, diffDir);
    if (diff.error) {
      failed++;
      results.push({
        url: pageUrl, screen, status: 'diff-fail', error: diff.error, pct: null,
      });
      continue;
    }

    const ok = diff.pct <= flags.maxPct;
    console.log(
      `${screen}: ${diff.pct.toFixed(2)}% differing  (max ${flags.maxPct}%)  [${ok ? 'PASS' : 'FAIL'}]`,
    );
    if (!ok) failed++;
    results.push({
      url: pageUrl,
      screen,
      status: ok ? 'pass' : 'fail',
      pct: diff.pct,
      differing: diff.differing,
      total: diff.total,
      topRegions: (diff.regions || []).slice(0, 5),
      rotatingMediaStabilized: Boolean(captureMeta?.suspectedRotatingMedia),
    });
  }

  const report = {
    seed,
    generatedAt: new Date().toISOString(),
    viewport: { width: flags.width, height: flags.height },
    maxPct: flags.maxPct,
    status: abortReason ? 'rejected' : (failed ? 'fail' : 'pass'),
    abortReason: abortReason || null,
    failed,
    passed: results.filter((r) => r.status === 'pass').length,
    pages: results,
  };
  const reportPath = join(rootOut, 'report.json');
  writeFileSync(reportPath, JSON.stringify(report, null, 2));

  console.log('\n=== site-fidelity summary ===');
  console.log(`passed ${report.passed}/${results.length}  failed ${failed}  report: ${reportPath}`);
  if (abortReason) console.log(`aborted: ${abortReason}`);
  for (const r of results) {
    const pct = r.pct == null ? '—' : `${r.pct.toFixed(2)}%`;
    console.log(`  [${r.status}] ${r.screen}: ${pct}`);
  }

  // Rejected hosts are unsuitable seeds, not converter regressions.
  process.exit(abortReason ? 0 : (failed ? 1 : 0));
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
