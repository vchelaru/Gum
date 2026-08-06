// @ts-nocheck
import { createHash } from 'node:crypto';
import { mkdirSync, writeFileSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';
import { parseBackgroundImageUrl } from './map.js';
import { installTsxEvaluateShim } from './tsx-evaluate-shim.js';

// Gum's LoaderManager.ValidTextureExtensions — WebP/AVIF/SVG are not loadable as-is;
// convert those to PNG. Dedup by content hash so the same bytes aren't written twice.
const CONTENT_TYPE_TO_EXT = {
  'image/png': 'png', 'image/jpeg': 'jpg', 'image/gif': 'gif',
  'image/bmp': 'bmp', 'image/svg+xml': 'svg',
  'image/webp': 'webp', 'image/avif': 'avif',
};
const URL_EXT_FALLBACK = new Set(['png', 'jpg', 'jpeg', 'gif', 'bmp', 'tga', 'svg', 'webp', 'avif']);
// SVG is a vector format — Pillow can't rasterize it (Image.open() only reads raster
// formats), so it's handled separately via rasterizeSvg() below, not this PIL round-trip.
const CONVERT_TO_PNG = new Set(['webp', 'avif']);
// GIF/BMP palette + browser color management diverge from Gum's raw decode — paint
// through Chromium so tiled backgrounds (Space Jam stars) match page screenshots.
const RASTERIZE_VIA_CHROMIUM = new Set(['gif', 'bmp']);

const SVG_MAX_DIM = 1024;
const SVG_UPSCALE = 2; // render at 2x the SVG's declared size for a crisper downscale

/**
 * Decode a `data:` image URL into bytes + MIME. Handles `;base64`, `;charset=…`, and
 * percent-encoded SVG payloads (`data:image/svg+xml;charset=US-ASCII,%3Csvg…`) used by
 * CSS selects (Pocket chevron). Returns null when the URL is not a usable data image.
 */
export function parseDataImageUrl(url) {
  if (!url || !url.startsWith('data:')) return null;
  const comma = url.indexOf(',');
  if (comma < 0) return null;
  const header = url.slice(5, comma); // after "data:"
  const payload = url.slice(comma + 1);
  const parts = header.split(';').filter(Boolean);
  const contentType = (parts[0] || '').trim().toLowerCase();
  const isBase64 = parts.some((p) => p.toLowerCase() === 'base64');
  try {
    if (isBase64) {
      return { buffer: Buffer.from(payload, 'base64'), contentType };
    }
    // Percent-encoded (or plain) SVG / text payloads.
    return { buffer: Buffer.from(decodeURIComponent(payload), 'utf8'), contentType };
  } catch {
    return null;
  }
}

/** Declared size from the root <svg> tag: width/height attrs, falling back to viewBox. */
export function svgIntrinsicSize(svgText) {
  const openTag = (svgText.match(/<svg\b[^>]*>/i) || [''])[0];
  const attr = (name) => {
    // Lookbehind requires a boundary right before the name so e.g. "stroke-width" doesn't
    // match `width` and "data-width" doesn't either — both common on real icon SVGs.
    const m = openTag.match(new RegExp(`(?<=[\\s"'])${name}\\s*=\\s*["']([^"']*)["']`, 'i'));
    if (!m) return null;
    const raw = m[1].trim();
    // Percentage/relative units aren't resolvable from the tag alone (e.g. width="100%")
    // — fall through to viewBox/default rather than silently treating "100" as px.
    if (!/^[\d.]+$/.test(raw)) return null;
    return parseFloat(raw);
  };
  let w = attr('width');
  let h = attr('height');
  if (!w || !h) {
    // Comma or whitespace separated per the SVG/CSS spec ("0 0 100 50" and "0,0,100,50"
    // are both valid).
    const vb = openTag.match(/viewBox\s*=\s*["']?\s*[\d.-]+[\s,]+[\d.-]+[\s,]+([\d.]+)[\s,]+([\d.]+)/i);
    if (vb) { w = w || parseFloat(vb[1]); h = h || parseFloat(vb[2]); }
  }
  if (!w || !h) return { width: 128, height: 128 };
  return { width: w, height: h };
}

/** Rasterize an SVG (vector, not loadable by Gum) to PNG via headless Chromium — the
 *  same technique convert.ts already uses for CSS-painted effects (rasterizeEffects).
 *  `browser` is the caller's already-open instance (see downloadImages), not launched
 *  here, so a page with several SVGs doesn't pay for a separate Chromium process. */
export async function rasterizeSvg(browser, buf) {
  const svgText = buf.toString('utf8');
  const { width, height } = svgIntrinsicSize(svgText);
  const scale = Math.min(SVG_UPSCALE, SVG_MAX_DIM / Math.max(width, height, 1));
  const renderW = Math.max(1, Math.round(width * scale));
  const renderH = Math.max(1, Math.round(height * scale));
  // SVGs can embed <script> — HtmlToGum already converts arbitrary third-party pages by
  // design, but there's no reason a static icon/logo raster needs script execution, so
  // disable it here specifically.
  const page = await browser.newPage({
    viewport: { width: renderW, height: renderH }, deviceScaleFactor: 1, javaScriptEnabled: false,
  });
  try {
    await installTsxEvaluateShim(page);
    await page.setContent(
      `<!doctype html><html><body style="margin:0;width:${renderW}px;height:${renderH}px">${svgText}</body></html>`,
    );
    await page.evaluate(({ width, height }) => {
      const svg = document.querySelector('svg');
      if (!svg) return;
      // Without a viewBox, resizing via CSS doesn't rescale content — it stays pinned to
      // its native top-left region, cropping the rest. Synthesize one from the declared
      // (pre-scale) size so CSS width/height:100% scales the actual artwork.
      if (!svg.hasAttribute('viewBox')) svg.setAttribute('viewBox', `0 0 ${width} ${height}`);
      svg.style.width = '100%';
      svg.style.height = '100%';
    }, { width, height });
    return await page.screenshot({ omitBackground: true });
  } finally {
    await page.close();
  }
}

function sha1(buf) {
  return createHash('sha1').update(buf).digest('hex').slice(0, 12);
}

const IMAGE_CT_RE = /^image\//i;

/** Attach a response listener; returns a Map<url, {buffer, contentType}>. Sites with a
 *  server-randomized/rotating hero banner (same URL, different bytes per request — Team
 *  Liquid's front page) return different content to downloadImages()'s own fetch() than
 *  what the page actually rendered. Capturing the response the live page already received
 *  guarantees the downloaded asset matches the pixels the reference screenshot shows. */
export function attachImageCapture(page) {
  const captured = new Map();
  page.on('response', async (res) => {
    try {
      const ct = (res.headers()['content-type'] || '').split(';')[0].trim();
      if (!IMAGE_CT_RE.test(ct)) return;
      if (res.status() < 200 || res.status() >= 300) return;
      const buffer = Buffer.from(await res.body());
      if (buffer.length > 0) captured.set(res.url(), { buffer, contentType: ct });
    } catch {
      // Body may be unavailable for cached/opaque responses — ignore.
    }
  });
  return captured;
}

/** Width/height from a PNG's IHDR chunk (bytes 16-23, big-endian) — used to recover the
 *  actual rasterized pixel size of an SVG->PNG conversion (SVG_UPSCALE / SVG_MAX_DIM in
 *  rasterizeSvg mean it's not simply the SVG's own declared intrinsic size) without
 *  duplicating that scale math here. */
export function pngDimensions(buf) {
  return { width: buf.readUInt32BE(16), height: buf.readUInt32BE(20) };
}

function sniffExt(buf) {
  if (buf.length >= 12 && buf.toString('ascii', 0, 4) === 'RIFF' && buf.toString('ascii', 8, 12) === 'WEBP') {
    return 'webp';
  }
  if (buf.length >= 8 && buf[0] === 0x89 && buf[1] === 0x50) return 'png';
  if (buf.length >= 3 && buf[0] === 0xff && buf[1] === 0xd8) return 'jpg';
  if (buf.length >= 5 && buf.toString('ascii', 0, 5) === '<?xml') return 'svg';
  if (buf.length >= 4 && buf.toString('ascii', 0, 4) === '<svg') return 'svg';
  return null;
}

/** Re-paint a raster (GIF/BMP) through Chromium so SourceFile pixels match what
 *  `page.screenshot` captures for CSS backgrounds / <img> — Gum loads files
 *  without the browser's color-management pass. */
export async function rasterizeRasterViaChromium(browser, buf, mime = 'image/gif') {
  const b64 = buf.toString('base64');
  const page = await browser.newPage({
    viewport: { width: 2048, height: 2048 },
    deviceScaleFactor: 1,
  });
  try {
    await installTsxEvaluateShim(page);
    await page.setContent(
      `<!doctype html><html><body style="margin:0;background:transparent">` +
      `<img id="i" src="data:${mime};base64,${b64}" style="display:block"/></body></html>`,
    );
    const loc = page.locator('#i');
    await loc.waitFor({ state: 'visible' });
    await page.evaluate(async () => {
      const img = document.getElementById('i');
      if (img && img.decode) await img.decode();
    });
    const box = await loc.boundingBox();
    if (!box || box.width < 1 || box.height < 1) {
      throw new Error('image has empty bounding box after decode');
    }
    return await page.screenshot({
      omitBackground: true,
      clip: {
        x: Math.floor(box.x),
        y: Math.floor(box.y),
        width: Math.ceil(box.width),
        height: Math.ceil(box.height),
      },
    });
  } finally {
    await page.close();
  }
}

/** Convert webp/avif bytes to PNG via Pillow (already used by regress). */
function convertToPng(buf, hintExt) {
  const script = `
import sys
from PIL import Image
import io
raw = sys.stdin.buffer.read()
im = Image.open(io.BytesIO(raw))
if im.mode not in ('RGB', 'RGBA'):
    im = im.convert('RGBA')
out = io.BytesIO()
im.save(out, format='PNG')
sys.stdout.buffer.write(out.getvalue())
`;
  const r = spawnSync('python', ['-c', script], { input: buf, maxBuffer: 32 * 1024 * 1024 });
  if (r.status !== 0) {
    const err = (r.stderr && r.stderr.toString()) || 'convert failed';
    throw new Error(`${hintExt}→png: ${err.trim()}`);
  }
  return Buffer.from(r.stdout);
}

export async function downloadImages(
  root: import('./types.js').BoxNode,
  outDir: string,
  browser: import('playwright-core').Browser,
  capturedImages: Map<string, { buffer: Buffer, contentType: string }> = new Map(),
) {
  const urls = new Set();
  (function collect(node) {
    if (node.imgSrc) urls.add(node.imgSrc);
    const bg = parseBackgroundImageUrl(node.style.backgroundImage);
    if (bg) urls.add(bg);
    const bi = parseBackgroundImageUrl(node.style.borderImageSource);
    if (bi) urls.add(bi);
    for (const child of node.children) collect(child);
  })(root);

  const assetMap = new Map();
  // url -> actual rasterized pixel {width,height}, populated only for SVG sources (the
  // only path where the on-disk asset's pixel size diverges from the box tree's captured
  // naturalWidth/naturalHeight — see rasterizeSvg's SVG_UPSCALE/SVG_MAX_DIM). map.ts's
  // TextureLeft/Top/Width/Height crop math (cover-fit and background-position sprites)
  // scales by this against naturalWidth/Height so crop coordinates land on the right
  // pixels in the actual (possibly upscaled) asset file.
  const assetSizeMap = new Map();
  if (urls.size === 0) return { assetMap, assetSizeMap };

  mkdirSync(outDir, { recursive: true });
  const hashToFile = new Map();
  let i = 0;
  for (const url of urls) {
    try {
      let buf;
      let contentType = '';
      if (url.startsWith('file:')) {
        // Node fetch() does not support file:// — local fixtures / file:// pages.
        buf = readFileSync(fileURLToPath(url));
      } else if (url.startsWith('data:')) {
        const parsed = parseDataImageUrl(url);
        if (!parsed) {
          console.warn(`  ! bad data URL: ${url.slice(0, 64)}…`);
          continue;
        }
        ({ buffer: buf, contentType } = parsed);
      } else if (capturedImages.has(url)) {
        // Prefer the exact bytes the live page already received over re-fetching — a
        // fresh fetch() can land on a different random rotation for the same URL.
        ({ buffer: buf, contentType } = capturedImages.get(url));
      } else {
        const res = await fetch(url);
        if (!res.ok) {
          console.warn(`  ! image download failed (${res.status}): ${url}`);
          continue;
        }
        buf = Buffer.from(await res.arrayBuffer());
        contentType = (res.headers.get('content-type') || '').split(';')[0].trim();
      }
      let ext = CONTENT_TYPE_TO_EXT[contentType];
      if (!ext) {
        const urlExt = (url.split(/[?#]/)[0].split('.').pop() || '').toLowerCase();
        if (URL_EXT_FALLBACK.has(urlExt)) ext = urlExt;
      }
      if (!ext) ext = sniffExt(buf);
      if (!ext) {
        console.warn(`  ! unsupported/unrecognized image format (content-type: "${contentType}"): ${url}`);
        continue;
      }

      let outExt = ext === 'jpeg' ? 'jpg' : ext;
      let outBuf = buf;
      if (ext === 'svg') {
        try {
          outBuf = await rasterizeSvg(browser, buf);
          outExt = 'png';
          assetSizeMap.set(url, pngDimensions(outBuf));
          console.log(`  image: rasterized svg → png`);
        } catch (e) {
          console.warn(`  ! svg rasterize failed: ${e.message} — skipped ${url}`);
          continue;
        }
      } else if (RASTERIZE_VIA_CHROMIUM.has(ext)) {
        try {
          const mime = ext === 'bmp' ? 'image/bmp' : 'image/gif';
          outBuf = await rasterizeRasterViaChromium(browser, buf, mime);
          outExt = 'png';
          assetSizeMap.set(url, pngDimensions(outBuf));
          console.log(`  image: chromium-painted ${ext} → png`);
        } catch (e) {
          console.warn(`  ! ${ext} chromium paint failed: ${e.message} — keeping original`);
        }
      } else if (CONVERT_TO_PNG.has(ext)) {
        try {
          outBuf = convertToPng(buf, ext);
          outExt = 'png';
          console.log(`  image: converted ${ext} → png`);
        } catch (e) {
          console.warn(`  ! ${e.message} — skipped ${url}`);
          continue;
        }
      }

      const hash = sha1(outBuf);
      if (hashToFile.has(hash)) {
        assetMap.set(url, hashToFile.get(hash));
        continue;
      }
      const filename = `img${i++}_${hash}.${outExt}`;
      writeFileSync(join(outDir, filename), outBuf);
      const rel = `Images/${filename}`;
      hashToFile.set(hash, rel);
      assetMap.set(url, rel);
    } catch (e) {
      console.warn(`  ! image download error: ${url} (${e.message})`);
    }
  }
  return { assetMap, assetSizeMap };
}
