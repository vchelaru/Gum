// @ts-nocheck
// Web-font extraction for the build-time FontCache path (§4.4 / B-first).
//
// 1. Capture font network responses while Playwright loads the page.
// 2. Read @font-face rules from CSSOM.
// 3. For each distinct (family, weight, style) used by text nodes in the box tree,
//    pick a matching face, instantiate variable axes via instantiate_font.py, and
//    write a static .ttf under Fonts/.
// 4. map.mjs emits Font = "Fonts/….ttf" so gumcli fonts bakes FontCache/*_ttf.fnt.
import { mkdirSync, writeFileSync, existsSync, rmSync, readFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';
import { nodeTsxArgs } from './tsx-run.js';

const __dirname = dirname(fileURLToPath(import.meta.url));

const FONT_URL_RE = /\.(woff2?|ttf|otf)(?:$|\?)/i;
const FONT_CT_RE = /font\/|application\/font|application\/x-font|woff/i;

export function parseFontWeight(w) {
  if (w == null || w === '') return 400;
  const s = String(w).trim().toLowerCase();
  if (s === 'bold' || s === 'bolder') return 700;
  if (s === 'normal' || s === 'lighter') return 400;
  // CSS can return "700" or a variable range "100 900" — take the first number.
  const m = s.match(/(\d{2,3})/);
  return m ? parseInt(m[1], 10) : 400;
}

export function parseFontStyle(style) {
  const s = String(style || 'normal').toLowerCase();
  return s === 'italic' || s === 'oblique' ? 'italic' : 'normal';
}

export function firstFontFamily(fam) {
  return (fam || '').split(',')[0].replace(/["']/g, '').trim();
}

/** Normalize for matching: "Inter Var" / "Inter" / '"Inter"' → "inter" */
export function normalizeFamily(fam) {
  return firstFontFamily(fam)
    .toLowerCase()
    .replace(/\s+var\b/g, '')
    .replace(/\s+/g, ' ')
    .trim();
}

export function fontFaceKey(family, weight, style) {
  return `${normalizeFamily(family)}|${parseFontWeight(weight)}|${parseFontStyle(style)}`;
}

/** Attach a response listener; returns a Map<url, {buffer, contentType}>. */
export function attachFontCapture(page) {
  const captured = new Map();
  page.on('response', async (res) => {
    try {
      const url = res.url();
      const ct = (res.headers()['content-type'] || '').split(';')[0].trim();
      if (!FONT_CT_RE.test(ct) && !FONT_URL_RE.test(url)) return;
      if (res.status() < 200 || res.status() >= 300) return;
      const buffer = Buffer.from(await res.body());
      if (buffer.length > 0) captured.set(url, { buffer, contentType: ct });
    } catch {
      // Body may be unavailable for cached/opaque responses — ignore.
    }
  });
  return captured;
}

/** Parse @font-face blocks from CSS text (cross-origin sheets can't use cssRules). */
export function parseFontFacesFromCss(cssText) {
  const out = [];
  const re = /@font-face\s*\{([^}]*)\}/gi;
  let m;
  while ((m = re.exec(cssText))) {
    const body = m[1];
    const prop = (name) => {
      const pm = body.match(new RegExp(`${name}\\s*:\\s*([^;]+)`, 'i'));
      return pm ? pm[1].trim() : '';
    };
    const family = prop('font-family').replace(/["']/g, '').trim();
    const src = prop('src');
    if (!family || !src) continue;
    const urls = [];
    const ure = /url\(\s*['"]?([^'")]+)['"]?\s*\)/gi;
    let um;
    while ((um = ure.exec(src))) urls.push(um[1]);
    if (!urls.length) continue;
    out.push({
      family,
      weight: prop('font-weight') || '400',
      style: prop('font-style') || 'normal',
      urls,
    });
  }
  return out;
}

/**
 * Collect @font-face from same-origin cssRules plus fetched cross-origin stylesheets.
 * Tabler's CDN CSS is cross-origin (cssRules throws); we must HTTP-fetch the text.
 */
export async function collectFontFaceRules(page) {
  const sameOrigin = await page.evaluate(() => {
    const out = [];
    const push = (family, weight, style, src) => {
      if (!family || !src) return;
      const urls = [];
      const re = /url\(\s*['"]?([^'")]+)['"]?\s*\)/gi;
      let m;
      while ((m = re.exec(src))) urls.push(m[1]);
      if (!urls.length) return;
      out.push({
        family: family.replace(/["']/g, '').trim(),
        weight: weight || '400',
        style: style || 'normal',
        urls,
      });
    };
    for (const sheet of document.styleSheets) {
      let rules;
      try { rules = sheet.cssRules; } catch { continue; }
      if (!rules) continue;
      for (const rule of rules) {
        if (!(rule instanceof CSSFontFaceRule)) continue;
        push(
          rule.style.getPropertyValue('font-family'),
          rule.style.getPropertyValue('font-weight'),
          rule.style.getPropertyValue('font-style'),
          rule.style.getPropertyValue('src'),
        );
      }
    }
    return out;
  });

  const hrefs = await page.evaluate(() =>
    [...document.querySelectorAll('link[rel="stylesheet"]')]
      .map((l) => l.href)
      .filter(Boolean));

  const fromFetch = [];
  for (const href of hrefs) {
    try {
      let css;
      if (href.startsWith('file:')) {
        css = readFileSync(fileURLToPath(href), 'utf8');
      } else {
        const res = await fetch(href);
        if (!res.ok) continue;
        css = await res.text();
      }
      const faces = parseFontFacesFromCss(css);
      // Resolve relative font urls against the stylesheet URL
      for (const f of faces) {
        f.urls = f.urls.map((u) => {
          try { return new URL(u, href).href; } catch { return u; }
        });
        fromFetch.push(f);
      }
    } catch (e) {
      console.warn(`  ! stylesheet fetch failed: ${href} (${e.message})`);
    }
  }

  return [...sameOrigin, ...fromFetch];
}

function weightRangeCovers(faceWeight, target) {
  const s = String(faceWeight || '400').trim();
  const parts = s.split(/\s+/).map((p) => parseInt(p, 10)).filter((n) => Number.isFinite(n));
  if (parts.length >= 2) return target >= Math.min(...parts) && target <= Math.max(...parts);
  if (parts.length === 1) return parts[0] === target || Math.abs(parts[0] - target) <= 50;
  const named = parseFontWeight(s);
  return Math.abs(named - target) <= 50;
}

function styleMatches(faceStyle, target) {
  const a = parseFontStyle(faceStyle);
  const b = parseFontStyle(target);
  // Variable italic axes often declare style: normal with ital axis — allow normal→italic.
  if (a === b) return true;
  if (b === 'italic' && a === 'normal') return true;
  return false;
}

function familiesMatch(faceFamily, usedFamily) {
  const a = normalizeFamily(faceFamily);
  const b = normalizeFamily(usedFamily);
  if (a === b) return true;
  // "inter" vs "inter var" already normalized; also allow prefix (Noto Sans → Noto Sans JP).
  return a.startsWith(b) || b.startsWith(a);
}

function collectUsedFaces(tree) {
  const used = new Map(); // key -> { family, weight, style }
  (function walk(node) {
    if (node.kind === 'text' || (node.text && node.style?.fontFamily)) {
      // extract.mjs sets kind via classify later; text nodes have non-empty text + fontSize
      const hasText = typeof node.text === 'string' && node.text.length > 0;
      if (hasText && node.style) {
        const family = firstFontFamily(node.style.fontFamily);
        if (family) {
          const weight = parseFontWeight(node.style.fontWeight);
          const style = parseFontStyle(node.style.fontStyle);
          const key = fontFaceKey(family, weight, style);
          if (!used.has(key)) used.set(key, { family, weight, style });
        }
      }
    }
    for (const c of node.children || []) walk(c);
  })(tree);
  return used;
}

function resolveUrl(pageUrl, maybeRelative) {
  try {
    return new URL(maybeRelative, pageUrl).href;
  } catch {
    return maybeRelative;
  }
}

function pickCaptured(captured, absUrl) {
  if (captured.has(absUrl)) return captured.get(absUrl);
  // Strip hash/query variants
  for (const [u, v] of captured) {
    if (u.split('?')[0] === absUrl.split('?')[0]) return v;
  }
  return null;
}

function extFor(buffer, contentType, url) {
  if (/woff2/i.test(contentType) || /\.woff2/i.test(url)) return 'woff2';
  if (/woff/i.test(contentType) || /\.woff($|\?)/i.test(url)) return 'woff';
  if (/ttf|truetype/i.test(contentType) || /\.ttf/i.test(url)) return 'ttf';
  if (/otf|opentype/i.test(contentType) || /\.otf/i.test(url)) return 'otf';
  // sniff
  if (buffer[0] === 0x77 && buffer[1] === 0x4f && buffer[2] === 0x46) return buffer[3] === 0x32 ? 'woff2' : 'woff';
  if (buffer[0] === 0x00 && buffer[1] === 0x01 && buffer[2] === 0x00 && buffer[3] === 0x00) return 'ttf';
  if (buffer.toString('ascii', 0, 4) === 'OTTO') return 'otf';
  return 'bin';
}

function safeStem(family, weight, style) {
  const base = normalizeFamily(family).replace(/[^a-z0-9]+/g, '_').replace(/^_|_$/g, '') || 'font';
  const ital = style === 'italic' ? 'i1' : 'i0';
  return `${base}_w${weight}_${ital}`;
}

function instantiateToTtf(inputPath, outputPath, weight, style) {
  const args = [join(__dirname, 'instantiate_font.py'), inputPath, outputPath, '--wght', String(weight)];
  if (style === 'italic') args.push('--ital', '1');
  const r = spawnSync('python', args, { encoding: 'utf8' });
  if (r.stdout) process.stdout.write(r.stdout);
  if (r.stderr) process.stderr.write(r.stderr);
  if (r.status !== 0 || !existsSync(outputPath)) {
    throw new Error(`instantiate_font failed for ${inputPath} → ${outputPath}`);
  }
}

/**
 * @returns {Map<string, string>} fontFaceKey → relative path "Fonts/….ttf"
 */
export async function materializeWebFonts({ tree, captured, rules, fontsDir, pageUrl }) {
  mkdirSync(fontsDir, { recursive: true });
  const fontMap = new Map();
  const used = collectUsedFaces(tree);
  if (used.size === 0) return fontMap;

  let i = 0;
  for (const [key, need] of used) {
    const candidates = rules.filter((r) =>
      familiesMatch(r.family, need.family)
      && weightRangeCovers(r.weight, need.weight)
      && styleMatches(r.style, need.style));
    // Prefer exact family name length (shorter = closer), then first url that we captured.
    candidates.sort((a, b) => normalizeFamily(a.family).length - normalizeFamily(b.family).length);

    let hit = null;
    let hitUrl = null;
    for (const face of candidates) {
      for (const u of face.urls) {
        const abs = resolveUrl(pageUrl, u);
        let cap = pickCaptured(captured, abs);
        if (!cap) {
          // Network listener often misses CDN fonts (cache / late load) — fetch directly.
          try {
            const res = await fetch(abs);
            if (res.ok) {
              const buffer = Buffer.from(await res.arrayBuffer());
              const contentType = (res.headers.get('content-type') || '').split(';')[0].trim();
              cap = { buffer, contentType };
              captured.set(abs, cap);
            }
          } catch { /* ignore */ }
        }
        if (cap) { hit = cap; hitUrl = abs; break; }
      }
      if (hit) break;
    }

    // Fallback: any captured font whose URL mentions the family token
    if (!hit) {
      const token = normalizeFamily(need.family).split(' ')[0];
      for (const [u, cap] of captured) {
        if (token && u.toLowerCase().includes(token)) { hit = cap; hitUrl = u; break; }
      }
    }

    if (!hit) {
      console.warn(`  ! web font not captured for ${key} (will use system/family name)`);
      continue;
    }

    const ext = extFor(hit.buffer, hit.contentType, hitUrl);
    const stem = safeStem(need.family, need.weight, need.style);
    const rawPath = join(fontsDir, `_raw${i}.${ext}`);
    const outName = `${stem}.ttf`;
    const outPath = join(fontsDir, outName);
    writeFileSync(rawPath, hit.buffer);
    try {
      instantiateToTtf(rawPath, outPath, need.weight, need.style);
    } catch (e) {
      console.warn(`  ! ${e.message}`);
      continue;
    } finally {
      try { rmSync(rawPath, { force: true }); } catch { /* ignore */ }
    }
    const rel = `Fonts/${outName}`;
    fontMap.set(key, rel);
    // Also alias without italic if we baked italic into the file under italic key only
    if (need.style === 'italic') {
      fontMap.set(fontFaceKey(need.family, need.weight, 'normal'), rel);
    }
    console.log(`  font: ${need.family} wght=${need.weight} ${need.style} → ${rel}`);
    i++;
  }
  return fontMap;
}

/** Run `dotnet … gumcli <args>` via the pinned wrapper (fonts/check/…). */
export function runGumcliFonts(gumxPath: string, _gumCliCsprojIgnored?: string) {
  const wrapper = join(__dirname, 'gumcli.ts');
  console.log(`> npx tsx gumcli.ts fonts ${gumxPath}`);
  const r = spawnSync(process.execPath, nodeTsxArgs(wrapper, 'fonts', gumxPath), { encoding: 'utf8', shell: false });
  if (r.stdout) process.stdout.write(r.stdout);
  if (r.stderr) process.stderr.write(r.stderr);
  if (r.status !== 0) throw new Error(`gumcli fonts exited ${r.status}`);
}
