// @ts-nocheck
// Generate / attach NineSlice textures (§5.5):
// 1. CSS border-image-source + border-image-slice → download already in assetMap
// 2. Else uniform border + border-radius → synthesize PNG via generate_nineslice.py
import { mkdirSync, existsSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';
import { parseColor, parseBackgroundImageUrl } from './map.js';

const __dirname = dirname(fileURLToPath(import.meta.url));

function isTransparent(c) {
  return !c || c.a === 0;
}

function bordersUniform(style) {
  const widths = [
    style.borderTopWidth || 0,
    style.borderRightWidth || 0,
    style.borderBottomWidth || 0,
    style.borderLeftWidth || 0,
  ];
  const colors = [
    parseColor(style.borderTopColor),
    parseColor(style.borderRightColor),
    parseColor(style.borderBottomColor),
    parseColor(style.borderLeftColor),
  ];
  const drawn = widths.map((w, i) => ({ w, c: colors[i] })).filter((s) => s.w > 0 && !isTransparent(s.c));
  if (drawn.length === 0) return null;
  const w0 = drawn[0].w;
  const c0 = drawn[0].c;
  if (!drawn.every((s) => s.w === w0 && s.c.r === c0.r && s.c.g === c0.g && s.c.b === c0.b && s.c.a === c0.a)) {
    return null;
  }
  return { width: w0, color: c0 };
}

/** Synthesized rounded+border panel (no border-image). */
export function nineSliceCandidate(style) {
  if (parseBackgroundImageUrl(style.borderImageSource)) return null; // handled separately
  const radius = style.borderTopLeftRadius || 0;
  const border = bordersUniform(style);
  if (!border || radius < 1) return null;
  const fill = parseColor(style.backgroundColor);
  if (isTransparent(fill)) return null;
  if (style.boxShadow && style.boxShadow !== 'none') return null;
  return { radius, borderWidth: border.width, borderColor: border.color, fill };
}

/**
 * @param {Map<string,string>} assetMap url → Images/...
 * @returns {Map<string, {sourceFile:string, frameWidth:number, tiling?:boolean}>}
 */
export function generateNineSliceAssets(
  root: import('./types.js').BoxNode,
  imagesDir: string,
  assetMap: Map<string, string> = new Map(),
) {
  mkdirSync(imagesDir, { recursive: true });
  const map = new Map();
  let i = 0;

  function walk(node, path) {
    const key = path.join('.');
    const style = node.style;
    if (!style) {
      (node.children || []).forEach((c, idx) => walk(c, [...path, idx]));
      return;
    }

    const biUrl = parseBackgroundImageUrl(style.borderImageSource);
    if (biUrl && assetMap.get(biUrl)) {
      const slice = style.borderImageSlice || 0;
      const frame = slice > 0 ? slice : 32;
      const tiling = /repeat|round|space/i.test(style.borderImageRepeat || '');
      map.set(key, {
        sourceFile: assetMap.get(biUrl),
        frameWidth: frame,
        tiling,
      });
      console.log(`  nineslice(border-image): ${key || '(root)'} → ${assetMap.get(biUrl)} frame=${frame}`);
    } else {
      const cand = nineSliceCandidate(style);
      if (cand) {
        const filename = `ns${i++}.png`;
        const outPath = join(imagesDir, filename);
        const fill = `${cand.fill.r},${cand.fill.g},${cand.fill.b},${cand.fill.a}`;
        const border = `${cand.borderColor.r},${cand.borderColor.g},${cand.borderColor.b},${cand.borderColor.a}`;
        const frame = Math.max(cand.radius, cand.borderWidth);
        const r = spawnSync('python', [
          join(__dirname, 'generate_nineslice.py'), outPath,
          '--fill', fill, '--border', border,
          '--width', String(Math.round(cand.borderWidth)),
          '--radius', String(Math.round(cand.radius)),
        ], { encoding: 'utf8' });
        if (r.stdout) process.stdout.write(r.stdout);
        if (r.stderr) process.stderr.write(r.stderr);
        if (r.status === 0 && existsSync(outPath)) {
          map.set(key, { sourceFile: `Images/${filename}`, frameWidth: frame });
          console.log(`  nineslice: ${key || '(root)'} → Images/${filename} frame=${frame}`);
        }
      }
    }
    (node.children || []).forEach((c, idx) => walk(c, [...path, idx]));
  }

  walk(root, []);
  return map;
}
