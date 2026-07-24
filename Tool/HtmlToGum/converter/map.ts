// @ts-nocheck
// Maps a Chromium box tree (from extract.ts) to a Gum screen in the compact .gusx
// format. This is Tier 1 + the flex/grid slice of Tier 2 from DESIGN.md: flex row/column
// stacks, uniform CSS Grid → AutoGrid, gap, flex-grow (Ratio), stretch cross-sizing,
// backgrounds, border-radius, and auto-sized text. Non-flex/non-grid falls back to
// absolute positioning; non-uniform grids and cell spans also fall back (warned).

import type {
  BoxNode, MappedScreen, NineSliceInfo, ResponsiveMap,
} from './types.js';

// ---- Gum enum values (see ai-reference/gum-xml-format.md) --------------------
const CL = {
  Regular: 0,
  TopToBottomStack: 1,
  LeftToRightStack: 2,
  AutoGridHorizontal: 3,
  AutoGridVertical: 4,
};
const DIM = { Absolute: 0, PercentageOfParent: 1, RelativeToParent: 2, RelativeToChildren: 4, Ratio: 7 };

// ---- helpers ----------------------------------------------------------------
// OKLab -> linear sRGB matrices (Björn Ottosson, https://bottosson.github.io/posts/oklab/).
function oklabToSrgb(L, a, b) {
  const l_ = L + 0.3963377774 * a + 0.2158037573 * b;
  const m_ = L - 0.1055613458 * a - 0.0638541728 * b;
  const s_ = L - 0.0894841775 * a - 1.2914855480 * b;
  const l = l_ ** 3;
  const m = m_ ** 3;
  const s = s_ ** 3;
  const rLin = +4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s;
  const gLin = -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s;
  const bLin = -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s;
  // Negative (out-of-gamut) channels always take the c <= threshold branch here, so they
  // stay negative through gamma() and get clamped to 0 below — never reach c ** (1/2.4).
  const gamma = (c) => (c <= 0.0031308 ? 12.92 * c : 1.055 * c ** (1 / 2.4) - 0.055);
  const clamp255 = (c) => Math.round(Math.min(255, Math.max(0, gamma(c) * 255)));
  return { r: clamp255(rLin), g: clamp255(gLin), b: clamp255(bLin) };
}

// "oklch(L C H)" / "oklch(L C H / A)" — CSS Color 4 syntax. L is 0..1 (or a percentage of
// it), C is chroma, H is hue in degrees. Chromium serializes getComputedStyle() this way
// for colors defined via oklch() (common in modern design systems, e.g. Tailwind v4's
// default palette) even when no color-mix()/relative-color is involved.
function parseOklch(str) {
  const m = str.match(
    /oklch\(\s*([\d.]+)(%)?\s+([\d.]+)(%)?\s+([\d.]+)(?:\s*\/\s*([\d.]+)(%)?)?\s*\)/,
  );
  if (!m) return null;
  const [, lRaw, lPct, cRaw, cPct, hRaw, aRaw, aPct] = m;
  const L = lPct ? parseFloat(lRaw) / 100 : parseFloat(lRaw);
  // Chroma percentage reference range per CSS Color 4: 100% = 0.4.
  const C = cPct ? (parseFloat(cRaw) / 100) * 0.4 : parseFloat(cRaw);
  const H = (parseFloat(hRaw) * Math.PI) / 180;
  const a = C * Math.cos(H);
  const b = C * Math.sin(H);
  const { r, g, b: bl } = oklabToSrgb(L, a, b);
  const alpha = aRaw === undefined ? 1 : (aPct ? parseFloat(aRaw) / 100 : parseFloat(aRaw));
  return { r, g, b: bl, a: Math.round(alpha * 255) };
}

export function parseColor(str) {
  if (!str) return null;
  // Legacy syntax: "rgb(r, g, b)" / "rgba(r, g, b, a)" — components 0..255.
  let m = str.match(/rgba?\(([^)]+)\)/);
  if (m) {
    const p = m[1].split(',').map((s) => parseFloat(s.trim()));
    const [r, g, b, a = 1] = p;
    return { r: Math.round(r), g: Math.round(g), b: Math.round(b), a: Math.round(a * 255) };
  }
  // CSS Color 4 syntax: "color(srgb r g b)" / "color(srgb r g b / a)" — components 0..1
  // floats. Chromium serializes getComputedStyle() this way for colors that resolve
  // through color-mix()/relative-color custom properties (common in modern design
  // systems, e.g. Tabler's --tblr-danger) even when the value is plain sRGB.
  m = str.match(/color\(srgb\s+([\d.]+)\s+([\d.]+)\s+([\d.]+)(?:\s*\/\s*([\d.]+))?\)/);
  if (m) {
    const [, r, g, b, a] = m;
    return {
      r: Math.round(parseFloat(r) * 255),
      g: Math.round(parseFloat(g) * 255),
      b: Math.round(parseFloat(b) * 255),
      a: Math.round((a !== undefined ? parseFloat(a) : 1) * 255),
    };
  }
  if (str.startsWith('oklch(')) return parseOklch(str);
  return null;
}
const isTransparent = (c) => !c || c.a === 0;
const isBold = (w) => w === 'bold' || w === 'bolder' || parseInt(w, 10) >= 600;
const isItalic = (s) => s === 'italic' || s === 'oblique';
const firstFont = (fam) => (fam || '').split(',')[0].replace(/["']/g, '').trim();

// Tags whose only visual effect (once background/border/shadow/padding is ruled out) is a
// font weight/style/color change — safe to fold into one Text via BBCode markup instead of
// a sibling Text per run. #text nodes carry no style deviation of their own.
const INLINE_PHRASING_TAGS = new Set(['#text', 'a', 'strong', 'b', 'em', 'i']);

// Chromium serializes resolved grid tracks as "200px 200px 200px" (fr already computed).
// Returns pixel lengths; empty for none/auto/unsupported keywords.
function parseGridTracks(str) {
  if (!str || str === 'none' || str === 'masonry') return [];
  return str.trim().split(/\s+/).map((t) => parseFloat(t)).filter((n) => Number.isFinite(n) && n >= 0);
}

function tracksAreUniform(tracks, tolPx = 2) {
  if (tracks.length < 1) return false;
  const first = tracks[0];
  return tracks.every((t) => Math.abs(t - first) <= tolPx);
}

// CSS grid-column/row end - start; "auto" / span keywords. Returns 1 when unparseable.
function gridSpan(start, end) {
  if (!start || !end || start === 'auto' || end === 'auto') return 1;
  const s = parseInt(start, 10);
  const e = parseInt(end, 10);
  if (Number.isFinite(s) && Number.isFinite(e) && e > s) return e - s;
  const span = String(end).match(/^span\s+(\d+)/i) || String(start).match(/^span\s+(\d+)/i);
  if (span) return parseInt(span[1], 10);
  return 1;
}

function childrenHaveGridSpans(node) {
  return node.children.some((c) => {
    const s = c.style;
    return gridSpan(s.gridColumnStart, s.gridColumnEnd) > 1
      || gridSpan(s.gridRowStart, s.gridRowEnd) > 1;
  });
}

/** Authored (non-auto) grid placement — AutoGrid only fills cells in DOM order. */
function childrenHaveExplicitGridPlacement(node) {
  return node.children.some((c) => {
    const s = c.style;
    if (gridSpan(s.gridColumnStart, s.gridColumnEnd) > 1) return true;
    if (gridSpan(s.gridRowStart, s.gridRowEnd) > 1) return true;
    const specs = [
      s.gridAreaSpecified, s.gridColumnSpecified, s.gridRowSpecified,
      s.gridColumnStartSpecified, s.gridColumnEndSpecified,
      s.gridRowStartSpecified, s.gridRowEndSpecified,
    ];
    return specs.some((v) => {
      const t = String(v || '').trim().toLowerCase();
      return t !== '' && t !== 'auto';
    });
  });
}

/**
 * Classify a container's children layout for walk().
 * @returns {'row'|'column'|'grid-h'|'grid-v'|'block'}
 */
function layoutModeOf(node) {
  const s = node.style;
  if (s.display === 'flex') {
    const wrap = (s.flexWrap || 'nowrap').toLowerCase();
    // Gum stacks are single-line — multi-line flex has no ChildrenLayout equivalent.
    // Fall back to Absolute from Chromium boxes (same as mixed grid).
    if (wrap === 'wrap' || wrap === 'wrap-reverse') return 'block';
    return s.flexDirection === 'row' || s.flexDirection === 'row-reverse' ? 'row' : 'column';
  }
  if (s.display === 'grid' || s.display === 'inline-grid') {
    const cols = parseGridTracks(s.gridTemplateColumns);
    const rows = parseGridTracks(s.gridTemplateRows);
    // Uniform equal tracks only — AutoGrid has one cell size. Mixed track sizes
    // (1fr 2fr) and cell spans have no Gum equivalent → caller falls back to absolute.
    if (cols.length === 0) return 'block';
    if (!tracksAreUniform(cols)) return 'block';
    if (rows.length > 0 && !tracksAreUniform(rows)) return 'block';
    if (childrenHaveExplicitGridPlacement(node)) return 'block';
    // AutoGrid fills cells in instance order; CSS order has no Gum equivalent here.
    if (node.children.some((c) => (c.style.order || 0) !== 0)) return 'block';
    const flow = (s.gridAutoFlow || 'row').split(' ')[0];
    return flow === 'column' ? 'grid-v' : 'grid-h';
  }
  return 'block';
}

/** True when flex-direction is *-reverse (main-start flipped). */
function isFlexReversed(style) {
  const fd = (style?.flexDirection || 'row').toLowerCase();
  return fd === 'row-reverse' || fd === 'column-reverse';
}

/**
 * row/column-reverse flips main-start/end. Invert start↔end justifies so a normal
 * LTR/TTB Gum stack + reversed child order matches Chromium.
 */
function invertMainStartEnd(justify) {
  const j = (justify || 'normal').toLowerCase();
  if (j === 'normal' || j === 'flex-start' || j === 'start' || j === 'left' || j === 'stretch') {
    return 'flex-end';
  }
  if (j === 'flex-end' || j === 'end' || j === 'right' || j === 'bottom') {
    return 'flex-start';
  }
  return justify;
}

function isStackLayout(mode) {
  return mode === 'row' || mode === 'column';
}
function isGridLayout(mode) {
  return mode === 'grid-h' || mode === 'grid-v';
}
function isManagedLayout(mode) {
  return isStackLayout(mode) || isGridLayout(mode);
}

function paddingOf(style) {
  return {
    top: style.paddingTop || 0,
    right: style.paddingRight || 0,
    bottom: style.paddingBottom || 0,
    left: style.paddingLeft || 0,
  };
}
function hasPadding(pad) {
  return pad.top > 0 || pad.right > 0 || pad.bottom > 0 || pad.left > 0;
}
function contentBoxRect(borderRect, pad) {
  return {
    x: borderRect.x + pad.left,
    y: borderRect.y + pad.top,
    width: Math.max(0, borderRect.width - pad.left - pad.right),
    height: Math.max(0, borderRect.height - pad.top - pad.bottom),
  };
}

/** CSS padding box = border-box inset by border widths (Gum NineSlice is paint-only). */
function borderPadOf(style) {
  return {
    top: style.borderTopWidth || 0,
    right: style.borderRightWidth || 0,
    bottom: style.borderBottomWidth || 0,
    left: style.borderLeftWidth || 0,
  };
}

/** Inset for the Content wrapper: padding, plus border when a frame/border occupies space. */
function contentInset(style, pad, { hasFrame = false } = {}) {
  if (!hasFrame) return pad;
  const b = borderPadOf(style);
  return {
    top: pad.top + b.top,
    right: pad.right + b.right,
    bottom: pad.bottom + b.bottom,
    left: pad.left + b.left,
  };
}

/** justify-content values that LeftToRight/TopToBottom stacks handle without spacers. */
function isJustifyStart(justify) {
  const j = (justify || 'normal').toLowerCase();
  return j === 'normal' || j === 'flex-start' || j === 'start' || j === 'left' || j === 'stretch';
}

/** margin:auto (and similar push margins) resolve to used px — Gum stacks ignore them. */
function childrenHavePushMargins(node, orientation) {
  if (!node?.children?.length) return false;
  const row = orientation === 'row' || orientation === 'row-reverse';
  return node.children.some((c) => {
    const s = c.style || {};
    if (row) return (s.marginLeft || 0) > 1 || (s.marginRight || 0) > 1;
    return (s.marginTop || 0) > 1 || (s.marginBottom || 0) > 1;
  });
}

/** justify-content modes we approximate with Ratio spacer Containers in the stack. */
function justifySpacerMode(justify) {
  const j = (justify || 'normal').toLowerCase();
  if (j === 'center') return 'center';
  if (j === 'flex-end' || j === 'end' || j === 'right' || j === 'bottom') return 'end';
  if (j === 'space-between') return 'between';
  // CSS space-around: end gutters are half the between gutters → Ratio 1 / 2 / 1.
  if (j === 'space-around') return 'around';
  // CSS space-evenly: all gutters equal → Ratio 1 throughout.
  if (j === 'space-evenly') return 'evenly';
  return null;
}

function isAlignStretch(align) {
  const a = (align || 'stretch').toLowerCase();
  return a === 'stretch' || a === 'normal';
}

/** Inline style width/height ending in % — cheap CSSOM hint (not full cascade). */
function specifiedPercent(style, axisName) {
  if (!style) return null;
  const raw = axisName === 'Width' ? style.widthSpecified : style.heightSpecified;
  const m = String(raw || '').trim().match(/^([\d.]+)\s*%$/);
  return m ? parseFloat(m[1]) : null;
}

/** Paint order: ascending z-index, stable by DOM index. Path keys still use DOM index. */
function childrenInPaintOrder(node) {
  return node.children
    .map((c, domIndex) => ({ c, domIndex, z: c.style.zIndex || 0 }))
    .sort((a, b) => (a.z - b.z) || (a.domIndex - b.domIndex));
}

// Computed box-shadow: "<color> <offsetX>px <offsetY>px <blur>px <spread>px[, ...]"
// (Chromium always serializes color first, confirmed against Tabler's card:
// "rgba(31, 41, 55, 0.04) 0px 0px 4px 0px"). Gum's Rectangle has native HasDropshadow/
// DropshadowOffsetX/Y/Blur/Red/Green/Blue/Alpha — no sprite rasterization needed for the
// common single-shadow case (contra the original DESIGN.md §5.3 assumption). Multiple
// shadows only keep the first (Gum has one shadow slot); `inset` shadows are skipped —
// Gum's dropshadow is an outer shadow, an inset would render backwards.
export function parseBoxShadow(str) {
  if (!str || str === 'none') return null;
  const first = str.split(/,(?![^(]*\))/)[0].trim(); // split on top-level commas only
  if (/inset/.test(first)) return null;
  const colorMatch = first.match(/^(rgba?\([^)]+\)|color\([^)]+\)|oklch\([^)]+\))/);
  if (!colorMatch) return null;
  const color = parseColor(colorMatch[1]);
  if (!color) return null;
  const lengths = first.slice(colorMatch[0].length).trim()
    .split(/\s+/).map((s) => parseFloat(s)).filter((n) => !isNaN(n));
  const [offsetX = 0, offsetY = 0, blur = 0] = lengths; // spread (4th value) has no Gum equivalent
  return { offsetX, offsetY, blur, color };
}

/**
 * Hard (blur≈0) text-shadow layers — used as CSS faux outlines (RPGUI's 4-way black
 * stamps). Soft shadows are skipped (no Text drop-shadow slot on FontCache path).
 * @returns {{ offsetX: number, offsetY: number, color: {r:number,g:number,b:number,a:number} }[]}
 */
export function parseHardTextShadows(style) {
  if (!style) return [];
  const raw = style.textShadow || '';
  if (!raw || raw === 'none') return [];

  const layers = raw.split(/,(?![^(]*\))/).map((s) => s.trim()).filter(Boolean);
  const out = [];
  for (const layer of layers) {
    if (/inset/i.test(layer)) return []; // mixed inset → bail
    let color = { r: 0, g: 0, b: 0, a: 255 };
    const colorMatch = layer.match(/rgba?\([^)]+\)|color\([^)]+\)|oklch\([^)]+\)|#[0-9a-fA-F]{3,8}/i);
    if (colorMatch) {
      const c = parseColor(colorMatch[0]);
      if (c) color = c;
    } else if (/\bblack\b/i.test(layer)) {
      color = { r: 0, g: 0, b: 0, a: 255 };
    }
    let rest = layer;
    if (colorMatch) rest = layer.replace(colorMatch[0], ' ');
    rest = rest.replace(/\b(?:black|white|red|blue|green|navy|gray|grey|transparent)\b/gi, ' ');
    const lengths = rest.split(/\s+/).map((s) => parseFloat(s)).filter((n) => !isNaN(n));
    if (lengths.length < 2) return [];
    const [offsetX = 0, offsetY = 0, blur = 0] = lengths;
    if (blur > 0.5) return []; // soft → not stampable as hard outline
    if (offsetX === 0 && offsetY === 0) continue;
    out.push({
      offsetX: Math.round(offsetX),
      offsetY: Math.round(offsetY),
      color,
    });
  }
  // Cap runaway multi-shadow stacks.
  out.splice(8);
  // RPGUI-style cardinal rings authored at ±2px read as a 1px outline on pixel fonts
  // (Chromium). Shrink pure axis-aligned hard rings so stamps match the visible ring.
  if (out.length >= 3 && out.every((p) => p.offsetX === 0 || p.offsetY === 0)) {
    const maxMag = Math.max(...out.map((p) => Math.max(Math.abs(p.offsetX), Math.abs(p.offsetY))));
    if (maxMag >= 2) {
      for (const p of out) {
        if (p.offsetX !== 0) p.offsetX = Math.sign(p.offsetX);
        if (p.offsetY !== 0) p.offsetY = Math.sign(p.offsetY);
      }
    }
  }
  return out;
}

/**
 * -webkit-text-stroke-width → Gum OutlineThickness (FontCache _oN). Prefer hard
 * text-shadow stamps when both exist — stroke alone uses the outline atlas path.
 */
export function parseTextOutlineThickness(style) {
  if (!style) return 0;
  if (parseHardTextShadows(style).length > 0) return 0;
  const stroke = Number(style.webkitTextStrokeWidth) || 0;
  if (stroke > 0) return Math.max(1, Math.min(8, Math.round(stroke)));
  return 0;
}

// CSS object-fit:cover crops the source image to fill the box without distorting its
// aspect ratio (scale up until both dimensions cover, center-crop the overflow). Gum's
// Sprite has no object-fit concept — TextureAddress defaults to EntireTexture, which
// just stretches the whole source into the box (CSS's *default* object-fit: fill —
// matches Gum's default fine). Only diverges when object-fit: cover is explicitly set,
// which needs an explicit TextureAddress=Custom source-rect crop to reproduce. Assumes
// object-position: center (the CSS default) — a real, undone simplification for
// off-center object-position.
function computeCoverCrop(srcW, srcH, dstW, dstH) {
  const scale = Math.max(dstW / srcW, dstH / srcH);
  const cropW = dstW / scale;
  const cropH = dstH / scale;
  return {
    left: Math.round((srcW - cropW) / 2),
    top: Math.round((srcH - cropH) / 2),
    width: Math.round(cropW),
    height: Math.round(cropH),
  };
}

// List bullets are pseudo-content (::marker) — never part of textContent, so they were
// silently absent from every emitted <li>. Gum has no marker/list concept, so this is a
// text-prefix approximation covering the common unordered-list styles; `decimal` and
// other counter-based types are left alone (a running counter is real work with no
// evidence yet it's needed) rather than emit a wrong or literal marker string.
//
// The real bullet glyphs (U+2022 •, U+25E6 ◦, U+25AA ▪) are outside Gum's default font
// range (BmfcSave.DefaultRanges = "32-126,160-255", ASCII + Latin-1 only — see §4.3) —
// confirmed empirically: emitting them produced correct XML that silently rendered as
// nothing, because KernSmith's generated atlas has no glyph for them. Substituted with
// Latin-1-safe characters instead of also threading BmfcSave.AddCharacters through the
// host — keeps the fix entirely inside the converter.
const LIST_MARKERS = { disc: '· ', circle: 'o ', square: '- ' }; // · (U+00B7, Latin-1) / o / -
function listMarkerPrefix(style) {
  if (style.display !== 'list-item') return '';
  return LIST_MARKERS[style.listStyleType] || '';
}

/** Map CSS text-align / flex justify|align into Gum Text Horizontal/VerticalAlignment. */
function textAlignHorizontal(style, { preferCenterHorizontal = false } = {}) {
  const ta = (style.textAlign || '').toLowerCase();
  if (ta === 'center') return 1;
  if (ta === 'right' || ta === 'end') return 2;
  if (ta === 'left' || ta === 'start') return 0;
  const jc = (style.justifyContent || '').toLowerCase();
  if (jc === 'center') return 1;
  if (jc === 'flex-end' || jc === 'end' || jc === 'right') return 2;
  if (preferCenterHorizontal) return 1;
  return null; // leave Gum default (left)
}

function textAlignVertical(style, { preferCenterVertical = false } = {}) {
  const ai = (style.alignItems || '').toLowerCase();
  if (ai === 'center') return 1;
  if (ai === 'flex-end' || ai === 'end' || ai === 'bottom') return 2;
  if (preferCenterVertical) return 1;
  return null;
}

function classify(node) {
  if (node.tag === 'img') return 'image';
  // Filter that must bake descendants into one sprite → opaque image leaf.
  if ((node.rasterSrc || node.style.needsRaster) && node.style.rasterWholeSubtree) {
    return 'image';
  }
  if (node.children.length > 0) return 'container';
  // Text leaf with chrome raster (border-image / gradient backdrop) stays 'text' so
  // textChrome emits Sprite + Text — not 'image', which would drop the label (#gold).
  if (node.text) return 'text';
  // Gradient/filter/border-image leaf with no text → Sprite (baked or url).
  if (node.rasterSrc || node.style.needsRaster) return 'image';
  // An empty div styled entirely via background-image (Tabler's own pattern for
  // aspect-ratio photo boxes: <div class="card-img-top" style="background-image:...">,
  // no <img> tag at all) is a leaf, same shape as a plain color rect — except its visual
  // content is an image, so it needs a Sprite, not a Rectangle. Caught here rather than
  // by the classify()-first, later-emit-nothing failure mode: a leaf 'rect' node never
  // reaches the container-only backdrop logic (see hasVisualStyling's other caller).
  if (parseBackgroundImageUrl(node.style.backgroundImage)) {
    return 'image';
  }
  return 'rect';
}
const BASE = { container: 'Container', text: 'Text', rect: 'Rectangle', image: 'Sprite' };

// Computed background-image: 'url("...")' / 'url(...)' / 'none'. No srcset/multi-layer
// support — first url() only, matching the single-shadow/single-side-border
// simplifications elsewhere in this mapper.
export function parseBackgroundImageUrl(str) {
  if (!str || str === 'none') return null;
  const m = str.match(/url\((['"]?)(.*?)\1\)/);
  return m ? m[2] : null;
}

// ---- name allocator (valid, unique Gum instance names) ----------------------
function makeNamer() {
  const used = new Set();
  let counter = 0;
  function claim(base) {
    let name = base;
    while (used.has(name)) name = base + ++counter;
    used.add(name);
    return name;
  }
  return {
    forNode(node) {
      let base = (node.id || node.tag || 'node').replace(/[^A-Za-z0-9_]/g, '_');
      if (!/^[A-Za-z_]/.test(base)) base = '_' + base;
      // Capitalize first letter for readability (Gum convention).
      base = base.charAt(0).toUpperCase() + base.slice(1);
      return claim(base);
    },
    // Mint a synthetic name (e.g. a backing Rectangle) sharing the same collision set.
    mint(base) {
      return claim(base);
    },
  };
}

// ---- variable emission ------------------------------------------------------
function v(type, name, valueType, value) {
  return { type, name, valueType, value };
}
const VF = (n, val) => v('float', n, 'xsd:float', val);
const VI = (n, val) => v('int', n, 'xsd:int', val);
const VB = (n, val) => v('bool', n, 'xsd:boolean', val ? 'true' : 'false');
const VS = (n, val) => v('string', n, 'xsd:string', val);
const VDIM = (n, val) => v('DimensionUnitType', n, 'xsd:int', val);
// PositionUnitType: X uses even-ish Left/Right; Y uses Top/Bottom (see UnitConverter.cs).
const POS = {
  PixelsFromLeft: 0, PixelsFromTop: 1, PixelsFromRight: 4, PixelsFromBottom: 5,
  PixelsFromCenterX: 6, PixelsFromCenterY: 7,
};
const ORIGIN_H = { Left: 0, Center: 1, Right: 2 };
const ORIGIN_V = { Top: 0, Center: 1, Bottom: 2 };
const VPOS = (n, val) => v('PositionUnitType', n, 'xsd:int', val);
const VOH = (n, val) => v('HorizontalAlignment', n, 'xsd:int', val);
const VOV = (n, val) => v('VerticalAlignment', n, 'xsd:int', val);

function borderSides(style) {
  return [
    { side: 'Top', width: style.borderTopWidth || 0, color: parseColor(style.borderTopColor) },
    { side: 'Right', width: style.borderRightWidth || 0, color: parseColor(style.borderRightColor) },
    { side: 'Bottom', width: style.borderBottomWidth || 0, color: parseColor(style.borderBottomColor) },
    { side: 'Left', width: style.borderLeftWidth || 0, color: parseColor(style.borderLeftColor) },
  ];
}

function bordersAreUniform(sides) {
  // Uniform StrokeWidth only when all four sides match. One/two/three drawn sides
  // (Bootstrap table row rules = border-bottom only) must use emitBorderEdges —
  // otherwise we wrongly read Top (often 0) and paint nothing.
  const drawn = sides.filter((s) => s.width > 0 && !isTransparent(s.color));
  if (drawn.length === 0) return true;
  if (drawn.length !== 4) return false;
  const w0 = drawn[0].width;
  const c0 = drawn[0].color;
  return drawn.every((s) => s.width === w0
    && s.color.r === c0.r && s.color.g === c0.g && s.color.b === c0.b && s.color.a === c0.a);
}

function anyBorder(style) {
  return borderSides(style).some((s) => s.width > 0 && !isTransparent(s.color));
}

// Background/border/corner-radius/shadow variables for a Rectangle instance. Shared by
// real 'rect' DOM nodes and by the synthetic backdrop Rectangle a styled Container gets.
// When borders are asymmetric, StrokeWidth stays 0 and `asymmetricBorders` lists the
// edges for emitBorderEdges() (four thin child Rectangles — figma-html technique, §8.7/§8.12).
function buildRectFillVars(name, style) {
  const out = [];
  const warnings = [];
  const bg = parseColor(style.backgroundColor);
  const opacity = (style.opacity != null && style.opacity < 1) ? style.opacity : 1;
  if (!isTransparent(bg)) {
    const fa = Math.round(bg.a * opacity);
    out.push(VI(`${name}.FillRed`, bg.r), VI(`${name}.FillGreen`, bg.g), VI(`${name}.FillBlue`, bg.b));
    if (fa !== 255) out.push(VI(`${name}.FillAlpha`, fa));
    out.push(VB(`${name}.IsFilled`, true)); // Rectangle defaults IsFilled=false
  }
  const sides = borderSides(style);
  const uniform = bordersAreUniform(sides);
  let asymmetricBorders = null;
  if (uniform) {
    const top = sides[0];
    if (top.width > 0 && !isTransparent(top.color)) {
      const sa = Math.round(top.color.a * opacity);
      out.push(
        VF(`${name}.StrokeWidth`, top.width),
        VI(`${name}.StrokeRed`, top.color.r), VI(`${name}.StrokeGreen`, top.color.g), VI(`${name}.StrokeBlue`, top.color.b),
      );
      if (sa !== 255) out.push(VI(`${name}.StrokeAlpha`, sa));
    } else {
      out.push(VF(`${name}.StrokeWidth`, 0));
    }
  } else {
    out.push(VF(`${name}.StrokeWidth`, 0));
    asymmetricBorders = sides.filter((s) => s.width > 0 && !isTransparent(s.color));
  }
  if (style.borderTopLeftRadius > 0) out.push(VF(`${name}.CornerRadius`, style.borderTopLeftRadius));

  const shadow = parseBoxShadow(style.boxShadow);
  if (shadow) {
    out.push(
      VB(`${name}.HasDropshadow`, true),
      VF(`${name}.DropshadowOffsetX`, shadow.offsetX),
      VF(`${name}.DropshadowOffsetY`, shadow.offsetY),
      VF(`${name}.DropshadowBlur`, shadow.blur),
      VI(`${name}.DropshadowRed`, shadow.color.r), VI(`${name}.DropshadowGreen`, shadow.color.g), VI(`${name}.DropshadowBlue`, shadow.color.b),
    );
    if (shadow.color.a !== 255) out.push(VI(`${name}.DropshadowAlpha`, shadow.color.a));
  }
  return { vars: out, warnings, asymmetricBorders };
}

// Does this node need a backing Rectangle/Sprite? (Container itself has no fill/border/
// radius/shadow/image properties in Gum — see §8.5/§8.6 of DESIGN.md.)
function hasVisualStyling(style) {
  const bg = parseColor(style.backgroundColor);
  return !isTransparent(bg) || anyBorder(style) || style.borderTopLeftRadius > 0
    || !!parseBoxShadow(style.boxShadow) || !!parseBackgroundImageUrl(style.backgroundImage)
    || !!style.needsRaster;
}

function imageNeedsUnderlay(style) {
  const bg = parseColor(style.backgroundColor);
  return !isTransparent(bg) || anyBorder(style);
}

function styleHasShadowOrRadius(style) {
  return style.borderTopLeftRadius > 0 || !!parseBoxShadow(style.boxShadow);
}

function wantsCoverCrop(style) {
  return style.objectFit === 'cover'
    || (style.backgroundSize || '').split(' ')[0] === 'cover';
}

/**
 * Build the instance list + variable list for a box tree.
 * @param {Map<string,string>} assetMap Downloaded-image URL -> relative SourceFile path
 *   (see convert.mjs's downloadImages). Nodes whose image URL isn't in the map (download
 *   failed, or nothing to download) get a warning instead of a dangling SourceFile.
 * @param {Map|null} responsiveMap Path-key -> {width,height} inference from
 *   responsive.mjs's computeResponsiveMap (two-viewport comparison). When null
 *   (convert.mjs --no-responsive), every axis stays Absolute at the measured value.
 *   When present, PercentageOfParent / RelativeToParent units come from inference;
 *   Absolute values still use this tree's measured px (geometry viewport).
 * @param {Map|null} fontMap fontFaceKey(family,weight,style) -> "Fonts/….ttf" from
 *   fonts.mjs. When present, Text.Font is the .ttf path (gumcli fonts / FontCache);
 *   weight/style are baked into the file so IsBold/IsItalic are omitted.
 * @returns {{instances: {name,baseType}[], variables: {}[], warnings: string[]}}
 */
export function mapTreeToScreen(
  root: BoxNode,
  assetMap: Map<string, string> = new Map(),
  responsiveMap: ResponsiveMap | null = null,
  fontMap: Map<string, string> | null = null,
  nineSliceMap: Map<string, NineSliceInfo> | null = null,
): MappedScreen {
  const namer = makeNamer();
  const instances = [];
  const variables = [];
  const warnings = [];

  function orientationOf(node) {
    return layoutModeOf(node);
  }

  // Sets ChildrenLayout (+ StackSpacing / AutoGrid cell counts) on targetName, or warns
  // and leaves Regular (free positioning) for unsupported layouts. Shared by both the
  // no-backdrop path and the backdrop path (synthetic inner content wrapper).
  function applyChildrenLayout(targetName, node, orientation, warnAsName) {
    if (orientation === 'row' || orientation === 'column') {
      variables.push(v('ChildrenLayout', `${targetName}.ChildrenLayout`, 'xsd:int',
        orientation === 'row' ? CL.LeftToRightStack : CL.TopToBottomStack));
      const gap = orientation === 'row' ? node.style.columnGap : node.style.rowGap;
      if (gap > 0) variables.push(VF(`${targetName}.StackSpacing`, gap));
      return;
    }
    if (orientation === 'grid-h' || orientation === 'grid-v') {
      const cols = parseGridTracks(node.style.gridTemplateColumns);
      let rows = parseGridTracks(node.style.gridTemplateRows);
      const colCount = cols.length;
      // Implicit rows: CSS auto-creates them; Gum needs an explicit vertical cell count.
      // Use the authored row track count when present, else enough cells to hold every child.
      let rowCount = rows.length > 0 ? rows.length : Math.max(1, Math.ceil(node.children.length / colCount));
      // Prefer enough cells for all children so nothing spills outside the grid bounds.
      rowCount = Math.max(rowCount, Math.ceil(node.children.length / colCount) || 1);

      variables.push(v('ChildrenLayout', `${targetName}.ChildrenLayout`, 'xsd:int',
        orientation === 'grid-h' ? CL.AutoGridHorizontal : CL.AutoGridVertical));
      variables.push(VI(`${targetName}.AutoGridHorizontalCells`, colCount));
      variables.push(VI(`${targetName}.AutoGridVerticalCells`, rowCount));

      // Gum AutoGrid has one StackSpacing for both axes. If row-gap ≠ column-gap, pick
      // column-gap (matches AutoGridHorizontal's primary axis) and warn — not silently
      // wrong on the common equal-gap case.
      const rg = node.style.rowGap || 0;
      const cg = node.style.columnGap || 0;
      if (rg > 0 || cg > 0) {
        if (rg > 0 && cg > 0 && Math.abs(rg - cg) > 1) {
          warnings.push(`"${warnAsName}" grid has unequal row-gap (${rg}px) and column-gap (${cg}px) — Gum AutoGrid has one StackSpacing; used column-gap.`);
        }
        variables.push(VF(`${targetName}.StackSpacing`, cg || rg));
      }
      return;
    }

    // Explain *why* we fell back when the author wrote display:grid but it wasn't uniform.
    if (node.style.display === 'grid' || node.style.display === 'inline-grid') {
      const cols = parseGridTracks(node.style.gridTemplateColumns);
      const rows = parseGridTracks(node.style.gridTemplateRows);
      let reason = 'unsupported grid';
      if (cols.length === 0) reason = 'no resolvable column tracks';
      else if (!tracksAreUniform(cols)) reason = 'non-uniform column tracks (AutoGrid cells are equal-sized)';
      else if (rows.length > 0 && !tracksAreUniform(rows)) reason = 'non-uniform row tracks (AutoGrid cells are equal-sized)';
      else if (childrenHaveExplicitGridPlacement(node)) {
        reason = childrenHaveGridSpans(node)
          ? 'grid item span / placement (AutoGrid is DOM-order equal cells only)'
          : 'explicit grid placement (AutoGrid is DOM-order equal cells only)';
      } else if (node.children.some((c) => (c.style.order || 0) !== 0)) {
        reason = 'grid item order (AutoGrid ignores CSS order)';
      }
      warnings.push(`"${warnAsName}" (${node.tag}) display:grid but ${reason} — children positioned absolutely (Tier 1 fallback).`);
    } else {
      warnings.push(`"${warnAsName}" (${node.tag}) is not display:flex/grid — children positioned absolutely (Tier 1 fallback).`);
    }
  }

  /** Apply stack/grid layout vars, or emit a Tier-1 fallback warning for unsupported grid. */
  function applyLayoutOrWarn(targetName, node, orientation, childLayout, warnAsName) {
    if (childLayout !== 'block') {
      applyChildrenLayout(targetName, node, orientation, warnAsName);
      return;
    }
    // layoutModeOf already chose block for mixed tracks / spans — still explain why.
    if (node.style.display === 'grid' || node.style.display === 'inline-grid') {
      applyChildrenLayout(targetName, node, 'block', warnAsName);
    }
  }

  // Full-fill (Dock.Fill-equivalent) sizing: same width/height as the parent, positioned
  // at its origin. Used for both halves of the backdrop+content overlay below.
  function fillParent(name) {
    variables.push(
      VDIM(`${name}.WidthUnits`, DIM.RelativeToParent), VF(`${name}.Width`, 0),
      VDIM(`${name}.HeightUnits`, DIM.RelativeToParent), VF(`${name}.Height`, 0),
    );
  }

  /** Inset a child by CSS padding (Content area). RelativeToParent negative = parent − pad. */
  function insetParent(name, pad) {
    variables.push(
      VF(`${name}.X`, Math.round(pad.left)),
      VF(`${name}.Y`, Math.round(pad.top)),
      VDIM(`${name}.WidthUnits`, DIM.RelativeToParent), VF(`${name}.Width`, -Math.round(pad.left + pad.right)),
      VDIM(`${name}.HeightUnits`, DIM.RelativeToParent), VF(`${name}.Height`, -Math.round(pad.top + pad.bottom)),
    );
  }

  function fillOrInset(name, pad) {
    if (hasPadding(pad)) insetParent(name, pad);
    else fillParent(name);
  }

  /**
   * CSS flex stretch only applies when the used cross size would be `auto`.
   * Explicit width/height (%, px, …) keeps that definite size — Froggy frogs are
   * `height: 20%` under default `align-items: stretch`.
   */
  function hasDefiniteCrossSize(style, parentOrientation) {
    if (!style) return false;
    const raw = parentOrientation === 'column' ? style.widthSpecified : style.heightSpecified;
    const s = String(raw || '').trim().toLowerCase();
    return s !== '' && s !== 'auto';
  }

  /**
   * Cross-axis sizing + alignment for a flex stack child.
   * stretch + indefinite → RelativeToParent 0; otherwise hug / % size and origin-align.
   * Child `align-self` overrides parent `align-items` when not `auto`.
   *
   * A literal (non-auto) margin on the cross-start/end edge still offsets the child even
   * under flex-start/stretch alignment — CSS aligns the item's *margin* box to the line,
   * not its border box (e.g. iana.org's <article style="display:flex"><main
   * style="margin-top:25px">: main's border-box sits 25px below the container's
   * cross-start, and its stretched height shrinks by that margin).
   */
  function emitStackCrossAxis(name, parentOrientation, alignItems, measuredCross, style) {
    const self = (style?.alignSelf || 'auto').toLowerCase();
    const align = (self !== 'auto' ? self : (alignItems || 'stretch')).toLowerCase();
    const stretch = isAlignStretch(align) && !hasDefiniteCrossSize(style, parentOrientation);
    const axisName = parentOrientation === 'column' ? 'Width' : 'Height';
    const posName = parentOrientation === 'column' ? 'X' : 'Y';
    const crossStart = (parentOrientation === 'column' ? style?.marginLeft : style?.marginTop) || 0;
    const crossEnd = (parentOrientation === 'column' ? style?.marginRight : style?.marginBottom) || 0;
    if (stretch) {
      variables.push(
        VDIM(`${name}.${axisName}Units`, DIM.RelativeToParent),
        VF(`${name}.${axisName}`, -Math.round(crossStart + crossEnd)),
      );
      if (crossStart > 0) variables.push(VF(`${name}.${posName}`, Math.round(crossStart)));
      return;
    }
    const pct = specifiedPercent(style, axisName);
    if (pct != null) {
      variables.push(VDIM(`${name}.${axisName}Units`, DIM.PercentageOfParent), VF(`${name}.${axisName}`, Math.round(pct)));
    } else {
      variables.push(VDIM(`${name}.${axisName}Units`, DIM.Absolute), VF(`${name}.${axisName}`, Math.round(measuredCross)));
    }
    if (isAlignStretch(align)) {
      // Definite size under stretch → cross-start, offset only by the child's own margin.
      if (crossStart > 0) variables.push(VF(`${name}.${posName}`, Math.round(crossStart)));
      return;
    }
    if (parentOrientation === 'column') {
      if (align === 'center') {
        variables.push(VPOS(`${name}.XUnits`, POS.PixelsFromCenterX), VOH(`${name}.XOrigin`, ORIGIN_H.Center), VF(`${name}.X`, 0));
      } else if (align === 'flex-end' || align === 'end' || align === 'right') {
        variables.push(VPOS(`${name}.XUnits`, POS.PixelsFromRight), VOH(`${name}.XOrigin`, ORIGIN_H.Right), VF(`${name}.X`, 0));
      }
    } else if (align === 'center') {
      variables.push(VPOS(`${name}.YUnits`, POS.PixelsFromCenterY), VOV(`${name}.YOrigin`, ORIGIN_V.Center), VF(`${name}.Y`, 0));
    } else if (align === 'flex-end' || align === 'end' || align === 'bottom') {
      variables.push(VPOS(`${name}.YUnits`, POS.PixelsFromBottom), VOV(`${name}.YOrigin`, ORIGIN_V.Bottom), VF(`${name}.Y`, 0));
    }
  }

  // Four thin edge Rectangles as children of parentName — Gum Rectangle has one stroke,
  // so asymmetric CSS borders become four filled bars (figma-html technique).
  function emitBorderEdges(parentName, edges) {
    for (const edge of edges) {
      const en = namer.mint(`${parentName}Border${edge.side}`);
      instances.push({ name: en, baseType: 'Rectangle' });
      variables.push(VS(`${en}.Parent`, parentName));
      const c = edge.color;
      variables.push(
        VI(`${en}.FillRed`, c.r), VI(`${en}.FillGreen`, c.g), VI(`${en}.FillBlue`, c.b),
        VB(`${en}.IsFilled`, true), VF(`${en}.StrokeWidth`, 0),
      );
      if (c.a !== 255) variables.push(VI(`${en}.FillAlpha`, c.a));
      if (edge.side === 'Top') {
        variables.push(
          VF(`${en}.X`, 0), VF(`${en}.Y`, 0),
          VDIM(`${en}.WidthUnits`, DIM.RelativeToParent), VF(`${en}.Width`, 0),
          VF(`${en}.Height`, edge.width),
        );
      } else if (edge.side === 'Bottom') {
        variables.push(
          VF(`${en}.X`, 0),
          VPOS(`${en}.YUnits`, POS.PixelsFromBottom), VOV(`${en}.YOrigin`, ORIGIN_V.Bottom), VF(`${en}.Y`, 0),
          VDIM(`${en}.WidthUnits`, DIM.RelativeToParent), VF(`${en}.Width`, 0),
          VF(`${en}.Height`, edge.width),
        );
      } else if (edge.side === 'Left') {
        variables.push(
          VF(`${en}.X`, 0), VF(`${en}.Y`, 0),
          VF(`${en}.Width`, edge.width),
          VDIM(`${en}.HeightUnits`, DIM.RelativeToParent), VF(`${en}.Height`, 0),
        );
      } else { // Right
        variables.push(
          VPOS(`${en}.XUnits`, POS.PixelsFromRight), VOH(`${en}.XOrigin`, ORIGIN_H.Right), VF(`${en}.X`, 0),
          VF(`${en}.Y`, 0),
          VF(`${en}.Width`, edge.width),
          VDIM(`${en}.HeightUnits`, DIM.RelativeToParent), VF(`${en}.Height`, 0),
        );
      }
    }
  }

  function applyOpacity(kind, name, style) {
    if (style.opacity == null || style.opacity >= 1 || style.opacity <= 0) return;
    // Rectangle uses FillAlpha/StrokeAlpha (handled in buildRectFillVars). Others use Alpha.
    if (kind === 'rect') return;
    variables.push(VI(`${name}.Alpha`, Math.round(style.opacity * 255)));
  }

  // HorizontalAlignment: 0 Left, 1 Center, 2 Right — VerticalAlignment: 0 Top, 1 Center, 2 Bottom.
  // text-align wins over flex justify when both are set.
  function applyTextAlign(name, style, opts = {}) {
    const h = textAlignHorizontal(style, opts);
    if (h != null) variables.push(v('HorizontalAlignment', `${name}.HorizontalAlignment`, 'xsd:int', h));
    const vert = textAlignVertical(style, opts);
    if (vert != null) variables.push(v('VerticalAlignment', `${name}.VerticalAlignment`, 'xsd:int', vert));
  }

  function emitSpriteSource(name, node) {
    const s = node.style;
    const url = node.rasterSrc || node.imgSrc || parseBackgroundImageUrl(s.backgroundImage);
    const asset = url && assetMap.get(url);
    if (asset) {
      variables.push(VS(`${name}.SourceFile`, asset));
      if (wantsCoverCrop(s) && node.naturalWidth > 0 && node.naturalHeight > 0) {
        const crop = computeCoverCrop(node.naturalWidth, node.naturalHeight, node.rect.width, node.rect.height);
        variables.push(
          v('TextureAddress', `${name}.TextureAddress`, 'xsd:int', 1), // Custom
          VI(`${name}.TextureLeft`, crop.left), VI(`${name}.TextureTop`, crop.top),
          VI(`${name}.TextureWidth`, crop.width), VI(`${name}.TextureHeight`, crop.height),
        );
      }
      return true;
    }
    if (!node.rasterSrc) {
      warnings.push(`"${name}" (${node.tag}) has no downloaded asset — src was missing or the download failed; Sprite has no SourceFile.`);
    } else {
      warnings.push(`"${name}" (${node.tag}) was flagged for rasterization but no sprite was captured.`);
    }
    return false;
  }

  // Web-font file path (B / gumcli fonts): style is baked into the instantiated TTF for
  // this exact family+weight+style. No mapping (system/KernSmith fallback) → caller falls
  // back to IsBold/IsItalic synthesis. Shared by emitVisual and the inline-run BBCode merge
  // so both pick the same font file for the same style.
  function resolveFontFile(s) {
    const family = firstFont(s.fontFamily);
    const weightNum = (() => {
      const w = s.fontWeight;
      if (w === 'bold' || w === 'bolder') return 700;
      if (w === 'normal' || w === 'lighter') return 400;
      const n = parseInt(w, 10);
      return Number.isFinite(n) ? n : 400;
    })();
    const styleKey = isItalic(s.fontStyle) ? 'italic' : 'normal';
    const famKey = (family || '').toLowerCase().replace(/\s+var\b/g, '').replace(/\s+/g, ' ').trim();
    const filePath = fontMap && family
      && (fontMap.get(`${famKey}|${weightNum}|${styleKey}`)
        || fontMap.get(`${famKey}|${weightNum}|normal`));
    return { family, filePath };
  }

  function emitVisual(kind, name, node, alignOpts = null, { colorOverride = null, skipOutline = false } = {}) {
    const s = node.style;
    if (kind === 'rect') {
      const { vars, warnings: w, asymmetricBorders } = buildRectFillVars(name, s);
      variables.push(...vars);
      warnings.push(...w);
      if (asymmetricBorders) emitBorderEdges(name, asymmetricBorders);
    } else if (kind === 'text') {
      variables.push(VS(`${name}.Text`, listMarkerPrefix(s) + node.text));
      if (s.fontSize) variables.push(VI(`${name}.FontSize`, Math.round(s.fontSize)));
      const col = colorOverride || parseColor(s.color);
      if (col) {
        variables.push(VI(`${name}.Red`, col.r), VI(`${name}.Green`, col.g), VI(`${name}.Blue`, col.b));
        if (col.a != null && col.a !== 255) variables.push(VI(`${name}.Alpha`, col.a));
      }
      const { family, filePath } = resolveFontFile(s);
      if (filePath) {
        variables.push(VS(`${name}.Font`, filePath));
      } else {
        if (isBold(s.fontWeight)) variables.push(VB(`${name}.IsBold`, true));
        if (isItalic(s.fontStyle)) variables.push(VB(`${name}.IsItalic`, true));
        // Only emit a bare family name when it's a common OS font. Emitting "Inter Var"
        // without an embedded TTF makes gumcli fonts fail (system lookup miss).
        if (family && /^(arial|times(?: new roman)?|courier(?: new)?|georgia|verdana|tahoma|segoe ui|helvetica|roboto|consolas)$/i.test(family)) {
          variables.push(VS(`${name}.Font`, family));
        } else if (family && !/^(sans-serif|serif|monospace|system-ui)$/i.test(family)) {
          warnings.push(`"${name}" font "${family}" not embedded — using Text default (Arial).`);
        }
      }
      applyTextAlign(name, s, alignOpts || {});
      if (!skipOutline) {
        const outline = parseTextOutlineThickness(s);
        if (outline > 0) variables.push(VI(`${name}.OutlineThickness`, outline));
      }
    } else if (kind === 'image') {
      emitSpriteSource(name, node);
    }
    applyOpacity(kind, name, s);
  }

  // Is this child a same-line inline-styled run that can fold into the parent's Text via
  // BBCode instead of becoming its own sibling Text? Must differ from the base run only in
  // weight/style (color support is a possible future extension — bail for now rather than
  // guess a BBCode Color format), have no chrome/shadow/padding of its own, and contain no
  // literal '[' or ']' (BBCode has no escape sequence for its own delimiters).
  function isSimpleInlineStyleChild(child, baseStyle) {
    if (!INLINE_PHRASING_TAGS.has(child.tag)) return false;
    if (child.lineCount > 1) return false;
    if (!child.text || /[[\]]/.test(child.text)) return false;
    if (hasVisualStyling(child.style)) return false;
    if (parseHardTextShadows(child.style).length > 0) return false;
    if (hasPadding(paddingOf(child.style))) return false;
    if (firstFont(child.style.fontFamily) !== firstFont(baseStyle.fontFamily)) return false;
    if (Math.round(child.style.fontSize) !== Math.round(baseStyle.fontSize)) return false;
    const baseColor = parseColor(baseStyle.color);
    const childColor = parseColor(child.style.color);
    return JSON.stringify(baseColor) === JSON.stringify(childColor);
  }

  // Folds a block-level text container's same-line inline runs (plain #text plus
  // <strong>/<b>/<em>/<i>/<a> weight-or-style-only children — e.g. iana.org's "...provided
  // by <a>Public Technical Identifiers</a>, an affiliate of <a>ICANN</a>.") into ONE Text
  // leaf using BBCode markup, instead of one sibling Text per run.
  //
  // Previously every run got its own WidthUnits=RelativeToChildren Text at a fixed Absolute
  // X lifted from Chromium. Gum's bitmap font renders each run at a slightly different pixel
  // width than Chromium measured (worst for bold), so the next run's fixed X drifted from
  // where the previous run actually ended — a visible gap before the run and an
  // overlap/garbling right after it. One Text lets Gum's own font engine lay out the whole
  // line itself, the same run-by-run measurement it already does for inline BBCode styling
  // (#3520) — self-consistent regardless of how its bitmap font compares to Chromium's.
  //
  // Only applies when every run sits on the same rendered line (paragraphs that wrap are
  // left alone — round 1 already handles per-line splitting for a single wrapping #text
  // node; mixing that with cross-run BBCode wrapping is a separate, untested feature).
  function mergeInlinePhrasingRun(node) {
    if (!node.children || node.children.length < 2) return null;
    if (!node.children.every((c) => isSimpleInlineStyleChild(c, node.style))) return null;
    const y0 = node.children[0].rect.y;
    if (!node.children.every((c) => Math.abs(c.rect.y - y0) < 1.5)) return null;

    const base = resolveFontFile(node.style);
    let text = '';
    for (const c of node.children) {
      let piece = c.text;
      const cf = resolveFontFile(c.style);
      if (cf.filePath && cf.filePath !== base.filePath) {
        piece = `[Font=${cf.filePath}]${piece}[/Font]`;
      } else if (!cf.filePath) {
        if (isBold(c.style.fontWeight) !== isBold(node.style.fontWeight)) {
          piece = `[IsBold=${isBold(c.style.fontWeight)}]${piece}[/IsBold]`;
        }
        if (isItalic(c.style.fontStyle) !== isItalic(node.style.fontStyle)) {
          piece = `[IsItalic=${isItalic(c.style.fontStyle)}]${piece}[/IsItalic]`;
        }
      }
      text += piece;
    }
    return { ...node, text, children: [], lineCount: 1 };
  }

  /** CSS hard text-shadow → black (or tinted) Text stamps behind the face label. */
  function emitTextWithHardShadows(parentName, baseName, node, shadows, pad, alignOpts, { hugWidth = false } = {}) {
    const placeLabel = (instName, ox = 0, oy = 0) => {
      // Single-line chips/badges: Width RelativeToChildren so BitmapFont never wraps
      // inside Chromium's tighter box ("Done" → "Do"/"ne"). Parent keeps Absolute size
      // for the chrome; label is centered in that box when preferCenterHorizontal.
      if (hugWidth) {
        const centerH = !!(alignOpts && alignOpts.preferCenterHorizontal);
        if (centerH) {
          variables.push(
            VPOS(`${instName}.XUnits`, POS.PixelsFromCenterX),
            VF(`${instName}.X`, ox),
            VOH(`${instName}.XOrigin`, ORIGIN_H.Center),
          );
        } else {
          variables.push(VF(`${instName}.X`, Math.round((pad.left || 0) + ox)));
        }
        variables.push(
          VF(`${instName}.Y`, Math.round((pad.top || 0) + oy)),
          VDIM(`${instName}.WidthUnits`, DIM.RelativeToChildren),
          VDIM(`${instName}.HeightUnits`, DIM.RelativeToParent),
          VF(`${instName}.Height`, -Math.round((pad.top || 0) + (pad.bottom || 0))),
        );
      } else if (hasPadding(pad)) {
        variables.push(
          VF(`${instName}.X`, Math.round(pad.left + ox)),
          VF(`${instName}.Y`, Math.round(pad.top + oy)),
          VDIM(`${instName}.WidthUnits`, DIM.RelativeToParent), VF(`${instName}.Width`, -Math.round(pad.left + pad.right)),
          VDIM(`${instName}.HeightUnits`, DIM.RelativeToParent), VF(`${instName}.Height`, -Math.round(pad.top + pad.bottom)),
        );
      } else {
        fillParent(instName);
        if (ox || oy) variables.push(VF(`${instName}.X`, ox), VF(`${instName}.Y`, oy));
      }
    };
    for (let i = 0; i < shadows.length; i++) {
      const sh = shadows[i];
      const sn = namer.mint(`${baseName}Sh${i}`);
      instances.push({ name: sn, baseType: 'Text' });
      variables.push(VS(`${sn}.Parent`, parentName));
      placeLabel(sn, sh.offsetX, sh.offsetY);
      emitVisual('text', sn, node, alignOpts, { colorOverride: sh.color, skipOutline: true });
    }
    const labelName = namer.mint(baseName);
    instances.push({ name: labelName, baseType: 'Text' });
    variables.push(VS(`${labelName}.Parent`, parentName));
    placeLabel(labelName);
    emitVisual('text', labelName, node, alignOpts, { skipOutline: true });
    return labelName;
  }

  // Emits WidthUnits/Width or HeightUnits/Height for one axis. Folds together two
  // concerns that must never both fire (they'd emit the same *Units variable name
  // twice, an undefined XML merge): responsive-inference units, when a responsiveMap
  // was supplied (see responsive.mjs — comparing two viewport renders distinguishes
  // "always 300px" from "always 25% of parent", something one snapshot cannot), and the
  // Sprite-default-isn't-Absolute fix (PercentageOfSourceFile, confirmed against the real
  // Sprite.gutx — every other standard defaults to Absolute so needs no explicit units
  // when nothing else applies).
  function emitSizeAxis(kind, name, path, axisName, fallbackMeasured, style) {
    const axisKey = axisName.toLowerCase();
    const inferred = responsiveMap && responsiveMap.get(path.join('.'))?.[axisKey];
    let units = null; // null = Absolute, the default for every standard except Sprite
    let value = fallbackMeasured; // always the geometry tree's measured px for Absolute
    if (inferred) {
      if (inferred.units === 'PercentageOfParent') {
        units = DIM.PercentageOfParent;
        value = inferred.value;
      } else if (inferred.units === 'RelativeToParent') {
        units = DIM.RelativeToParent;
        value = inferred.value;
      } else if (inferred.ambiguous) {
        const pct = specifiedPercent(style, axisName);
        if (pct != null) {
          units = DIM.PercentageOfParent;
          value = pct;
          warnings.push(`"${name}" ${axisName.toLowerCase()}: ambiguous between viewports — used inline ${pct}% from CSSOM.`);
        } else {
          warnings.push(`"${name}" ${axisName.toLowerCase()}: neither constant nor cleanly proportional between the two training viewports (likely a CSS breakpoint) — used the requested viewport's measured value as Absolute.`);
        }
      }
    } else {
      // --no-responsive (or no map entry): honor inline width/height: N%.
      const pct = specifiedPercent(style, axisName);
      if (pct != null) {
        units = DIM.PercentageOfParent;
        value = pct;
      }
    }
    if (units === null && kind === 'image') units = DIM.Absolute;
    if (units !== null) variables.push(VDIM(`${name}.${axisName}Units`, units));
    variables.push(VF(`${name}.${axisName}`, Math.round(value)));
  }

  // parent === null for the root; parentOrientation drives child sizing; path is this
  // node's child-index chain from the root ("0.1.0"), the key responsiveMap is keyed by.
  // parentAlignItems is the parent's align-items (cross-axis) for flex stacks.
  function walk(node, parentName, parentOrientation, parentRect, path = [], parentAlignItems = 'stretch', opts = {}) {
    node = mergeInlinePhrasingRun(node) || node;
    const kind = classify(node);
    const name = namer.forNode(node);
    // Image with a solid background-color/border (Cerberus broken-image affordance,
    // styled <img>): Sprite has no fill — wrap as Container → underlay Rectangle → Sprite
    // (same backdrop pattern as styled Containers, §8.6/§8.12).
    const underlay = kind === 'image' && imageNeedsUnderlay(node.style);
    // Text leaves with background/border/radius (game hotbar slots, chips, badges): Gum
    // Text has no fill — promote to Container → chrome → Text label (DESIGN §8.15).
    const textChrome = kind === 'text' && hasVisualStyling(node.style);
    // Hard text-shadow (RPGUI faux outline) → Container with stamped shadow Texts + face.
    const hardShadows = kind === 'text' ? parseHardTextShadows(node.style) : [];
    const textShadowWrap = hardShadows.length > 0;
    // Gum Text has no CSS padding — table cells, sidebar section labels, etc. must wrap
    // as Container + inset label or glyphs sit on the border-box edge.
    const textPad = kind === 'text' ? paddingOf(node.style) : paddingOf({});
    const textHasFrame = textChrome
      || (node.style.borderTopWidth || 0) > 0
      || (node.style.borderRightWidth || 0) > 0
      || (node.style.borderBottomWidth || 0) > 0
      || (node.style.borderLeftWidth || 0) > 0
      || !!(nineSliceMap && nineSliceMap.get(path.join('.')));
    const textInset = kind === 'text'
      ? contentInset(node.style, textPad, { hasFrame: textHasFrame })
      : paddingOf({});
    const textNeedsPad = kind === 'text' && hasPadding(textInset);
    const textWrap = textChrome || textShadowWrap || textNeedsPad;
    const nsSelf = nineSliceMap && nineSliceMap.get(path.join('.'));
    let baseType = (underlay || textWrap) ? 'Container' : BASE[kind];
    // Leaf rect with a generated 9-slice texture → NineSlice standard (not Rectangle).
    if (kind === 'rect' && nsSelf) baseType = 'NineSlice';
    instances.push({ name, baseType });

    const sizeKind = (underlay || textWrap) ? 'container' : kind;

    if (parentName === null) {
      // Root: absolute position from its rect (screen-space, top-left origin); size
      // goes through the same responsive-inference path as everything else, though in
      // practice the root has no parent to be percentage-relative to and stays Absolute.
      variables.push(VF(`${name}.X`, Math.round(node.rect.x)), VF(`${name}.Y`, Math.round(node.rect.y)));
      emitSizeAxis(sizeKind, name, path, 'Width', node.rect.width, node.style);
      emitSizeAxis(sizeKind, name, path, 'Height', node.rect.height, node.style);
    } else {
      variables.push(VS(`${name}.Parent`, parentName));
      if (kind === 'text' && !textWrap) {
        // Height is pinned to Chromium's measured line-box (Absolute) rather than
        // RelativeToChildren, because Gum's generated BitmapFont line height is taller
        // than Chromium's CSS line box — trusting the measured box keeps the vertical
        // flow matching Chromium (§4.3).
        if (isStackLayout(parentOrientation)) {
          if (parentOrientation === 'column') {
            emitStackCrossAxis(name, parentOrientation, parentAlignItems, node.rect.width, node.style);
            variables.push(VDIM(`${name}.HeightUnits`, DIM.Absolute), VF(`${name}.Height`, Math.round(node.rect.height)));
            // Non-stretch cross already set Absolute width; stretch set RelativeToParent.
            // Multi-line under stretch: width is parent-driven (wrap matches Chromium box).
            if (!isAlignStretch(parentAlignItems) && node.lineCount > 1) {
              // already Absolute width from emitStackCrossAxis
            } else if (isAlignStretch(parentAlignItems) && node.lineCount === 1) {
              // keep stretch width; single-line hugging would fight stretch — prefer stretch
            }
          } else {
            emitStackCrossAxis(name, parentOrientation, parentAlignItems, node.rect.height, node.style);
            if (node.lineCount > 1) {
              variables.push(VDIM(`${name}.WidthUnits`, DIM.Absolute), VF(`${name}.Width`, Math.round(node.rect.width)));
            } else if (node.style.flexGrow > 0) {
              variables.push(VDIM(`${name}.WidthUnits`, DIM.Ratio), VF(`${name}.Width`, Math.round(node.rect.width)));
            } else {
              variables.push(VDIM(`${name}.WidthUnits`, DIM.RelativeToChildren));
            }
          }
        } else {
          // Block / Absolute parent: pin height to Chromium's line-box. For single-line
          // left-aligned text, hug content width — BitmapFont glyphs are often slightly
          // wider than Chromium, and Absolute width would wrap ("Upgrade to" / "Pro").
          variables.push(
            VDIM(`${name}.HeightUnits`, DIM.Absolute), VF(`${name}.Height`, Math.round(node.rect.height)),
          );
          const hAlign = textAlignHorizontal(node.style);
          const centerOrRight = hAlign === 1 || hAlign === 2;
          if (node.lineCount > 1 || centerOrRight) {
            variables.push(
              VDIM(`${name}.WidthUnits`, DIM.Absolute), VF(`${name}.Width`, Math.round(node.rect.width)),
            );
          } else {
            variables.push(VDIM(`${name}.WidthUnits`, DIM.RelativeToChildren));
          }
          if (!isManagedLayout(parentOrientation)) {
            variables.push(
              VF(`${name}.X`, Math.round(node.rect.x - parentRect.x)),
              VF(`${name}.Y`, Math.round(node.rect.y - parentRect.y)),
            );
          }
        }
      } else if (isStackLayout(parentOrientation)) {
        const grow = opts.ignoreFlexGrow ? 0 : node.style.flexGrow;
        // NOTE: the Ratio VALUE is the measured main-axis length, not the flex-grow
        // number. CSS distributes only the free space left after each item's flex-basis
        // (intrinsic content size); Gum's Ratio splits ALL remaining space proportionally,
        // ignoring basis. Feeding measured lengths makes Gum's proportional split
        // reproduce Chromium's resolved sizes exactly (they sum to the same free space).
        if (parentOrientation === 'column') {
          emitStackCrossAxis(name, parentOrientation, parentAlignItems, node.rect.width, node.style);
          if (grow > 0) variables.push(VDIM(`${name}.HeightUnits`, DIM.Ratio), VF(`${name}.Height`, Math.round(node.rect.height)));
          else emitSizeAxis(sizeKind, name, path, 'Height', node.rect.height, node.style);
        } else {
          emitStackCrossAxis(name, parentOrientation, parentAlignItems, node.rect.height, node.style);
          if (grow > 0) variables.push(VDIM(`${name}.WidthUnits`, DIM.Ratio), VF(`${name}.Width`, Math.round(node.rect.width)));
          else emitSizeAxis(sizeKind, name, path, 'Width', node.rect.width, node.style);
        }
      } else if (isGridLayout(parentOrientation)) {
        // AutoGrid: each child's "parent" for RelativeToParent is its cell. CSS grid
        // items stretch by default (align/justify-items: stretch) — fill the cell.
        variables.push(
          VDIM(`${name}.WidthUnits`, DIM.RelativeToParent), VF(`${name}.Width`, 0),
          VDIM(`${name}.HeightUnits`, DIM.RelativeToParent), VF(`${name}.Height`, 0),
        );
      } else {
        // Absolute (Tier 1) fallback: position relative to parent's content/border rect.
        // Always use Chromium's measured px — authored width/height % on grid items is
        // relative to the grid *area* (cell), not the grid container. PercentageOfParent
        // against this parent would stretch items to the full garden (Garden L10 bug).
        // Keep Chromium Absolute size for the chrome plate; single-line badge labels use
        // hugWidth (RelativeToChildren) so BitmapFont won't wrap in a tight text box.
        variables.push(
          VF(`${name}.X`, Math.round(node.rect.x - parentRect.x)),
          VF(`${name}.Y`, Math.round(node.rect.y - parentRect.y)),
          VDIM(`${name}.WidthUnits`, DIM.Absolute), VF(`${name}.Width`, Math.round(node.rect.width)),
          VDIM(`${name}.HeightUnits`, DIM.Absolute), VF(`${name}.Height`, Math.round(node.rect.height)),
        );
      }
    }

    if (underlay) {
      const bgName = namer.mint(`${name}Bg`);
      instances.push({ name: bgName, baseType: 'Rectangle' });
      variables.push(VS(`${bgName}.Parent`, name));
      fillParent(bgName);
      const { vars, warnings: w, asymmetricBorders } = buildRectFillVars(bgName, node.style);
      variables.push(...vars);
      warnings.push(...w);
      if (asymmetricBorders) emitBorderEdges(bgName, asymmetricBorders);

      const url = node.rasterSrc || node.imgSrc || parseBackgroundImageUrl(node.style.backgroundImage);
      if (url && assetMap.get(url)) {
        const sprName = namer.mint(`${name}Img`);
        instances.push({ name: sprName, baseType: 'Sprite' });
        variables.push(VS(`${sprName}.Parent`, name));
        fillParent(sprName); // explicit RelativeToParent — Sprite's odd default
        emitSpriteSource(sprName, node);
        applyOpacity('image', sprName, node.style);
      } else if (node.imgSrc || parseBackgroundImageUrl(node.style.backgroundImage)) {
        warnings.push(`"${name}" (${node.tag}) image download failed — showing background-color underlay only.`);
      }
      applyOpacity('container', name, node.style);
    } else if (textChrome) {
      // Styled text leaf → chrome behind + Text label (hotbar slots, chips, badges, #gold).
      // May have both background-image and border-image (RPGUI framed-grey) — layer both.
      const bgImageUrl = parseBackgroundImageUrl(node.style.backgroundImage);
      const urlAsset = bgImageUrl && assetMap.get(bgImageUrl);
      const rasterAsset = node.rasterSrc && assetMap.get(node.rasterSrc);
      const bgAsset = rasterAsset || urlAsset;
      if (bgAsset) {
        const sprName = namer.mint(`${name}Bg`);
        instances.push({ name: sprName, baseType: 'Sprite' });
        variables.push(VS(`${sprName}.Parent`, name));
        fillParent(sprName);
        if (rasterAsset) variables.push(VS(`${sprName}.SourceFile`, rasterAsset));
        else {
          const fakeNode = { ...node, imgSrc: null, rasterSrc: null };
          emitSpriteSource(sprName, fakeNode);
        }
      }
      // Rasterized border-image chrome already includes the frame — don't stack NineSlice.
      if (nsSelf && !rasterAsset) {
        const nsName = namer.mint(`${name}Ns`);
        instances.push({ name: nsName, baseType: 'NineSlice' });
        variables.push(VS(`${nsName}.Parent`, name));
        fillParent(nsName);
        variables.push(VS(`${nsName}.SourceFile`, nsSelf.sourceFile));
        variables.push(v('float?', `${nsName}.CustomFrameTextureCoordinateWidth`, 'xsd:float', nsSelf.frameWidth));
        if (nsSelf.tiling) variables.push(VB(`${nsName}.IsTilingMiddleSections`, true));
      } else if (!bgAsset) {
        const bgName = namer.mint(`${name}Bg`);
        instances.push({ name: bgName, baseType: 'Rectangle' });
        variables.push(VS(`${bgName}.Parent`, name));
        fillParent(bgName);
        const { vars, warnings: w, asymmetricBorders } = buildRectFillVars(bgName, node.style);
        variables.push(...vars);
        warnings.push(...w);
        if (asymmetricBorders) emitBorderEdges(bgName, asymmetricBorders);
      }
      // Single-line chrome (badges): prefer center. Label Width=RelativeToChildren so
      // BitmapFont can't wrap inside Chromium's tighter measured box ("Done").
      const alignOpts = {
        preferCenterVertical: node.lineCount <= 1,
        preferCenterHorizontal: node.lineCount <= 1,
      };
      emitTextWithHardShadows(name, `${name}Label`, node, hardShadows, textInset, alignOpts, {
        hugWidth: (node.lineCount || 1) <= 1,
      });
      applyOpacity('container', name, node.style);
    } else if ((textShadowWrap || textNeedsPad) && kind === 'text') {
      const alignOpts = {
        preferCenterVertical: node.lineCount <= 1,
        preferCenterHorizontal: false,
      };
      emitTextWithHardShadows(name, `${name}Label`, node, hardShadows, textInset, alignOpts);
      applyOpacity('container', name, node.style);
    } else if (kind === 'rect' && nsSelf) {
      variables.push(VS(`${name}.SourceFile`, nsSelf.sourceFile));
      variables.push(v('float?', `${name}.CustomFrameTextureCoordinateWidth`, 'xsd:float', nsSelf.frameWidth));
      if (nsSelf.tiling) variables.push(VB(`${name}.IsTilingMiddleSections`, true));
      applyOpacity('rect', name, node.style);
    } else {
      emitVisual(kind, name, node);
    }

    if (kind === 'container') {
      const orientation = orientationOf(node);
      if (node.style.display === 'flex') {
        const wrap = (node.style.flexWrap || 'nowrap').toLowerCase();
        if (wrap === 'wrap' || wrap === 'wrap-reverse') {
          warnings.push(
            `"${name}" flex-wrap:${wrap} — Gum stacks are single-line; children Absolute from Chromium boxes.`,
          );
        }
      }
      const pad = paddingOf(node.style);
      const ns = nineSliceMap && nineSliceMap.get(path.join('.'));
      const hasFrame = !!ns
        || (node.style.borderTopWidth || 0) > 0
        || (node.style.borderRightWidth || 0) > 0
        || (node.style.borderBottomWidth || 0) > 0
        || (node.style.borderLeftWidth || 0) > 0;
      // Gum NineSlice/borders are paint overlays on the border-box; CSS children live in
      // the padding box (border-box inset by border). Inset Content by border+padding so
      // kids don't cover the frame (RPGUI ornate borders).
      const inset = contentInset(node.style, pad, { hasFrame });
      const alignItems = node.style.alignItems || 'stretch';
      const reversed = isStackLayout(orientation) && isFlexReversed(node.style);
      let justify = node.style.justifyContent || 'normal';
      if (reversed) {
        justify = invertMainStartEnd(justify);
        warnings.push(
          `"${name}" flex-direction:${node.style.flexDirection} — stack children emitted in reverse DOM order with start/end justify inverted.`,
        );
      }
      const spacerMode = isStackLayout(orientation) ? justifySpacerMode(justify) : null;
      // Stacks lay out in DOM order. Paint-order sorting is only for absolute/block
      // children (z-index). Using z-order inside a stack would scramble flex positions.
      let childLayout = orientation;
      if (isStackLayout(orientation) && !isJustifyStart(justify) && !spacerMode) {
        warnings.push(`"${name}" justify-content:${justify} — unsupported; children positioned absolutely from Chromium's measured boxes.`);
        childLayout = 'block';
      }
      if (isStackLayout(childLayout) && childrenHavePushMargins(node, orientation)) {
        warnings.push(
          `"${name}" has flex children with margin push (e.g. ms-auto) — Absolute from Chromium boxes.`,
        );
        childLayout = 'block';
      }
      const childParentRect = contentBoxRect(node.rect, inset);

      function emitRatioSpacer(parentForKids, suffix, ratio = 1) {
        const sn = namer.mint(`${name}Spacer${suffix}`);
        instances.push({ name: sn, baseType: 'Container' });
        variables.push(VS(`${sn}.Parent`, parentForKids));
        if (orientation === 'row') {
          variables.push(VDIM(`${sn}.WidthUnits`, DIM.Ratio), VF(`${sn}.Width`, ratio));
          variables.push(VDIM(`${sn}.HeightUnits`, DIM.RelativeToParent), VF(`${sn}.Height`, 0));
        } else {
          variables.push(VDIM(`${sn}.HeightUnits`, DIM.Ratio), VF(`${sn}.Height`, ratio));
          variables.push(VDIM(`${sn}.WidthUnits`, DIM.RelativeToParent), VF(`${sn}.Width`, 0));
        }
      }

      function walkKids(parentForKids) {
        const usePaintOrder = childLayout === 'block';
        let list = usePaintOrder
          ? childrenInPaintOrder(node)
          : node.children.map((c, domIndex) => ({ c, domIndex }));

        if (isStackLayout(childLayout)) {
          // CSS order (then DOM index). Reverse after sort for *-reverse directions.
          list = list.slice().sort((a, b) => {
            const oa = a.c.style.order || 0;
            const ob = b.c.style.order || 0;
            return (oa - ob) || (a.domIndex - b.domIndex);
          });
          if (reversed) list.reverse();
        }

        const walkOne = ({ c, domIndex }) => {
          const pos = (c.style.position || 'static').toLowerCase();
          // Out-of-flow: CSS fixed/absolute leave the flex/grid formatting context.
          // Fixed → screen-top-level (no Parent), viewport Absolute coords (like root).
          // Absolute → Absolute under the nearest in-tree parent box, out of stack/grid.
          if (pos === 'fixed') {
            warnings.push(
              `position:fixed under "${name}" → emitted as screen-level Absolute (viewport coords), out of flex/grid flow.`,
            );
            walk(c, null, null, null, [...path, domIndex], 'stretch', opts);
            return;
          }
          if (pos === 'sticky') {
            warnings.push(
              `position:sticky under "${name}" — treated as in-flow snapshot (no sticky scrolling).`,
            );
          }
          if (pos === 'absolute' && isManagedLayout(childLayout)) {
            warnings.push(
              `position:absolute under "${name}" → Absolute on outer box, removed from ${childLayout} flow.`,
            );
            // Parent to the OUTER instance (`name`), which uses Regular layout (Bg +
            // Content). Parenting into *Content would put the node back in the stack/grid.
            // Position relative to the border box — matches CSS absolute when the
            // container is the containing block (position:relative/absolute/fixed).
            walk(c, name, 'block', node.rect, [...path, domIndex], 'stretch', opts);
            return;
          }
          if (spacerMode && (c.style.flexGrow || 0) > 0) {
            warnings.push(`"${name}" child has flex-grow with justify-content:${justify} — using measured Absolute size; grow+justify together is not fully mapped.`);
          }
          walk(c, parentForKids, childLayout, childParentRect, [...path, domIndex], alignItems, {
            ignoreFlexGrow: !!spacerMode,
          });
        };

        if (spacerMode && list.length > 0) {
          if (spacerMode === 'center') {
            emitRatioSpacer(parentForKids, 'Lead');
            list.forEach(walkOne);
            emitRatioSpacer(parentForKids, 'Trail');
          } else if (spacerMode === 'end') {
            emitRatioSpacer(parentForKids, 'Lead');
            list.forEach(walkOne);
          } else if (spacerMode === 'between') {
            list.forEach((item, i) => {
              if (i > 0) emitRatioSpacer(parentForKids, `Gap${i}`);
              walkOne(item);
            });
          } else if (spacerMode === 'around') {
            // End gutters half of between: Ratio 1 — item — 2 — item — 2 — item — 1
            emitRatioSpacer(parentForKids, 'Lead', 1);
            list.forEach((item, i) => {
              if (i > 0) emitRatioSpacer(parentForKids, `Gap${i}`, 2);
              walkOne(item);
            });
            emitRatioSpacer(parentForKids, 'Trail', 1);
          } else if (spacerMode === 'evenly') {
            emitRatioSpacer(parentForKids, 'Lead', 1);
            list.forEach((item, i) => {
              if (i > 0) emitRatioSpacer(parentForKids, `Gap${i}`, 1);
              walkOne(item);
            });
            emitRatioSpacer(parentForKids, 'Trail', 1);
          }
        } else {
          list.forEach(walkOne);
        }
      }

      if (hasVisualStyling(node.style)) {
        // Gum's Container is a pure layout group — it has no fill/border/radius of its
        // own. Restructure as: this instance becomes a free-positioned OUTER wrapper
        // holding overlapping children — fill / background Sprite / NineSlice frame
        // (painted first, behind) and an inner Container for real ChildrenLayout + DOM
        // kids. RPGUI framed panels use BOTH a tiled parchment background-image AND a
        // border-image; previously the Sprite path skipped NineSlice entirely.
        const bgImageUrl = parseBackgroundImageUrl(node.style.backgroundImage);
        const rasterAsset = node.rasterSrc && assetMap.get(node.rasterSrc);
        const urlAsset = bgImageUrl && assetMap.get(bgImageUrl);
        const bgAsset = rasterAsset || urlAsset;
        const bgColor = parseColor(node.style.backgroundColor);
        let paintedBackdrop = false;

        if (bgAsset) {
          // Photo/gradient/tiled backdrop: optional solid underlay, then Sprite.
          // When chrome is a baked raster (gradient / border-image), skip underlay —
          // the screenshot already includes fill + frame.
          const wantUnderlay = !rasterAsset && (
            !isTransparent(bgColor)
            || (!ns && (anyBorder(node.style) || styleHasShadowOrRadius(node.style)))
          );
          if (wantUnderlay) {
            const underName = namer.mint(`${name}BgFill`);
            instances.push({ name: underName, baseType: 'Rectangle' });
            variables.push(VS(`${underName}.Parent`, name));
            fillParent(underName);
            const { vars, warnings: w, asymmetricBorders } = buildRectFillVars(underName, node.style);
            variables.push(...vars);
            warnings.push(...w);
            if (asymmetricBorders) emitBorderEdges(underName, asymmetricBorders);
            paintedBackdrop = true;
          }
          const sprName = namer.mint(`${name}Bg`);
          instances.push({ name: sprName, baseType: 'Sprite' });
          variables.push(VS(`${sprName}.Parent`, name));
          fillParent(sprName);
          if (rasterAsset) {
            variables.push(VS(`${sprName}.SourceFile`, rasterAsset));
          } else {
            const fakeNode = { ...node, imgSrc: null, rasterSrc: null };
            emitSpriteSource(sprName, fakeNode);
          }
          paintedBackdrop = true;
        } else if (!isTransparent(bgColor) || anyBorder(node.style) || styleHasShadowOrRadius(node.style)) {
          // Solid / border chrome without a downloaded image — only when no NineSlice
          // will cover the same role (generated ns already includes fill+border).
          if (!ns) {
            const fillName = namer.mint(`${name}Bg`);
            instances.push({ name: fillName, baseType: 'Rectangle' });
            variables.push(VS(`${fillName}.Parent`, name));
            fillParent(fillName);
            const { vars, warnings: w, asymmetricBorders } = buildRectFillVars(fillName, node.style);
            variables.push(...vars);
            warnings.push(...w);
            if (asymmetricBorders) emitBorderEdges(fillName, asymmetricBorders);
            paintedBackdrop = true;
          }
        }

        // Rasterized border-image / gradient chrome already includes the frame.
        if (ns && !rasterAsset) {
          // border-image / generated 9-slice — on top of parchment so the frame shows.
          const nsName = namer.mint(`${name}Ns`);
          instances.push({ name: nsName, baseType: 'NineSlice' });
          variables.push(VS(`${nsName}.Parent`, name));
          fillParent(nsName);
          variables.push(VS(`${nsName}.SourceFile`, ns.sourceFile));
          variables.push(v('float?', `${nsName}.CustomFrameTextureCoordinateWidth`, 'xsd:float', ns.frameWidth));
          if (ns.tiling) variables.push(VB(`${nsName}.IsTilingMiddleSections`, true));
          paintedBackdrop = true;
        } else if (!paintedBackdrop) {
          const bgName = namer.mint(`${name}Bg`);
          instances.push({ name: bgName, baseType: 'Rectangle' });
          variables.push(VS(`${bgName}.Parent`, name));
          fillParent(bgName);
          const { vars, warnings: w, asymmetricBorders } = buildRectFillVars(bgName, node.style);
          variables.push(...vars);
          warnings.push(...w);
          if (asymmetricBorders) emitBorderEdges(bgName, asymmetricBorders);
        }

        if (bgImageUrl && !bgAsset) {
          warnings.push(`"${name}" has a background-image that failed to download (${bgImageUrl}) — fell back to its solid-color / NineSlice styling only.`);
        }

        const contentName = namer.mint(`${name}Content`);
        instances.push({ name: contentName, baseType: 'Container' });
        variables.push(VS(`${contentName}.Parent`, name));
        fillOrInset(contentName, inset);
        applyLayoutOrWarn(contentName, node, orientation, childLayout, name);
        // overflow:hidden clips at the border box (outer), not the padding box.
        if (node.style.overflow !== 'visible') variables.push(VB(`${name}.ClipsChildren`, true));
        walkKids(contentName);
      } else if (hasPadding(inset) && (isManagedLayout(orientation) || node.children.length > 0)) {
        // Padding/border inset without a painted backdrop: still need an inset content
        // wrapper so flex/grid children don't start at the border-box origin.
        const contentName = namer.mint(`${name}Content`);
        instances.push({ name: contentName, baseType: 'Container' });
        variables.push(VS(`${contentName}.Parent`, name));
        fillOrInset(contentName, inset);
        applyLayoutOrWarn(contentName, node, orientation, childLayout, name);
        if (node.style.overflow !== 'visible') variables.push(VB(`${name}.ClipsChildren`, true));
        walkKids(contentName);
      } else {
        applyLayoutOrWarn(name, node, orientation, childLayout, name);
        if (node.style.overflow !== 'visible') variables.push(VB(`${name}.ClipsChildren`, true));
        walkKids(name);
      }
    }

    return name;
  }

  walk(root, null, null, null);
  return { instances, variables, warnings };
}

// ---- serialize to compact .gusx XML ----------------------------------------
function xmlEscape(s) {
  return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

export function toGusx(screenName: string, { instances, variables }: MappedScreen) {
  const varXml = variables.map((vr) =>
    `    <Variable Type="${vr.type}" Name="${xmlEscape(vr.name)}" SetsValue="true">\n` +
    `      <Value xsi:type="${vr.valueType}">${xmlEscape(vr.value)}</Value>\n` +
    `    </Variable>`).join('\n');
  const instXml = instances.map((i) =>
    `  <Instance Name="${xmlEscape(i.name)}" BaseType="${xmlEscape(i.baseType)}" />`).join('\n');

  return `<?xml version="1.0" encoding="utf-8"?>
<ScreenSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Name>${screenName}</Name>
  <State>
    <Name>Default</Name>
${varXml}
  </State>
${instXml}
  <Behaviors />
</ScreenSave>
`;
}
