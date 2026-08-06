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
  // Also skip non-rendered host tags whose textContent would otherwise leak (Space Jam
  // sitemap: <noscript><iframe src="googletagmanager…"></iframe></noscript> showed up
  // as a literal string at the top of the Gum screenshot).
  const SKIP_TAGS = new Set([
    'SCRIPT', 'STYLE', 'NOSCRIPT', 'TEMPLATE', 'HEAD', 'META', 'LINK', 'TITLE',
  ]);
  function isVisible(el) {
    if (!el || el.nodeType !== Node.ELEMENT_NODE) return false;
    if (SKIP_TAGS.has(String(el.tagName).toUpperCase())) return false;
    const cs = getComputedStyle(el);
    if (cs.opacity === '0' || cs.display === 'none' || cs.visibility === 'hidden') return false;
    // Zero-size tracking iframes / pixels (GTM) — not painted, but may not be display:none.
    if (String(el.tagName).toUpperCase() === 'IFRAME') {
      const r = el.getBoundingClientRect();
      if (r.width < 1 && r.height < 1) return false;
    }
    return true;
  }

  // Chromium paints HTML presentational table borders (`border="1"` / `border=""`) as a
  // fixed grey bevel — light #eee top/left on the table (outset), dark #9a on cells
  // (inset). getComputedStyle still reports border-*-color as currentColor (e.g. Space
  // Jam yellow), so without this rewrite Gum strokes yellow and diffs ~10% on sitemap.
  const HTML_TABLE_BEVEL_LIGHT = 'rgb(238, 238, 238)';
  const HTML_TABLE_BEVEL_DARK = 'rgb(154, 154, 154)';

  function htmlTableBorderAttrWidth(table) {
    if (!table || !table.hasAttribute('border')) return 0;
    const raw = table.getAttribute('border');
    if (raw === '0') return 0;
    const n = parseInt(raw, 10);
    // Empty border="" is treated as 1 (legacy HTML / Space Jam sitemap).
    if (raw === '' || !Number.isFinite(n) || n < 0) return 1;
    return n;
  }

  function authoredBorderColor(el) {
    if (!el || !el.style) return false;
    const s = el.style;
    return !!(s.borderColor || s.borderTopColor || s.borderRightColor
      || s.borderBottomColor || s.borderLeftColor);
  }

  /** Replace lying currentColor table borders with Chromium's painted grey bevel. */
  function applyHtmlTableBorderPaint(el, style) {
    const tag = String(el.tagName).toUpperCase();
    if (tag !== 'TABLE' && tag !== 'TD' && tag !== 'TH') return style;
    const table = tag === 'TABLE' ? el : el.closest('table');
    if (htmlTableBorderAttrWidth(table) <= 0) return style;
    if (authoredBorderColor(el) || authoredBorderColor(table)) return style;
    const hasBorder = (style.borderTopWidth || 0) > 0 || (style.borderRightWidth || 0) > 0
      || (style.borderBottomWidth || 0) > 0 || (style.borderLeftWidth || 0) > 0;
    if (!hasBorder) return style;
    const isTable = tag === 'TABLE';
    const tl = isTable ? HTML_TABLE_BEVEL_LIGHT : HTML_TABLE_BEVEL_DARK;
    const br = isTable ? HTML_TABLE_BEVEL_DARK : HTML_TABLE_BEVEL_LIGHT;
    return {
      ...style,
      borderTopColor: tl,
      borderLeftColor: tl,
      borderBottomColor: br,
      borderRightColor: br,
    };
  }

  // BitmapFont metrics + missing text-decoration:underline leave multi-line table prose
  // (Space Jam sitemap) well above a 5% pixel budget even after BBCode merges. Baking the
  // cell through Chromium captures borders, underlines, and glyph raster in one sprite.
  // Mirrors forms.ts formControlFromDom — must be inlined here because this whole
  // function is serialized into the page via page.evaluate (no module imports).
  function formControlFromDom(el) {
    const tag = String(el.tagName || '').toUpperCase();
    const TEXT_INPUT_TYPES = new Set([
      'text', 'email', 'search', 'tel', 'url', 'number', 'date', 'datetime-local',
      'month', 'week', 'time', '',
    ]);
    if (tag === 'INPUT') {
      const inputType = String(el.type || 'text').toLowerCase();
      const placeholder = String(el.placeholder || '');
      const value = String(el.value || '');
      const checked = !!el.checked;
      const disabled = !!el.disabled;
      if (inputType === 'hidden' || inputType === 'file' || inputType === 'image'
        || inputType === 'range' || inputType === 'color' || inputType === 'reset') {
        return null;
      }
      if (inputType === 'password') {
        return { role: 'password', inputType, placeholder, value, checked, disabled };
      }
      if (inputType === 'checkbox') {
        return { role: 'checkbox', inputType, placeholder, value, checked, disabled };
      }
      if (inputType === 'radio') {
        return { role: 'radio', inputType, placeholder, value, checked, disabled };
      }
      if (inputType === 'submit' || inputType === 'button') {
        return {
          role: inputType === 'submit' ? 'submit' : 'button',
          inputType,
          placeholder,
          value: value || (inputType === 'submit' ? 'Submit' : 'Button'),
          checked,
          disabled,
        };
      }
      if (TEXT_INPUT_TYPES.has(inputType)) {
        return {
          role: 'textbox',
          inputType: inputType || 'text',
          placeholder,
          value,
          checked,
          disabled,
        };
      }
      return null;
    }
    if (tag === 'TEXTAREA') {
      return {
        role: 'textbox',
        inputType: 'textarea',
        placeholder: String(el.placeholder || ''),
        value: String(el.value || ''),
        checked: false,
        disabled: !!el.disabled,
      };
    }
    if (tag === 'BUTTON') {
      const type = String(el.type || 'submit').toLowerCase();
      const label = String(el.innerText || el.textContent || '').replace(/\s+/g, ' ').trim()
        || (type === 'submit' ? 'Submit' : 'Button');
      return {
        role: type === 'submit' ? 'submit' : 'button',
        inputType: type,
        placeholder: '',
        value: label,
        checked: false,
        disabled: !!el.disabled,
      };
    }
    if (tag === 'SELECT') {
      const options = Array.from(el.options || []).map((o) => String(o.text || o.value || '').trim());
      const selected = el.selectedOptions && el.selectedOptions[0];
      const value = selected
        ? String(selected.text || selected.value || '').trim()
        : (options[0] || '');
      return {
        role: 'combobox',
        inputType: 'select',
        placeholder: '',
        value,
        checked: false,
        disabled: !!el.disabled,
        options,
      };
    }
    return null;
  }

  function isPreformattedWhiteSpace(whiteSpace) {
    const ws = String(whiteSpace || '').toLowerCase();
    return ws === 'pre' || ws === 'pre-wrap' || ws === 'pre-line' || ws === 'break-spaces';
  }

  // textContent is raw source; collapse only when CSS white-space says so. Without this,
  // <pre><code> newlines become spaces and Gum soft-wraps at the wrong places
  // (tabsoverspaces code blocks).
  function textForWhiteSpace(raw, whiteSpace) {
    let t = String(raw || '').replace(/\r\n/g, '\n').replace(/\r/g, '\n');
    const ws = String(whiteSpace || 'normal').toLowerCase();
    if (ws === 'pre' || ws === 'pre-wrap' || ws === 'break-spaces') {
      // Drop a single leading/trailing newline that HTML source formatting injects;
      // keep interior newlines and runs of spaces/tabs.
      return t.replace(/^\n/, '').replace(/\n$/, '');
    }
    if (ws === 'pre-line') {
      return t
        .replace(/[ \t\f\v]+/g, ' ')
        .replace(/^\n+|\n+$/g, '')
        .split('\n')
        .map((line) => line.replace(/ +$/, ''))
        .join('\n')
        .trim();
    }
    return t.replace(/\s+/g, ' ').trim();
  }

  function shouldRasterTextHeavyCell(el) {
    const tag = String(el.tagName).toUpperCase();
    const text = (el.innerText || '').replace(/\s+/g, ' ').trim();
    // Space Jam sitemap: multi-line table prose with underlines BitmapFont can't match.
    if (tag === 'TD' || tag === 'TH') {
      if (text.length < 40) return false;
      const imgs = el.querySelectorAll('img');
      // Icon / logo cells with a short alt caption stay structured Sprites.
      if (imgs.length > 0 && text.length < 80) return false;
      return true;
    }
    // Multi-line preformatted blocks: extract used to collapse newlines to spaces, and
    // Gum Text soft-wrap ≠ Chromium pre layout (tabsoverspaces <pre><code>). Bake the
    // host so indentation and line breaks match Chromium paint.
    // Only <pre> or leaf/phrasing-only hosts — not large layout wrappers that inherit
    // white-space:pre (pastebin's per-line / full-paste containers).
    {
      const csPre = getComputedStyle(el);
      const preWs = isPreformattedWhiteSpace(csPre.whiteSpace);
      if (tag === 'PRE' || preWs) {
        let leafLike = tag === 'PRE';
        if (!leafLike && preWs) {
          const kids = Array.from(el.children).filter(isVisible);
          leafLike = kids.length === 0
            || kids.every((c) => {
              const t = String(c.tagName).toUpperCase();
              return PHRASING.has(t) || t === 'BR' || t === 'WBR';
            });
        }
        if (leafLike) {
          const range = document.createRange();
          range.selectNodeContents(el);
          const rects = Array.from(range.getClientRects()).filter((r) => r.width > 0 && r.height > 0);
          // getClientRects returns one box per inline fragment — many spans on one
          // visual line (pastebin highlighter) must not count as multi-line.
          const lineYs = [];
          for (const r of rects) {
            if (!lineYs.some((y) => Math.abs(y - r.y) < 1)) lineYs.push(r.y);
          }
          const nonemptyLines = String(el.innerText || '')
            .replace(/\r/g, '')
            .split('\n')
            .filter((l) => l.length > 0);
          if (lineYs.length >= 2 || nonemptyLines.length >= 2) return true;
        }
      }
    }
    // Custom web-font multi-line blocks (Pocket Graphik/Doyle): KernSmith metrics wrap
    // differently than Chromium, so every line is a pixel miss even when glyphs look fine.
    // Headings may wrap with <60 chars at large sizes — use a lower floor for H*.
    // Narrow wrapping links (TL Community News) use Arial but still drift on wrap —
    // include <a>/<li> when the line box is clearly multi-line.
    if (!/^(P|H1|H2|H3|H4|H5|H6|LI|A)$/.test(tag)) return false;
    const minChars = /^H[1-6]$/.test(tag) ? 16 : 40;
    if (text.length < minChars) return false;
    const cs = getComputedStyle(el);
    const first = String(cs.fontFamily || '')
      .split(',')[0]
      .replace(/["']/g, '')
      .trim()
      .toLowerCase();
    if (!first) return false;
    const systemFace = /^(arial|helvetica|helvetica neue|times|times new roman|courier|courier new|verdana|georgia|tahoma|segoe ui|consolas|menlo|monaco|sans-serif|serif|monospace|system-ui)$/;
    const range = document.createRange();
    range.selectNodeContents(el);
    const rects = Array.from(range.getClientRects()).filter((r) => r.width > 0 && r.height > 0);
    if (rects.length < 2) return false;
    // System faces: bake narrow wrapping links (sidebar news) and centered multi-line
    // marketing copy, whose wrap/centering amplifies small BitmapFont metric drift.
    // Leave wide left-aligned article prose structured (HN / Wikipedia stay Text).
    if (systemFace.test(first)) {
      const box = el.getBoundingClientRect();
      const centeredProse = tag === 'P' && cs.textAlign === 'center';
      if (box.width > 280 && !centeredProse) return false;
      if (tag !== 'A' && tag !== 'LI' && !centeredProse) return false;
    }
    return true;
  }

  // Large <img> drawn far from its natural size: Gum Sprite stretch-resample ≠ Chromium's
  // filter (Embrace hero 1792→720 alone can cost ~6% of the pixel gate even when aspect
  // matches). Bake Chromium's painted pixels for big on-screen figures; leave near-native
  // and small icons as structured Sprites.
  function shouldRasterScaledImage(el) {
    if (String(el.tagName).toUpperCase() !== 'IMG') return false;
    const nw = el.naturalWidth || 0;
    const nh = el.naturalHeight || 0;
    if (nw < 2 || nh < 2) return false;
    const box = el.getBoundingClientRect();
    if (box.width < 40 || box.height < 40) return false;
    if (box.width * box.height < 80_000) return false; // ~283² — skip icons/thumbs
    const sx = box.width / nw;
    const sy = box.height / nh;
    const scale = Math.min(sx, sy);
    if (scale >= 0.9 && scale <= 1.1) return false;
    return true;
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
    let hasPseudoBackdrop = false;
    if (el && !isSvg) {
      for (const pseudo of ['::before', '::after']) {
        let pcs;
        try { pcs = getComputedStyle(el, pseudo); } catch { continue; }
        const content = pcs.content;
        if (!content || content === 'none' || content === 'normal') continue;
        // CSS returns quoted strings: "\"\\f003\"" / '"•"' / '""'
        const raw = String(content).replace(/^["']|["']$/g, '');
        const pw = parseFloat(pcs.width) || 0;
        const ph = parseFloat(pcs.height) || 0;
        const borders = (parseFloat(pcs.borderTopWidth) || 0)
          + (parseFloat(pcs.borderRightWidth) || 0)
          + (parseFloat(pcs.borderBottomWidth) || 0)
          + (parseFloat(pcs.borderLeftWidth) || 0);
        const pbg = pcs.backgroundColor || '';
        const hasBg = pbg && pbg !== 'transparent' && !/^rgba\(\s*0\s*,\s*0\s*,\s*0\s*,\s*0\s*\)$/i.test(pbg);
        const hasBgImage = !!pcs.backgroundImage && pcs.backgroundImage !== 'none';
        const hasShadow = !!pcs.boxShadow && pcs.boxShadow !== 'none';
        // Font Awesome / Material Icons use ::before glyphs with width/height:auto
        // (parseFloat → 0) and no box chrome — previously skipped → empty bordered hosts.
        const fam = (pcs.fontFamily || '').toLowerCase();
        const isIconFont = /font\s*awesome|fontawesome|material icons|glyphicons?|ionicons|bootstrap-icons|feather|lucide/i.test(fam);
        let isPua = false;
        try {
          const cp = raw && raw.codePointAt(0);
          isPua = cp != null && cp >= 0xe000 && cp <= 0xf8ff;
        } catch { /* ignore */ }
        if (pw > 0 || ph > 0 || borders > 0 || hasBg || hasBgImage || hasShadow || isIconFont || isPua) {
          hasPseudoChrome = true;
          // Empty-content pseudos commonly paint a full-box tint/texture over a host
          // background (Pi-hole hero color overlay). Bake host chrome only and keep
          // structured children; icon/glyph pseudos still bake the whole host.
          hasPseudoBackdrop = !raw && (hasBg || hasBgImage || hasShadow);
          break;
        }
      }
    }
    return {
      needsRaster: hasFilter || hasGradient || hasBorderImage || isSvg || hasPseudoChrome,
      rasterWholeSubtree: hasFilter || isSvg || (hasPseudoChrome && !hasPseudoBackdrop),
      // Transparent PNG for icons so sidebar/card chrome isn't baked into the sprite.
      rasterOmitBackground: isSvg || (hasPseudoChrome && !hasPseudoBackdrop),
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
    const lineRects = Array.from(range.getClientRects()).filter((r) => r.width > 0 && r.height > 0);

    function makeLeaf(text, r) {
      return {
        id: null,
        tag: '#text',
        rect: { x: r.x, y: r.y, width: r.width, height: r.height },
        text,
        lineCount: 1,
        imgSrc: null,
        naturalWidth: 0,
        naturalHeight: 0,
        rasterSrc: null,
        style: leafStyle(),
        children: [],
      };
    }

    // A run that itself wraps across multiple rendered lines (e.g. a long sentence
    // that continues after a sibling <a> on line 1, then wraps under it) can't be
    // represented by one Absolute leaf: Range.getBoundingClientRect() unions every
    // line into a single box whose left edge is the *leftmost* line — wrapped lines
    // restart at the block's margin — not the actual start of this run on line 1.
    // Placing a leaf there overlaps whatever inline content precedes it. Split into
    // one leaf per rendered line instead, using caretRangeFromPoint to find where
    // Chromium actually broke the line.
    if (lineRects.length > 1) {
      const boundaries = [0];
      let ok = true;
      for (let i = 1; i < lineRects.length; i++) {
        const r = lineRects[i];
        const caret = document.caretRangeFromPoint(r.left + 1, r.top + r.height / 2);
        if (caret && caret.startContainer === textNode && caret.startOffset > boundaries[i - 1]) {
          boundaries.push(caret.startOffset);
        } else {
          ok = false;
          break;
        }
      }
      if (ok) {
        boundaries.push(textNode.textContent.length);
        const leaves = [];
        for (let i = 0; i < lineRects.length; i++) {
          const raw = textNode.textContent.slice(boundaries[i], boundaries[i + 1]);
          // Collapse runs of whitespace to one space, but only trim at the very start/end
          // of the whole text node — a space at an internal line boundary is the wrap
          // point itself and reconstructs the original word-joined text.
          let lineText = raw.replace(/\s+/g, ' ');
          if (i === 0) lineText = lineText.replace(/^ /, '');
          if (i === lineRects.length - 1) lineText = lineText.replace(/ $/, '');
          if (!lineText) continue;
          if (i === 0 && lead) lineText = lead + lineText;
          if (i === lineRects.length - 1 && trail) lineText += trail;
          lineText = normalizeForBitmapFont(applyTextTransform(lineText, cs.textTransform));
          if (lineText.trim()) leaves.push(makeLeaf(lineText, lineRects[i]));
        }
        if (leaves.length > 0) return leaves;
      }
    }

    return [makeLeaf(ownText, rect)];

    function leafStyle() {
      return {
        display: 'inline',
        backgroundImage: 'none',
        backgroundSize: cs.backgroundSize,
        backgroundPosition: cs.backgroundPosition || '0% 0%',
        backgroundRepeat: 'no-repeat',
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
      };
    }
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
          const leaves = walkTextNode(child, el);
          if (leaves) walkChildren.push(...leaves);
        } else if (child.nodeType === Node.ELEMENT_NODE && isVisible(child)) {
          walkChildren.push(walk(child));
        }
      }
    } else if (elementChildren.length === 0) {
      walkChildren = [];
      const tag = String(el.tagName).toUpperCase();
      // <input type="submit|button|reset"> labels are in .value, not textContent
      // (Chromium paints value; textContent is empty — KORE "Sign In").
      if (tag === 'INPUT') {
        const type = String(el.type || 'text').toLowerCase();
        if (type === 'submit' || type === 'button' || type === 'reset') {
          ownText = String(el.value || '').replace(/\s+/g, ' ').trim();
        }
      } else {
        ownText = textForWhiteSpace(el.textContent, cs.whiteSpace);
      }
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

    if (shouldRasterTextHeavyCell(el) && !paint.needsRaster) {
      paint.needsRaster = true;
      paint.rasterWholeSubtree = true;
      paint.rasterOmitBackground = false;
      kids = [];
      ownText = '';
      lineCount = 1;
    }
    if (shouldRasterScaledImage(el) && !paint.needsRaster) {
      paint.needsRaster = true;
      paint.rasterWholeSubtree = true;
      // Capture Chromium's already-resampled paint (omitBackground false) so the sprite
      // matches the reference screenshot's filter, not Gum's stretch of the full PNG.
      paint.rasterOmitBackground = false;
    }

    return {
      id: el.id || null,
      tag: el.tagName.toLowerCase(),
      rect: box,
      text: ownText,
      lineCount,
      form: formControlFromDom(el),
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
      style: applyHtmlTableBorderPaint(el, {
        display: cs.display,
        backgroundImage: cs.backgroundImage,
        backgroundSize: cs.backgroundSize,
        // CSS sprite-sheet icons (background-image + background-position offset, e.g.
        // GeeksforGeeks' social icon strip) select one sub-region of a shared image —
        // see computeBackgroundSpriteCrop() in map.ts.
        backgroundPosition: cs.backgroundPosition,
        // Space Jam-style starfields use the CSS default (`repeat`) — without this the
        // mapper stretches one tile to fill (EntireTexture). See wantsTiledBackground().
        backgroundRepeat: cs.backgroundRepeat || 'repeat',
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
      }),
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

  // Canvas background: browsers paint the page canvas white when neither <html> nor <body>
  // sets an opaque background (CSS "canvas" default). Gum has no such default, so a page
  // that relies on it renders with a transparent root — the screenshot is transparent where
  // Chromium is white, which the pixel diff scores as a full miss (OWASP: whole content band).
  // Propagate the effective page background onto the root so BodyBg paints a backmost fill.
  const rootTag = String(root.tagName).toUpperCase();
  if ((rootTag === 'BODY' || rootTag === 'HTML') && tree && tree.style) {
    const isTransp = (c) => !c || c === 'transparent'
      || /^rgba\(\s*0\s*,\s*0\s*,\s*0\s*,\s*0\s*\)$/i.test(c);
    const noRootBgImage = !tree.style.backgroundImage || tree.style.backgroundImage === 'none';
    if (isTransp(tree.style.backgroundColor) && noRootBgImage) {
      let pageBg = getComputedStyle(document.documentElement).backgroundColor;
      if (isTransp(pageBg)) pageBg = getComputedStyle(document.body).backgroundColor;
      if (isTransp(pageBg)) pageBg = 'rgb(255, 255, 255)';
      tree.style.backgroundColor = pageBg;
    }
  }

  await enrichNaturalSizes(tree);
  return tree;
}
