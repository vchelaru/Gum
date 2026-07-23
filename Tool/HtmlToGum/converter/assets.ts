// @ts-nocheck
import { createHash } from 'node:crypto';
import { mkdirSync, writeFileSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';
import { parseBackgroundImageUrl } from './map.js';

// Gum's LoaderManager.ValidTextureExtensions — WebP/AVIF/SVG are not loadable as-is;
// convert those to PNG. Dedup by content hash so the same bytes aren't written twice.
const CONTENT_TYPE_TO_EXT = {
  'image/png': 'png', 'image/jpeg': 'jpg', 'image/gif': 'gif',
  'image/bmp': 'bmp', 'image/svg+xml': 'svg',
  'image/webp': 'webp', 'image/avif': 'avif',
};
const URL_EXT_FALLBACK = new Set(['png', 'jpg', 'jpeg', 'gif', 'bmp', 'tga', 'svg', 'webp', 'avif']);
const CONVERT_TO_PNG = new Set(['webp', 'avif', 'svg']);

function sha1(buf) {
  return createHash('sha1').update(buf).digest('hex').slice(0, 12);
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

/** Convert webp/avif/svg bytes to PNG via Pillow (already used by regress). */
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

export async function downloadImages(root: import('./types.js').BoxNode, outDir: string) {
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
  if (urls.size === 0) return assetMap;

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
        const m = url.match(/^data:([^;,]+)?(?:;base64)?,([\s\S]*)$/i);
        if (!m) {
          console.warn(`  ! bad data URL: ${url.slice(0, 64)}…`);
          continue;
        }
        contentType = (m[1] || '').trim();
        buf = Buffer.from(m[2], /;base64,/i.test(url) ? 'base64' : 'utf8');
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
      if (CONVERT_TO_PNG.has(ext)) {
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
  return assetMap;
}
