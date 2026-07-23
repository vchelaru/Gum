// @ts-nocheck
// The DOM walk that runs INSIDE the page (via page.evaluate). It reads Chromium's
// *resolved* layout — getBoundingClientRect + getComputedStyle — for the subtree rooted
// at `rootSelector`, and returns a plain-JSON box tree. No CSS is parsed by us; Chromium
// already resolved the cascade, flexbox, units, and inheritance.
//
// Async: after the sync walk, background-image leaves get naturalWidth/Height via Image()
// so map.ts can apply object-fit:cover / background-size:cover crops (same as <img>).
import type { BoxNode } from './types.js';

export async function extractBoxTree(rootSelector: string): Promise<BoxNode> {
  // display:none / visibility:hidden / opacity:0 elements have no visual presence — CSS
  // removes display:none from layout entirely, not just from paint. Skipping them (and
  // their whole subtree) here matches that: an emitted Gum instance has no "not laid out
  // at all" state, so the closest correct behavior is to not emit one. Common in real
  // pages (collapsed dropdowns, inactive tabs, modal content) that never showed up in
  // either hand-written fixture.
  function isVisible(el) {
    const cs = getComputedStyle(el);
    return cs.opacity !== '0' && cs.display !== 'none' && cs.visibility !== 'hidden';
  }

  function bgImageUrl(str) {
    if (!str || str === 'none') return null;
    const m = str.match(/url\((['"]?)(.*?)\1\)/);
    return m ? m[2] : null;
  }

  // Paint that can't map to Rectangle/Sprite fill primitives.
  // - needsRaster: gradient / filter / border-image / inline SVG / ::before|::after chrome
  // - rasterWholeSubtree: CSS filter applies to descendants too, so the sprite must
  //   bake children in. Gradients and border-image only need a backdrop; kids stay structured.
  //   Inline SVG and pseudo-element icons bake the whole host (Gum has no SVG/stroke path).
  // border-image (RPGUI frames) is screenshotted with kids/text hidden — Gum NineSlice
  // can't match ornate atlas + border-image-width scaling well enough for ≤5%.
  function needsRasterPaint(cs, el) {
    const hasFilter = !!(cs.filter && cs.filter !== 'none');
    const bg = cs.backgroundImage || '';
    const hasGradient = /gradient\(/i.test(bg);
    const bi = cs.borderImageSource || '';
    const hasBorderImage = !!bi && bi !== 'none' && /url\(/i.test(bi);
    const isSvg = !!(el && String(el.tagName).toUpperCase() === 'SVG');
    let hasPseudoChrome = false;
    if (el && !isSvg) {
      for (const pseudo of ['::before', '::after']) {
        let pcs;
        try { pcs = getComputedStyle(el, pseudo); } catch { continue; }
        const content = pcs.content;
        if (!content || content === 'none' || content === 'normal') continue;
        const pw = parseFloat(pcs.width) || 0;
        const ph = parseFloat(pcs.height) || 0;
        const borders = (parseFloat(pcs.borderTopWidth) || 0)
          + (parseFloat(pcs.borderRightWidth) || 0)
          + (parseFloat(pcs.borderBottomWidth) || 0)
          + (parseFloat(pcs.borderLeftWidth) || 0);
        const pbg = pcs.backgroundColor || '';
        const hasBg = pbg && pbg !== 'transparent' && !/^rgba\(\s*0\s*,\s*0\s*,\s*0\s*,\s*0\s*\)$/i.test(pbg);
        if (pw > 0 || ph > 0 || borders > 0 || hasBg) {
          hasPseudoChrome = true;
          break;
        }
      }
    }
    return {
      needsRaster: hasFilter || hasGradient || hasBorderImage || isSvg || hasPseudoChrome,
      rasterWholeSubtree: hasFilter || isSvg || hasPseudoChrome,
      // Transparent PNG for icons so sidebar/card chrome isn't baked into the sprite.
      rasterOmitBackground: isSvg || hasPseudoChrome,
    };
  }

  // Specified (authored) width/height — not computed px. Walks same-origin stylesheets
  // + inline style with a crude cascade (important > specificity > order). CORS sheets
  // are skipped (cssRules throws); dual-viewport inference still covers those.
  function specifiedProp(el, prop) {
    const inline = el.style.getPropertyValue(prop);
    if (inline) return inline.trim();

    function specificity(sel) {
      const s = String(sel || '');
      const a = (s.match(/#/g) || []).length;
      const b = (s.match(/\.|\[|:(?!:)/g) || []).length;
      const c = (s.match(/^[a-zA-Z]+|[^\w.#:[\]\s>+~]+/g) || []).length;
      return [a, b, c];
    }
    function cmpSpec(x, y) {
      for (let i = 0; i < 3; i++) if (x[i] !== y[i]) return x[i] - y[i];
      return 0;
    }

    let best = null;
    let order = 0;
    function consider(decl, sel) {
      const value = decl.getPropertyValue(prop);
      if (!value) return;
      const important = decl.getPropertyPriority(prop) === 'important';
      const cand = { value: value.trim(), spec: specificity(sel), order: order++, important };
      if (!best) { best = cand; return; }
      if (cand.important !== best.important) {
        if (cand.important) best = cand;
        return;
      }
      const s = cmpSpec(cand.spec, best.spec);
      if (s > 0 || (s === 0 && cand.order >= best.order)) best = cand;
    }
    function walkRules(rules) {
      for (const rule of rules) {
        if (rule.type === CSSRule.MEDIA_RULE || rule.type === CSSRule.SUPPORTS_RULE) {
          try { walkRules(rule.cssRules); } catch { /* ignore */ }
          continue;
        }
        if (!(rule instanceof CSSStyleRule)) continue;
        let match = false;
        try { match = el.matches(rule.selectorText); } catch { continue; }
        if (match) consider(rule.style, rule.selectorText);
      }
    }
    for (const sheet of document.styleSheets) {
      let rules;
      try { rules = sheet.cssRules; } catch { continue; }
      if (rules) walkRules(rules);
    }
    return best ? best.value : '';
  }

  function parseBorderImageSlice(cs) {
    // Computed border-image-slice e.g. "30" / "30 fill" / "10 20 30 40"
    const raw = (cs.borderImageSlice || '').replace(/\s+fill\s*/i, ' ').trim();
    if (!raw || raw === 'none') return 0;
    const n = parseFloat(raw.split(/\s+/)[0]);
    return Number.isFinite(n) ? n : 0;
  }

  function applyTextTransform(text, transform) {
    const t = (transform || 'none').toLowerCase();
    if (!text || t === 'none' || t === 'unset' || t === 'initial' || t === 'inherit') return text;
    if (t === 'uppercase') return text.toLocaleUpperCase();
    if (t === 'lowercase') return text.toLocaleLowerCase();
    if (t === 'capitalize') {
      // CSS capitalize: first letter of each whitespace-separated word.
      return text.replace(/(^|\s)(\S)/g, (_, sp, ch) => sp + ch.toLocaleUpperCase());
    }
    return text;
  }

  // Gum bitmap fonts (default ASCII + Latin-1) lack common Unicode punctuation that
  // pages use for typography (em/en dash, minus). Map to ASCII so glyphs render.
  function normalizeForBitmapFont(text) {
    if (!text) return text;
    return text
      .replace(/[\u2010-\u2015\u2212]/g, '-') // hyphen/dash variants → ASCII hyphen
      .replace(/[\u2018\u2019\u201A]/g, "'") // single quotes
      .replace(/[\u201C\u201D\u201E]/g, '"'); // double quotes
  }

  // Inline phrasing hosts (`<h1><strong>A</strong> B</h1>`): element-only walks drop
  // sibling #text nodes. Walk childNodes so each text run gets its own Absolute leaf.
  const PHRASING = new Set([
    'STRONG', 'B', 'EM', 'I', 'SPAN', 'A', 'SMALL', 'CODE', 'LABEL', 'ABBR',
    'TIME', 'MARK', 'U', 'S', 'SUB', 'SUP', 'SVG', 'IMG', 'BR', 'WBR',
  ]);

  function walkTextNode(textNode, parentEl) {
    const collapsed = (textNode.textContent || '').replace(/\s+/g, ' ');
    const trimmed = collapsed.trim();
    if (!trimmed) return null;
    // Keep a single leading/trailing space so `<strong>A</strong> B` doesn't become "AB".
    const lead = /^\s/.test(collapsed) ? ' ' : '';
    const trail = /\s$/.test(collapsed) ? ' ' : '';
    const range = document.createRange();
    range.selectNodeContents(textNode);
    const rect = range.getBoundingClientRect();
    if (rect.width <= 0 && rect.height <= 0) return null;
    const cs = getComputedStyle(parentEl);
    const ownText = normalizeForBitmapFont(
      applyTextTransform(lead + trimmed + trail, cs.textTransform),
    );
    if (!ownText.trim()) return null;
    let lineCount = 1;
    const rects = Array.from(range.getClientRects()).filter((r) => r.width > 0 && r.height > 0);
    if (rects.length > 0) lineCount = Math.max(1, rects.length);
    return {
      id: null,
      tag: '#text',
      rect: { x: rect.x, y: rect.y, width: rect.width, height: rect.height },
      text: ownText,
      lineCount,
      imgSrc: null,
      naturalWidth: 0,
      naturalHeight: 0,
      rasterSrc: null,
      style: {
        display: 'inline',
        backgroundImage: 'none',
        backgroundSize: cs.backgroundSize,
        objectFit: cs.objectFit,
        objectPosition: cs.objectPosition,
        listStyleType: 'none',
        flexDirection: cs.flexDirection,
        flexWrap: cs.flexWrap,
        rowGap: 0,
        columnGap: 0,
        flexGrow: 0,
        order: 0,
        alignItems: cs.alignItems,
        alignSelf: 'auto',
        justifyContent: cs.justifyContent,
        textAlign: cs.textAlign,
        paddingTop: 0,
        paddingRight: 0,
        paddingBottom: 0,
        paddingLeft: 0,
        marginTop: 0,
        marginRight: 0,
        marginBottom: 0,
        marginLeft: 0,
        zIndex: 0,
        gridTemplateColumns: 'none',
        gridTemplateRows: 'none',
        gridAutoFlow: cs.gridAutoFlow,
        gridColumnStart: 'auto',
        gridColumnEnd: 'auto',
        gridRowStart: 'auto',
        gridRowEnd: 'auto',
        gridColumnStartSpecified: '',
        gridColumnEndSpecified: '',
        gridRowStartSpecified: '',
        gridRowEndSpecified: '',
        gridAreaSpecified: '',
        gridColumnSpecified: '',
        gridRowSpecified: '',
        position: 'static',
        backgroundColor: 'rgba(0, 0, 0, 0)',
        borderTopLeftRadius: 0,
        borderTopWidth: 0,
        borderRightWidth: 0,
        borderBottomWidth: 0,
        borderLeftWidth: 0,
        borderTopColor: 'rgba(0, 0, 0, 0)',
        borderRightColor: 'rgba(0, 0, 0, 0)',
        borderBottomColor: 'rgba(0, 0, 0, 0)',
        borderLeftColor: 'rgba(0, 0, 0, 0)',
        boxShadow: 'none',
        textShadow: cs.textShadow || 'none',
        webkitTextStrokeWidth: parseFloat(cs.webkitTextStrokeWidth) || 0,
        overflow: 'visible',
        opacity: 1,
        filter: 'none',
        needsRaster: false,
        rasterWholeSubtree: false,
        color: cs.color,
        fontSize: parseFloat(cs.fontSize) || 0,
        fontWeight: cs.fontWeight,
        fontStyle: cs.fontStyle,
        fontFamily: cs.fontFamily,
        widthSpecified: '',
        heightSpecified: '',
        borderImageSource: 'none',
        borderImageSlice: 0,
        borderImageRepeat: '',
      },
      children: [],
    };
  }

  function walk(el) {
    const cs = getComputedStyle(el);
    const rect = el.getBoundingClientRect();

    const elementChildren = Array.from(el.children).filter(isVisible);
    const brOnly = elementChildren.length > 0
      && elementChildren.every((c) => c.tagName === 'BR' || c.tagName === 'WBR');
    const onlyPhrasing = elementChildren.length > 0
      && !brOnly
      && elementChildren.every((c) => PHRASING.has(String(c.tagName).toUpperCase()));
    // A node is "text" when it has no element children (or only br/wbr) but has visible text.
    // el.textContent is raw source text — it does NOT apply the browser's own
    // `white-space: normal` collapsing (runs of spaces/tabs/newlines -> one space),
    // which only happens at render time. Without collapsing here, HTML source
    // formatting (indentation, line-wrapped markup) leaks into the emitted string as
    // literal embedded newlines/gaps that were never visible on the actual page.
    // text-transform is also paint-time only — bake it into the string since Gum Text
    // has no text-transform equivalent.
    // <br>-only mixed content: use innerText so "Mira<br>HP" is not dropped.
    // Phrasing + sibling text (`<strong>A</strong> B`): walk childNodes so #text runs
    // are kept (element-only walks would drop them).
    let ownText = '';
    let walkChildren;
    if (brOnly) {
      walkChildren = [];
      ownText = (el.innerText || '')
        .replace(/\r\n/g, '\n')
        .split('\n')
        .map((line) => line.replace(/[ \t\f\v]+/g, ' ').trim())
        .join('\n')
        .replace(/^\n+|\n+$/g, '');
      if (ownText) {
        ownText = normalizeForBitmapFont(applyTextTransform(ownText, cs.textTransform));
      }
    } else if (onlyPhrasing) {
      walkChildren = [];
      for (const child of el.childNodes) {
        if (child.nodeType === Node.TEXT_NODE) {
          const leaf = walkTextNode(child, el);
          if (leaf) walkChildren.push(leaf);
        } else if (child.nodeType === Node.ELEMENT_NODE && isVisible(child)) {
          walkChildren.push(walk(child));
        }
      }
    } else if (elementChildren.length === 0) {
      walkChildren = [];
      ownText = el.textContent.replace(/\s+/g, ' ').trim();
      if (ownText) {
        ownText = normalizeForBitmapFont(applyTextTransform(ownText, cs.textTransform));
      }
    } else {
      walkChildren = elementChildren;
    }

    // How many lines Chromium actually wrapped this text into — ask Chromium directly
    // (via Range.getClientRects, one rect per rendered line) rather than inferring it
    // from height/line-height math, which can be wrong for unknown line-heights.
    let lineCount = 1;
    if (ownText && el.firstChild) {
      const range = document.createRange();
      range.selectNodeContents(el);
      const rects = Array.from(range.getClientRects()).filter((r) => r.width > 0 && r.height > 0);
      lineCount = Math.max(1, rects.length);
    }

    const paint = needsRasterPaint(cs, el);
    let box = { x: rect.x, y: rect.y, width: rect.width, height: rect.height };
    // ::before/::after bars (AdminKit hamburger) often paint outside a thin middle strip.
    if (paint.rasterOmitBackground && paint.needsRaster && el.tagName.toUpperCase() !== 'SVG') {
      const minW = 20;
      const minH = 18;
      if (box.height < minH) {
        const pad = (minH - box.height) / 2;
        box = { x: box.x, y: box.y - pad, width: box.width, height: minH };
      }
      if (box.width < minW) {
        const pad = (minW - box.width) / 2;
        box = { x: box.x - pad, y: box.y, width: minW, height: box.height };
      }
    }

    let kids = brOnly || onlyPhrasing || elementChildren.length === 0
      ? walkChildren
      : walkChildren.map(walk);
    // Icon chip (AdminKit `.stat`): colored/rounded host + sole SVG → one sprite.
    // Rasterizing only the SVG leaves an empty circle (or a mismatched glyph plate).
    const chip = kids.length === 1 && kids[0].tag === 'svg' && kids[0].style?.needsRaster;
    const bg = cs.backgroundColor || '';
    const bgOpaque = bg && bg !== 'transparent'
      && !/^rgba\(\s*0\s*,\s*0\s*,\s*0\s*,\s*0\s*\)$/i.test(bg);
    if (chip && (bgOpaque || (parseFloat(cs.borderTopLeftRadius) || 0) > 0) && !paint.needsRaster) {
      paint.needsRaster = true;
      paint.rasterWholeSubtree = true;
      paint.rasterOmitBackground = false;
      kids[0].style.needsRaster = false;
      kids[0].style.rasterWholeSubtree = false;
      kids[0].style.rasterOmitBackground = false;
      kids = [];
    }
    // Hamburger / CSS-icon host: sole child is a ::before/::after chrome leaf. Bake the
    // parent hit-target (e.g. .sidebar-toggle) so all three bars are in the screenshot;
    // the leaf's border-box is often only the middle 3px bar.
    const pseudoKid = kids.length === 1
      && kids[0].style?.needsRaster
      && kids[0].style?.rasterOmitBackground
      && kids[0].tag !== 'svg';
    if (pseudoKid && !paint.needsRaster) {
      paint.needsRaster = true;
      paint.rasterWholeSubtree = true;
      paint.rasterOmitBackground = false;
      kids[0].style.needsRaster = false;
      kids[0].style.rasterWholeSubtree = false;
      kids[0].style.rasterOmitBackground = false;
      kids = [];
    }

    return {
      id: el.id || null,
      tag: el.tagName.toLowerCase(),
      rect: box,
      text: ownText,
      lineCount,
      // el.currentSrc resolves srcset/responsive-image selection; falls back to el.src
      // for a plain <img>. Both are already-resolved absolute URLs, ready to download.
      imgSrc: el.tagName.toLowerCase() === 'img' ? (el.currentSrc || el.src || null) : null,
      // Native pixel size of the source image, needed to emulate object-fit:cover as a
      // Gum TextureAddress=Custom source-rect crop (see map.ts) — Gum's Sprite has no
      // object-fit concept of its own, it just stretches to fill like CSS's object-fit:
      // fill default. 0 for non-<img> nodes or an image that failed to load; bg-image
      // leaves are filled in by enrichNaturalSizes() below.
      naturalWidth: el.naturalWidth || 0,
      naturalHeight: el.naturalHeight || 0,
      // Set by convert.ts when this node was screenshotted as a raster sprite (§5.3).
      rasterSrc: null,
      style: {
        display: cs.display,
        backgroundImage: cs.backgroundImage,
        backgroundSize: cs.backgroundSize,
        objectFit: cs.objectFit,
        objectPosition: cs.objectPosition,
        listStyleType: cs.listStyleType,
        flexDirection: cs.flexDirection,
        flexWrap: cs.flexWrap,
        // computed `gap` resolves to "row-gap column-gap"; grab row-gap for a column stack.
        rowGap: parseFloat(cs.rowGap) || 0,
        columnGap: parseFloat(cs.columnGap) || 0,
        flexGrow: parseFloat(cs.flexGrow) || 0,
        order: parseInt(cs.order, 10) || 0,
        alignItems: cs.alignItems,
        alignSelf: cs.alignSelf,
        justifyContent: cs.justifyContent,
        textAlign: cs.textAlign,
        paddingTop: parseFloat(cs.paddingTop) || 0,
        paddingRight: parseFloat(cs.paddingRight) || 0,
        paddingBottom: parseFloat(cs.paddingBottom) || 0,
        paddingLeft: parseFloat(cs.paddingLeft) || 0,
        // Used px (margin:auto resolves to free space — critical for .ms-auto / navbar-align).
        marginTop: parseFloat(cs.marginTop) || 0,
        marginRight: parseFloat(cs.marginRight) || 0,
        marginBottom: parseFloat(cs.marginBottom) || 0,
        marginLeft: parseFloat(cs.marginLeft) || 0,
        zIndex: (() => {
          const z = cs.zIndex;
          if (z === 'auto' || z === '') return 0;
          const n = parseInt(z, 10);
          return Number.isFinite(n) ? n : 0;
        })(),
        gridTemplateColumns: cs.gridTemplateColumns,
        gridTemplateRows: cs.gridTemplateRows,
        gridAutoFlow: cs.gridAutoFlow,
        gridColumnStart: cs.gridColumnStart,
        gridColumnEnd: cs.gridColumnEnd,
        gridRowStart: cs.gridRowStart,
        gridRowEnd: cs.gridRowEnd,
        // Authored placement (not computed line numbers). Computed grid-*-start is often
        // a resolved integer even for auto-placed items — that would false-trigger Absolute.
        gridColumnStartSpecified: specifiedProp(el, 'grid-column-start'),
        gridColumnEndSpecified: specifiedProp(el, 'grid-column-end'),
        gridRowStartSpecified: specifiedProp(el, 'grid-row-start'),
        gridRowEndSpecified: specifiedProp(el, 'grid-row-end'),
        gridAreaSpecified: specifiedProp(el, 'grid-area'),
        gridColumnSpecified: specifiedProp(el, 'grid-column'),
        gridRowSpecified: specifiedProp(el, 'grid-row'),
        position: cs.position,
        backgroundColor: cs.backgroundColor,
        borderTopLeftRadius: parseFloat(cs.borderTopLeftRadius) || 0,
        borderTopWidth: parseFloat(cs.borderTopWidth) || 0,
        borderRightWidth: parseFloat(cs.borderRightWidth) || 0,
        borderBottomWidth: parseFloat(cs.borderBottomWidth) || 0,
        borderLeftWidth: parseFloat(cs.borderLeftWidth) || 0,
        borderTopColor: cs.borderTopColor,
        borderRightColor: cs.borderRightColor,
        borderBottomColor: cs.borderBottomColor,
        borderLeftColor: cs.borderLeftColor,
        boxShadow: cs.boxShadow,
        // Glyph outline / faux border (RPGUI: 4-way text-shadow). Mapped to Gum OutlineThickness.
        textShadow: cs.textShadow || 'none',
        webkitTextStrokeWidth: parseFloat(cs.webkitTextStrokeWidth) || 0,
        overflow: cs.overflow,
        opacity: parseFloat(cs.opacity),
        filter: cs.filter,
        ...paint,
        color: cs.color,
        fontSize: parseFloat(cs.fontSize) || 0,
        fontWeight: cs.fontWeight,
        fontStyle: cs.fontStyle,
        fontFamily: cs.fontFamily,
        // Specified sizes from cascade (inline + same-origin sheets), not computed px.
        widthSpecified: specifiedProp(el, 'width'),
        heightSpecified: specifiedProp(el, 'height'),
        // CSS border-image → NineSlice SourceFile + frame width (slice).
        borderImageSource: cs.borderImageSource || 'none',
        borderImageSlice: parseBorderImageSlice(cs),
        borderImageRepeat: cs.borderImageRepeat || '',
      },
      children: kids,
    };
  }

  async function enrichNaturalSizes(node) {
    const url = bgImageUrl(node.style.backgroundImage);
    if (url && !(node.naturalWidth > 0)) {
      await new Promise((resolve) => {
        const img = new Image();
        img.onload = () => {
          node.naturalWidth = img.naturalWidth || 0;
          node.naturalHeight = img.naturalHeight || 0;
          resolve();
        };
        img.onerror = () => resolve();
        img.src = url;
      });
    }
    for (const child of node.children) await enrichNaturalSizes(child);
  }

  const root = document.querySelector(rootSelector);
  if (!root) throw new Error('root selector not found: ' + rootSelector);
  const tree = walk(root);
  await enrichNaturalSizes(tree);
  return tree;
}
