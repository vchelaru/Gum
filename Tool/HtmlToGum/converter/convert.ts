// @ts-nocheck
// HTML/CSS → Gum .gusx via Chromium's computed box tree.
//
// Usage:
//   npx tsx convert.ts <html> <selector> <screenName> [width] [height] [flags...]
//
// Flags:
//   --responsive[=narrow,wide]  Dual-viewport unit inference (DEFAULT). Geometry always
//                               comes from the requested [width]x[height] viewport; the
//                               optional narrow,wide pair are only the training samples.
//                               When omitted, a nearby second width is chosen automatically
//                               (avoids common CSS breakpoints when possible).
//   --no-responsive             Single-viewport Absolute — fixed HUD / regress escape hatch.
//   --compare-naive             Also emit <Screen>Naive (Absolute control) for A/B.
//   --tag=<name>                Save chromium-<name>.png (and chromium.png).
//   --out=<dir>                 Write .gumx / Screens / Images / Fonts / FontCache here
//                               instead of ../.out. Chromium shots go in <dir> when --out
//                               is set; otherwise under ../.regress/ (gitignored).
//
// After emit: gumcli fonts (via gumcli.ts) bakes FontCache from Font/FontSize and any
// Fonts/*.ttf web-font instances.
//
// Examples:
//   npx tsx convert.ts ../samples/features/inventory.html #panel InventoryScreen 800 600
//   npx tsx convert.ts ../samples/features/responsive-sidebar.html #layout R 800 400 --responsive=400,1200
//   npx tsx convert.ts ../samples/features/inventory.html #panel Inv 800 600 --no-responsive
//   npx tsx convert.ts page.html body Hud 800 600 --out=/tmp/html-to-gum-stage
//   npm run gumcli -- fonts ../.out/Generated.gumx
import { chromium } from 'playwright-core';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { dirname, resolve, join, isAbsolute } from 'node:path';
import { mkdirSync, cpSync, readFileSync, writeFileSync, rmSync, copyFileSync } from 'node:fs';
import { extractBoxTree } from './extract.js';
import { mapTreeToScreen, toGusx } from './map.js';
import { downloadImages } from './assets.js';
import { computeResponsiveMap } from './responsive.js';
import {
  attachFontCapture, collectFontFaceRules, materializeWebFonts, runGumcliFonts,
} from './fonts.js';
import { generateNineSliceAssets } from './nineslice.js';
import { installTsxEvaluateShim } from './tsx-evaluate-shim.js';
import { samplePath } from './samples-path.js';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '..');

function parseArgs(argv) {
  const positional = [];
  // responsive defaults ON — matches the project goal (emit layout that survives resize).
  const flags = {
    responsive: true, narrow: null, wide: null, compareNaive: false, tag: null, out: null,
  };
  for (const a of argv) {
    if (a === '--responsive') {
      flags.responsive = true;
    } else if (a.startsWith('--responsive=')) {
      flags.responsive = true;
      const [n, w] = a.slice('--responsive='.length).split(',');
      flags.narrow = parseInt(n, 10);
      flags.wide = parseInt(w, 10);
    } else if (a === '--no-responsive') {
      flags.responsive = false;
    } else if (a === '--compare-naive') {
      flags.compareNaive = true;
    } else if (a.startsWith('--tag=')) {
      flags.tag = a.slice('--tag='.length);
    } else if (a.startsWith('--out=')) {
      flags.out = a.slice('--out='.length);
    } else if (a === '--out') {
      console.error('--out requires a path: --out=<dir>');
      process.exit(2);
    } else if (a.startsWith('--')) {
      console.error(`Unknown flag: ${a}`);
      process.exit(2);
    } else {
      positional.push(a);
    }
  }
  return { positional, flags };
}

/** Second training width when the user didn't pass --responsive=n,w.
 *  Prefer a nearby width that does not cross common CSS breakpoints (Bootstrap-ish),
 *  so structure stays comparable while still detecting %-of-parent scaling. */
function defaultAltWidth(primaryW) {
  const delta = Math.max(64, Math.round(primaryW * 0.1));
  const breakpoints = [576, 768, 992, 1200];
  const crosses = (a, b) => breakpoints.some((bp) => (a - bp) * (b - bp) < 0);
  const down = primaryW - delta;
  const up = primaryW + delta;
  if (down >= 320 && !crosses(primaryW, down)) return down;
  if (!crosses(primaryW, up)) return up;
  return down >= 320 ? down : up;
}

const { positional, flags } = parseArgs(process.argv.slice(2));

const htmlArg = positional[0] || samplePath('features', 'inventory.html');
const htmlFile = /^https?:\/\//i.test(htmlArg)
  ? htmlArg
  : (isAbsolute(htmlArg) ? htmlArg : resolve(__dirname, htmlArg));
const rootSelector = positional[1] || '#panel';
const screenName = positional[2] || 'GeneratedScreen';
const VIEWPORT = {
  width: parseInt(positional[3], 10) || 800,
  height: parseInt(positional[4], 10) || 600,
};

const TRAIN_H = VIEWPORT.height;
let TRAIN_NARROW;
let TRAIN_WIDE;
if (flags.responsive) {
  if (flags.narrow != null && flags.wide != null) {
    TRAIN_NARROW = Math.min(flags.narrow, flags.wide);
    TRAIN_WIDE = Math.max(flags.narrow, flags.wide);
  } else {
    const alt = defaultAltWidth(VIEWPORT.width);
    TRAIN_NARROW = Math.min(VIEWPORT.width, alt);
    TRAIN_WIDE = Math.max(VIEWPORT.width, alt);
  }
}
const tag = flags.tag || screenName.replace(/[^a-zA-Z0-9]+/g, '-').replace(/^-|-$/g, '').toLowerCase() || 'out';

const scaffoldDir = join(__dirname, 'scaffold');
const outProjectDir = flags.out
  ? resolve(flags.out)
  : join(repoRoot, '.out');
// Screenshots land in .regress/ (gitignored) unless --out= is set.
const regressDir = join(repoRoot, '.regress');
const chromiumPng = flags.out
  ? join(outProjectDir, 'chromium.png')
  : join(regressDir, 'chromium.png');
const chromiumTagged = flags.out
  ? join(outProjectDir, `chromium-${tag}.png`)
  : join(regressDir, `chromium-${tag}.png`);
if (!flags.out) mkdirSync(regressDir, { recursive: true });

async function renderTree(browser, width, height, { captureFonts = false } = {}) {
  const page = await browser.newPage({ viewport: { width, height }, deviceScaleFactor: 1 });
  await installTsxEvaluateShim(page);
  const capturedFonts = captureFonts ? attachFontCapture(page) : null;
  const url = /^https?:\/\//i.test(htmlFile) ? htmlFile : pathToFileURL(htmlFile).href;
  await page.goto(url, { waitUntil: 'networkidle' });
  await installTsxEvaluateShim(page); // re-apply after navigation clears some contexts
  await page.evaluate(() => document.fonts.ready);
  // SPA / framework roots (e.g. Gameface Solid #root) need a beat after networkidle.
  try {
    await page.waitForFunction(
      (sel) => {
        const el = document.querySelector(sel);
        return el && (el.children.length > 0 || (el.textContent || '').trim().length > 0);
      },
      rootSelector,
      { timeout: 8000 },
    );
  } catch {
    /* static pages already have content */
  }
  const fontFaceRules = captureFonts ? await collectFontFaceRules(page) : [];
  const tree = await page.evaluate(extractBoxTree, rootSelector);
  return { page, tree, width, height, capturedFonts, fontFaceRules, pageUrl: url };
}

/**
 * Screenshot nodes flagged needsRaster (gradients / CSS filter / border-image) into Images/.
 * Backdrop-only parents: hide element children (and own text paint) so the sprite is
 * chrome-only; structured kids / Text labels are still emitted by map.mjs. Filter
 * parents bake the whole subtree (CSS filter applies to descendants).
 */
async function rasterizeEffects(page, tree, imagesDir, assetMap, rootSelector) {
  let i = 0;

  async function withChromeOnly(path, mark, fn) {
    await page.evaluate(({ rootSelector, path, mark }) => {
      function isVisible(el) {
        const cs = getComputedStyle(el);
        return cs.opacity !== '0' && cs.display !== 'none' && cs.visibility !== 'hidden';
      }
      let el = document.querySelector(rootSelector);
      if (!el) return;
      for (const idx of path) {
        const kids = Array.from(el.children).filter(isVisible);
        el = kids[idx];
        if (!el) return;
      }
      el.dataset.htmlToGumRaster = mark;
      // Hide descendants so only this node's paint (bg + border-image) is captured.
      for (const child of Array.from(el.children).filter(isVisible)) {
        child.dataset.htmlToGumVis = child.style.visibility || '';
        child.style.visibility = 'hidden';
      }
      // Text leaves: hide own glyphs so map can emit a structured Text label on top.
      el.dataset.htmlToGumColor = el.style.color || '';
      el.dataset.htmlToGumFill = el.style.webkitTextFillColor || '';
      el.style.color = 'transparent';
      el.style.webkitTextFillColor = 'transparent';
    }, { rootSelector, path, mark });
    try {
      await fn();
    } finally {
      await page.evaluate((mark) => {
        const el = document.querySelector(`[data-html-to-gum-raster="${mark}"]`);
        if (!el) return;
        for (const child of el.children) {
          if (child.dataset && 'htmlToGumVis' in child.dataset) {
            child.style.visibility = child.dataset.htmlToGumVis;
            delete child.dataset.htmlToGumVis;
          }
        }
        if ('htmlToGumColor' in el.dataset) {
          el.style.color = el.dataset.htmlToGumColor;
          delete el.dataset.htmlToGumColor;
        }
        if ('htmlToGumFill' in el.dataset) {
          el.style.webkitTextFillColor = el.dataset.htmlToGumFill;
          delete el.dataset.htmlToGumFill;
        }
        delete el.dataset.htmlToGumRaster;
      }, mark);
    }
  }

  async function walk(node, path) {
    if (node.style?.needsRaster) {
      const r = node.rect;
      const clip = {
        x: Math.max(0, Math.floor(r.x)),
        y: Math.max(0, Math.floor(r.y)),
        width: Math.max(1, Math.ceil(r.width)),
        height: Math.max(1, Math.ceil(r.height)),
      };
      mkdirSync(imagesDir, { recursive: true });
      const filename = `raster${i++}.png`;
      const key = `raster:${path.length ? path.join('.') : 'root'}`;
      const omitBg = !!node.style.rasterOmitBackground;
      const shot = async () => {
        // Pseudo-element chrome (hamburger bars): element screenshots only capture the
        // middle bar's tiny border-box. Use the inflated clip from extract (page shot).
        const pseudoChrome = omitBg && node.tag !== 'svg';
        if (omitBg && !pseudoChrome) {
          const mark = `htg${i}`;
          const found = await page.evaluate(({ rootSelector, path, mark }) => {
            function isVisible(el) {
              const cs = getComputedStyle(el);
              return cs.opacity !== '0' && cs.display !== 'none' && cs.visibility !== 'hidden';
            }
            let el = document.querySelector(rootSelector);
            if (!el) return false;
            for (const idx of path) {
              const kids = Array.from(el.children).filter(isVisible);
              el = kids[idx];
              if (!el) return false;
            }
            el.setAttribute('data-html-to-gum-shot', mark);
            return true;
          }, { rootSelector, path, mark });
          if (found) {
            try {
              await page.locator(`[data-html-to-gum-shot="${mark}"]`).screenshot({
                path: join(imagesDir, filename),
                omitBackground: true,
              });
            } finally {
              await page.evaluate((mark) => {
                document.querySelector(`[data-html-to-gum-shot="${mark}"]`)
                  ?.removeAttribute('data-html-to-gum-shot');
              }, mark);
            }
            return;
          }
        }
        await page.screenshot({
          path: join(imagesDir, filename),
          clip,
          omitBackground: false,
        });
      };
      const backdropOnly = !node.style.rasterWholeSubtree
        && (node.children.length > 0 || !!(node.text && String(node.text).trim()));
      if (backdropOnly) await withChromeOnly(path, key, shot);
      else await shot();
      node.rasterSrc = key;
      assetMap.set(key, `Images/${filename}`);
    }
    // Path indices must match el.children (element-only). Synthetic #text leaves from
    // extract's phrasing walk are skipped so withChromeOnly stays aligned.
    let elemIdx = 0;
    for (let c = 0; c < node.children.length; c++) {
      const child = node.children[c];
      if (child.tag === '#text') await walk(child, path);
      else {
        await walk(child, [...path, elemIdx]);
        elemIdx++;
      }
    }
  }
  await walk(tree, []);
}

async function main() {
  const browser = await chromium.launch();
  let tree;
  let responsiveMap = null;
  let mismatches = [];
  let shotPage;

  // Geometry always comes from the requested VIEWPORT. Training samples (when
  // responsive) only drive unit inference — PercentageOfParent / RelativeToParent /
  // Absolute — and must not replace the design-size box tree.
  const primary = await renderTree(browser, VIEWPORT.width, VIEWPORT.height, { captureFonts: true });
  tree = primary.tree;
  shotPage = primary.page;
  const capturedFonts = primary.capturedFonts || new Map();
  const fontFaceRules = primary.fontFaceRules || [];
  const pageUrl = primary.pageUrl;

  if (flags.responsive) {
    const cache = new Map();
    cache.set(`${VIEWPORT.width}x${VIEWPORT.height}`, primary.tree);
    async function treeAt(w, h) {
      const key = `${w}x${h}`;
      if (cache.has(key)) return cache.get(key);
      const r = await renderTree(browser, w, h);
      await r.page.close();
      cache.set(key, r.tree);
      return r.tree;
    }
    const treeNarrow = await treeAt(TRAIN_NARROW, TRAIN_H);
    const treeWide = await treeAt(TRAIN_WIDE, TRAIN_H);
    ({ map: responsiveMap, mismatches } = computeResponsiveMap(
      treeNarrow, treeWide,
      { width: TRAIN_NARROW, height: TRAIN_H },
      { width: TRAIN_WIDE, height: TRAIN_H },
    ));
  }

  rmSync(outProjectDir, { recursive: true, force: true });
  mkdirSync(join(outProjectDir, 'Screens'), { recursive: true });
  cpSync(join(scaffoldDir, 'Standards'), join(outProjectDir, 'Standards'), { recursive: true });
  const imagesDir = join(outProjectDir, 'Images');
  const fontsDir = join(outProjectDir, 'Fonts');

  const assetMap = new Map();
  await rasterizeEffects(shotPage, tree, imagesDir, assetMap, rootSelector);

  const clip = {
    x: Math.max(0, Math.floor(tree.rect.x)),
    y: Math.max(0, Math.floor(tree.rect.y)),
    width: Math.min(VIEWPORT.width - Math.floor(tree.rect.x), Math.ceil(tree.rect.width)),
    height: Math.min(VIEWPORT.height - Math.floor(tree.rect.y), Math.ceil(tree.rect.height)),
  };
  await shotPage.screenshot({ path: chromiumPng, clip });
  copyFileSync(chromiumPng, chromiumTagged);
  await shotPage.close();
  await browser.close();

  const downloaded = await downloadImages(tree, imagesDir);
  for (const [k, v] of downloaded) assetMap.set(k, v);

  const fontMap = await materializeWebFonts({
    tree, captured: capturedFonts, rules: fontFaceRules, fontsDir, pageUrl,
  });
  const nineSliceMap = generateNineSliceAssets(tree, imagesDir, assetMap);

  const mapped = mapTreeToScreen(tree, assetMap, responsiveMap, fontMap, nineSliceMap);
  const screens = [{ name: screenName, mapped, gusx: toGusx(screenName, mapped) }];

  if (flags.responsive && flags.compareNaive) {
    const naive = mapTreeToScreen(tree, assetMap, null, fontMap, nineSliceMap);
    const naiveName = `${screenName}Naive`;
    screens.push({ name: naiveName, mapped: naive, gusx: toGusx(naiveName, naive) });
  }

  const screenRefs = screens.map((s) => `  <ScreenReference Name="${s.name}" />`).join('\n');
  const gumx = readFileSync(join(scaffoldDir, 'Sample.gumx'), 'utf8')
    .replace('  <ScreenReference Name="InventoryPanelScreen" />', screenRefs);
  writeFileSync(join(outProjectDir, 'Generated.gumx'), gumx);
  for (const s of screens) writeFileSync(join(outProjectDir, 'Screens', `${s.name}.gusx`), s.gusx);
  writeFileSync(join(outProjectDir, 'boxtree.json'), JSON.stringify(tree, null, 2));

  // B-first: bake FontCache from Font/FontSize (+ .ttf paths) before the MonoGame host loads.
  try {
    runGumcliFonts(join(outProjectDir, 'Generated.gumx'));
  } catch (e) {
    console.warn(`  ! gumcli fonts failed: ${e.message}`);
  }

  console.log(`box tree root: ${tree.tag}#${tree.id} (${Math.round(tree.rect.width)}x${Math.round(tree.rect.height)})`);
  if (flags.responsive) {
    console.log(`responsive: trained on ${TRAIN_NARROW}px + ${TRAIN_WIDE}px (geometry from ${VIEWPORT.width}×${VIEWPORT.height})`);
    const inferredPct = [...responsiveMap.values()].filter((v) =>
      v.width.units === 'PercentageOfParent' || v.height.units === 'PercentageOfParent'
      || v.width.units === 'RelativeToParent' || v.height.units === 'RelativeToParent').length;
    const ambiguous = [...responsiveMap.values()].filter((v) => v.width.ambiguous || v.height.ambiguous).length;
    console.log(`nodes with inferred %-or-fill axis: ${inferredPct}, ambiguous: ${ambiguous}`);
    if (mismatches.length) {
      console.log('structure mismatches (narrow vs wide DOM differs):');
      for (const m of mismatches) console.log('  - ' + m);
    }
  } else {
    console.log('responsive: off (--no-responsive)');
  }
  for (const s of screens) {
    console.log(`${s.name}: ${s.mapped.instances.length} instances, ${s.mapped.variables.length} vars`);
  }
  const rasterCount = [...assetMap.keys()].filter((k) => String(k).startsWith('raster:')).length;
  console.log(`images downloaded: ${downloaded.size}, rasterized effects: ${rasterCount}, web fonts: ${fontMap.size}, nineslices: ${nineSliceMap.size}`);
  console.log(`wrote: ${join(outProjectDir, 'Screens', screenName + '.gusx')}`);
  console.log(`wrote: ${chromiumTagged}`);
  const warnings = screens.flatMap((s) => s.mapped.warnings);
  if (warnings.length) {
    console.log('warnings:');
    for (const w of warnings) console.log('  - ' + w);
  } else {
    console.log('warnings: none');
  }
}

main().catch((e) => { console.error(e); process.exit(1); });
