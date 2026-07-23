// @ts-nocheck
// Build the multi-example demo gallery under ../demo/
// Usage: npx tsx build-demo.ts
import { spawnSync } from 'node:child_process';
import { resolve, dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  mkdirSync, writeFileSync, copyFileSync, existsSync, readFileSync,
} from 'node:fs';
import { nodeTsxArgs } from './tsx-run.js';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '..');
const demoDir = join(repoRoot, 'demo');
const examplesDir = join(demoDir, 'examples');
const hostDir = join(repoRoot, 'host');
const convertTs = join(__dirname, 'convert.ts');

/** Curated set that covers shipped capabilities (skips flaky CDN fixtures). */
const EXAMPLES = [
  {
    id: 'showcase',
    title: 'Aether Outpost',
    blurb: 'Full HUD composite — fixed chrome, flex, AutoGrid, NineSlice, text slots, gradients.',
    features: ['fixed', 'flex', 'justify', 'AutoGrid', 'NineSlice', 'text chrome', 'absolute', 'gradients'],
    html: 'input/demo-showcase.html', sel: 'body', screen: 'DemoShowcaseScreen', w: 960, h: 600,
  },
  {
    id: 'rpgui',
    title: 'RPGUI fantasy HUD',
    blurb: 'Third-party RPGUI — border-image chrome rasterized, text-shadow stamps, pixel font, progress.',
    features: ['border-image', 'raster chrome', 'text-shadow', 'web font', 'absolute HUD', 'progress'],
    html: 'input/rpgui-hud.html', sel: '#hud', screen: 'RpguiHudScreen', w: 800, h: 600,
    extraArgs: ['--no-responsive'],
  },
  {
    id: 'responsive',
    title: 'Responsive units',
    blurb: 'Dual-viewport inference → PercentageOfParent sidebar + fill main.',
    features: ['responsive', '% of parent'],
    html: 'input/responsive-sidebar.html', sel: '#layout', screen: 'ResponsiveDemo', w: 1200, h: 400,
    extraArgs: ['--responsive=400,1200'],
  },
  {
    id: 'raster',
    title: 'Raster effects',
    blurb: 'Leaf gradients/filters → Sprite; gradient parents keep structured children under a backdrop Sprite.',
    features: ['gradients', 'filter', 'raster backdrop'],
    html: 'input/raster-effects.html', sel: 'body', screen: 'RasterScreen', w: 800, h: 400,
  },
  {
    id: 'textshadow',
    title: 'Text shadow outline',
    blurb: 'Hard multi-layer text-shadow → black Text stamps behind the face (RPGUI-style faux outline).',
    features: ['text-shadow', 'glyph outline'],
    html: 'input/text-outline.html', sel: '#panel', screen: 'TextOutlineScreen', w: 800, h: 400,
    extraArgs: ['--no-responsive'],
  },
  {
    id: 'textxform',
    title: 'text-transform',
    blurb: 'uppercase / lowercase / capitalize baked into the string (Gum Text has no text-transform).',
    features: ['text-transform'],
    html: 'input/text-transform.html', sel: '#panel', screen: 'TextTransformScreen', w: 800, h: 400,
    extraArgs: ['--no-responsive'],
  },
  {
    id: 'brtext',
    title: 'Line breaks',
    blurb: '<br>-only children → innerText lines (party cards, multi-line labels).',
    features: ['<br>', 'innerText'],
    html: 'input/br-text.html', sel: '#panel', screen: 'BrTextScreen', w: 800, h: 400,
    extraArgs: ['--no-responsive'],
  },
  {
    id: 'borderbg',
    title: 'Frame + fill',
    blurb: 'background-image parchment under border-image → chrome raster with structured children on top.',
    features: ['border-image', 'background-image', 'raster chrome'],
    html: 'input/border-image-with-bg.html', sel: '#panel', screen: 'BorderBgScreen', w: 800, h: 400,
    extraArgs: ['--no-responsive'],
  },
  {
    id: 'nineslice',
    title: 'NineSlice panel',
    blurb: 'Uniform border + radius → generated 9-slice texture.',
    features: ['NineSlice', 'border-radius'],
    html: 'input/nineslice-panel.html', sel: '#panel', screen: 'NineSliceScreen', w: 800, h: 600,
  },
  {
    id: 'asymmetric',
    title: 'Asymmetric borders',
    blurb: 'Per-side border widths/colors as edge Rectangles.',
    features: ['asymmetric borders'],
    html: 'input/asymmetric-border.html', sel: '#box', screen: 'AsymBorderScreen', w: 800, h: 600,
  },
  {
    id: 'grid',
    title: 'Uniform grid',
    blurb: 'Equal-track CSS Grid → Gum AutoGrid.',
    features: ['AutoGrid', '1fr tracks'],
    html: 'input/grid-uniform.html', sel: '#grid', screen: 'GridScreen', w: 800, h: 600,
  },
  {
    id: 'gridspan',
    title: 'Mixed / spanned grid',
    blurb: 'Non-uniform tracks + cell span → Absolute Tier-1 fallback (warned).',
    features: ['grid fallback', 'Absolute'],
    html: 'input/grid-span.html', sel: '#grid', screen: 'GridSpanScreen', w: 800, h: 600,
  },
  {
    id: 'justify',
    title: 'justify-content',
    blurb: 'space-between via Ratio spacer Containers in the stack.',
    features: ['justify-content', 'Ratio spacers'],
    html: 'input/justify-between.html', sel: '#bar', screen: 'JustifyScreen', w: 800, h: 600,
  },
  {
    id: 'align',
    title: 'align-items',
    blurb: 'Cross-axis center / end origins inside a row stack.',
    features: ['align-items', 'flex row'],
    html: 'input/align-items-center.html', sel: '#bar', screen: 'AlignScreen', w: 800, h: 600,
  },
  {
    id: 'padding',
    title: 'Flex padding',
    blurb: 'Padding becomes an inset Content wrapper (RelativeToParent −pad).',
    features: ['padding', 'flex'],
    html: 'input/padding-flex.html', sel: '#panel', screen: 'PaddingScreen', w: 800, h: 600,
  },
  {
    id: 'zindex',
    title: 'z-index paint order',
    blurb: 'Sibling emit order matches CSS stacking (DOM index tie-break).',
    features: ['z-index'],
    html: 'input/z-index-order.html', sel: '#stage', screen: 'ZIndexScreen', w: 800, h: 600,
  },
  {
    id: 'fixed',
    title: 'Fixed + absolute',
    blurb: 'Fixed HUD reparented to screen; absolute badge out of flex flow.',
    features: ['position:fixed', 'position:absolute'],
    html: 'input/fixed-hud.html', sel: 'body', screen: 'FixedHudScreen', w: 800, h: 600,
  },
  {
    id: 'cssom',
    title: 'CSSOM percentages',
    blurb: 'Stylesheet width/height % (not just inline) → PercentageOfParent.',
    features: ['CSSOM cascade', '% widths'],
    html: 'input/cssom-percent.html', sel: 'body', screen: 'CssomScreen', w: 800, h: 600,
  },
  {
    id: 'opacity',
    title: 'Underlay + opacity',
    blurb: 'Styled <img> → Container + fill underlay + Sprite; opacity on the right channel.',
    features: ['img underlay', 'opacity'],
    html: 'input/underlay-opacity.html', sel: '#panel', screen: 'UnderlayScreen', w: 800, h: 400,
    extraArgs: ['--no-responsive'],
  },
];

function run(cmd, args, cwd) {
  console.log(`\n> ${cmd} ${args.join(' ')}`);
  const r = spawnSync(cmd, args, { cwd, encoding: 'utf8', shell: false });
  if (r.stdout) process.stdout.write(r.stdout);
  if (r.stderr) process.stderr.write(r.stderr);
  if (r.status !== 0) throw new Error(`${cmd} exited ${r.status}`);
}

function pixelDiff(chromiumPath, gumPath, crop, outDir) {
  const script = `
from PIL import Image, ImageChops
import json, sys
chrom = Image.open(sys.argv[1]).convert('RGB')
gum = Image.open(sys.argv[2]).convert('RGB')
x, y = int(sys.argv[4]), int(sys.argv[5])
w, h = chrom.size
# Chromium shot is already clipped to the root selector; crop Gum to the same rect.
gum_c = gum.crop((x, y, x + w, y + h))
if gum_c.size != chrom.size:
    print(json.dumps({"error": f"size {gum_c.size} vs {chrom.size}"}))
    sys.exit(1)
d = ImageChops.difference(gum_c, chrom)
diff = sum(1 for p in d.getdata() if p != (0, 0, 0))
total = w * h
gum_c.save(sys.argv[3])  # cropped gum.png for the gallery pair
print(json.dumps({"diff": diff, "total": total, "pct": round(100.0 * diff / total, 3), "h": h, "w": w}))
`;
  const gumCropped = join(outDir, 'gum.png');
  const r = spawnSync('python', [
    '-c', script, chromiumPath, gumPath, gumCropped, String(crop.x), String(crop.y),
  ], { encoding: 'utf8', maxBuffer: 32 * 1024 * 1024 });
  if (r.status !== 0) throw new Error(`diff failed: ${r.stderr || r.stdout}`);
  return JSON.parse(r.stdout.trim());
}

mkdirSync(examplesDir, { recursive: true });
const results = [];

for (const ex of EXAMPLES) {
  console.log(`\n======== ${ex.id} — ${ex.title} ========`);
  const outDir = join(examplesDir, ex.id);
  mkdirSync(outDir, { recursive: true });

  const convertArgs = [
    convertTs,
    ex.html, ex.sel, ex.screen, String(ex.w), String(ex.h),
    `--tag=demo-${ex.id}`,
    ...(ex.extraArgs || []),
  ];
  run(process.execPath, nodeTsxArgs(...convertArgs), __dirname);

  const chromiumSrc = join(repoRoot, '.regress', `chromium-demo-${ex.id}.png`);
  const chromiumDst = join(outDir, 'chromium.png');
  if (!existsSync(chromiumSrc)) throw new Error(`missing ${chromiumSrc}`);
  copyFileSync(chromiumSrc, chromiumDst);

  const gumFull = join(outDir, 'gum-full.png');
  run('dotnet', [
    'run', '-c', 'Release', '--project', join(hostDir, 'HtmlToGumHost.csproj'),
    '--', gumFull, 'generated/Generated.gumx', ex.screen, String(ex.w), String(ex.h),
  ], hostDir);

  const tree = JSON.parse(readFileSync(
    join(hostDir, 'Content', 'generated', 'boxtree.json'), 'utf8'));
  const crop = {
    x: Math.max(0, Math.floor(tree.rect.x)),
    y: Math.max(0, Math.floor(tree.rect.y)),
  };

  const stats = pixelDiff(chromiumDst, gumFull, crop, outDir);
  if (stats.error) throw new Error(`${ex.id}: ${stats.error}`);
  results.push({
    id: ex.id,
    title: ex.title,
    blurb: ex.blurb,
    features: ex.features,
    canvasW: ex.w,
    canvasH: ex.h,
    ...stats,
    html: ex.html,
  });
  console.log(`  → ${stats.pct}% (${stats.diff}/${stats.total}) crop@${crop.x},${crop.y}`);
}

writeFileSync(join(demoDir, 'manifest.json'), JSON.stringify(results, null, 2));

const nav = results.map((r) =>
  `      <a href="#${r.id}">${r.title}</a>`).join('\n');

const sections = results.map((r) => {
  const tags = r.features.map((f) => `<span class="tag">${escapeHtml(f)}</span>`).join('');
  return `
    <section class="example" id="${r.id}">
      <div class="ex-head">
        <div>
          <h2>${escapeHtml(r.title)}</h2>
          <p class="blurb">${escapeHtml(r.blurb)}</p>
          <div class="tags">${tags}</div>
        </div>
        <div class="stats">
          <div><b>${r.pct}%</b> delta</div>
          <div>${r.w}×${r.h}</div>
          <div class="mono">${escapeHtml(r.html)}</div>
        </div>
      </div>
      <div class="pair">
        <figure>
          <div class="cap"><strong>Chromium</strong></div>
          <img src="examples/${r.id}/chromium.png" alt="${escapeHtml(r.title)} Chromium" width="${r.w}" height="${r.h}" loading="lazy">
        </figure>
        <figure>
          <div class="cap"><strong>Gum</strong></div>
          <img src="examples/${r.id}/gum.png" alt="${escapeHtml(r.title)} Gum" width="${r.w}" height="${r.h}" loading="lazy">
        </figure>
      </div>
    </section>`;
}).join('\n');

function escapeHtml(s) {
  return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

const avg = results.reduce((a, r) => a + r.pct, 0) / results.length;
const zeros = results.filter((r) => r.pct === 0).length;

const html = `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>html-to-gum — Feature gallery</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Instrument+Sans:wght@400;600;700&family=JetBrains+Mono:wght@400;600&display=swap" rel="stylesheet">
<style>
  :root {
    --bg0: #0a0c10;
    --bg1: #12161e;
    --ink: #e8ecf2;
    --muted: #8b95a5;
    --line: #2a3340;
    --accent: #d4a84b;
    --ok: #3d8a5a;
    --chrome: #161c26;
  }
  * { box-sizing: border-box; margin: 0; padding: 0; }
  html { scroll-behavior: smooth; }
  body {
    min-height: 100%;
    background:
      radial-gradient(1100px 520px at 12% -8%, #1a2230 0%, transparent 55%),
      radial-gradient(800px 420px at 100% 0%, #1a1810 0%, transparent 40%),
      var(--bg0);
    color: var(--ink);
    font-family: "Instrument Sans", system-ui, sans-serif;
  }
  .wrap { max-width: 1120px; margin: 0 auto; padding: 40px 24px 96px; }

  .hero { margin-bottom: 28px; }
  .brand {
    font-family: "JetBrains Mono", monospace;
    font-size: clamp(2.2rem, 4.5vw, 3.2rem);
    font-weight: 600;
    letter-spacing: -0.03em;
    color: var(--accent);
    line-height: 1.05;
  }
  .lede {
    margin-top: 12px;
    max-width: 38rem;
    color: var(--muted);
    line-height: 1.55;
    font-size: 1.02rem;
  }
  .meta {
    margin-top: 16px;
    display: flex; flex-wrap: wrap; gap: 10px 20px;
    font-family: "JetBrains Mono", monospace;
    font-size: 0.78rem;
    color: var(--muted);
  }
  .meta b { color: var(--ink); font-weight: 600; }

  .toc {
    display: flex; flex-wrap: wrap; gap: 8px;
    margin: 28px 0 40px;
    padding-bottom: 24px;
    border-bottom: 1px solid var(--line);
  }
  .toc a {
    font-family: "JetBrains Mono", monospace;
    font-size: 0.72rem;
    color: var(--muted);
    text-decoration: none;
    padding: 6px 10px;
    border: 1px solid var(--line);
    background: var(--chrome);
  }
  .toc a:hover { color: var(--ink); border-color: var(--accent); }

  .example { margin-bottom: 56px; scroll-margin-top: 24px; }
  .ex-head {
    display: flex; justify-content: space-between; gap: 24px;
    align-items: flex-start; flex-wrap: wrap;
    margin-bottom: 14px;
  }
  h2 {
    font-size: 1.35rem;
    font-weight: 600;
    letter-spacing: -0.02em;
    margin-bottom: 6px;
  }
  .blurb { color: var(--muted); font-size: 0.95rem; line-height: 1.45; max-width: 36rem; }
  .tags { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 10px; }
  .tag {
    font-family: "JetBrains Mono", monospace;
    font-size: 0.68rem;
    color: var(--ok);
    border: 1px solid #2a4a38;
    background: #121a16;
    padding: 3px 8px;
  }
  .stats {
    text-align: right;
    font-family: "JetBrains Mono", monospace;
    font-size: 0.75rem;
    color: var(--muted);
    line-height: 1.7;
  }
  .stats b { color: var(--accent); font-size: 1.05rem; }
  .stats .mono { color: #6a7382; font-size: 0.68rem; }

  .pair {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 12px;
  }
  @media (max-width: 820px) { .pair { grid-template-columns: 1fr; } }
  figure {
    background: var(--bg1);
    border: 1px solid var(--line);
  }
  .cap {
    padding: 8px 12px;
    border-bottom: 1px solid var(--line);
    font-family: "JetBrains Mono", monospace;
    font-size: 0.72rem;
    color: var(--muted);
  }
  .cap strong { color: var(--ink); font-weight: 600; }
  figure img { display: block; width: 100%; height: auto; background: #0e1218; }

  footer {
    margin-top: 48px;
    padding-top: 20px;
    border-top: 1px solid var(--line);
    font-family: "JetBrains Mono", monospace;
    font-size: 0.72rem;
    color: var(--muted);
    line-height: 1.7;
  }
  footer code { color: var(--accent); }
</style>
</head>
<body>
  <div class="wrap">
    <header class="hero">
      <div class="brand">html-to-gum</div>
      <p class="lede">
        Chromium box tree → Gum <code>.gusx</code> → MonoGame.
        Every section is a real fixture: left is the browser, right is the converted screen.
      </p>
      <div class="meta">
        <span>Examples <b>${results.length}</b></span>
        <span>Exact match <b>${zeros}</b></span>
        <span>Mean residual <b>${avg.toFixed(2)}%</b></span>
      </div>
    </header>

    <nav class="toc">
${nav}
    </nav>

${sections}

    <footer>
      Rebuild: <code>cd converter && npx tsx build-demo.ts</code><br>
      Manifest: <code>demo/manifest.json</code> · Source fixtures under <code>converter/input/</code>
    </footer>
  </div>
</body>
</html>
`;

writeFileSync(join(demoDir, 'index.html'), html);
console.log(`\nWrote ${demoDir}/index.html (${results.length} examples)`);
console.log(`Mean residual ${avg.toFixed(2)}% · ${zeros} at 0%`);
