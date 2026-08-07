// @ts-nocheck
// Helpers passed directly to Playwright page.evaluate. Keep each function self-contained:
// Playwright serializes the function body without module-scope bindings.

/**
 * Isolate one element for an omitBackground screenshot.
 *
 * Playwright's omitBackground only clears the page canvas; painted DOM ancestors still
 * show through transparent pixels. For an inline SVG over a photo hero, that bakes the
 * photo into the SVG sprite and Gum paints it twice (Pi-hole's bottom separator).
 *
 * Neutralize ancestor chrome and hide sibling branches while preserving the target branch.
 * Every touched element's exact inline style is restored by restoreRasterIsolation.
 *
 * Path indices must match extractBoxTree: skip SCRIPT/STYLE/NOSCRIPT/… — Chromium can
 * report NOSCRIPT as display≠none, so a body child index of 0 would hit NOSCRIPT while
 * extract's path[0] is the first real DIV (catfishing.net nav SVG rasters fell back to
 * opaque page clips and stamped green squares over the labels).
 *
 * @param clearInheritedColor when true (SVG default), pin target color then force
 *   ancestors transparent so currentColor fills survive without ancestor glyphs showing
 *   through. When false (text-heavy cells), only clear backgrounds — pinning
 *   -webkit-text-fill / ancestor color:transparent flattens descendant span colors.
 */
export function isolateElementForTransparentScreenshot({
  rootSelector, path, mark, clearInheritedColor = true,
}) {
  const SKIP_TAGS = new Set([
    'SCRIPT', 'STYLE', 'NOSCRIPT', 'TEMPLATE', 'HEAD', 'META', 'LINK', 'TITLE',
  ]);
  function isVisible(el) {
    if (!el || el.nodeType !== Node.ELEMENT_NODE) return false;
    if (SKIP_TAGS.has(String(el.tagName).toUpperCase())) return false;
    const cs = getComputedStyle(el);
    if (cs.opacity === '0' || cs.display === 'none' || cs.visibility === 'hidden') return false;
    if (String(el.tagName).toUpperCase() === 'IFRAME') {
      const r = el.getBoundingClientRect();
      if (r.width < 1 && r.height < 1) return false;
    }
    return true;
  }
  function remember(el, role) {
    el.setAttribute('data-html-to-gum-isolation', `${mark}:${role}`);
    el.setAttribute(
      'data-html-to-gum-isolation-style',
      el.hasAttribute('style') ? el.getAttribute('style') : '__HTMLTOGUM_NO_STYLE__',
    );
  }

  let el = document.querySelector(rootSelector);
  if (!el) return false;
  for (const idx of path) {
    const kids = Array.from(el.children).filter(isVisible);
    el = kids[idx];
    if (!el) return false;
  }
  el.setAttribute('data-html-to-gum-shot', mark);

  if (clearInheritedColor) {
    // Pin inherited color on the target before ancestors go transparent — currentColor SVG
    // fills would otherwise disappear with the parent. Only `color` (not webkitTextFillColor).
    const cs = getComputedStyle(el);
    remember(el, 'target');
    el.style.setProperty('color', cs.color, 'important');
  }

  let branch = el;
  let ancestor = el.parentElement;
  while (ancestor) {
    // Sibling branches may overlap or sit behind the transparent target.
    for (const sibling of ancestor.children) {
      if (sibling === branch) continue;
      remember(sibling, 'sibling');
      sibling.style.setProperty('visibility', 'hidden', 'important');
    }

    // Preserve inherited opacity/filter on the target, but remove ancestor paint that
    // would otherwise be composited into transparent target pixels.
    remember(ancestor, 'ancestor');
    ancestor.style.setProperty('background-color', 'transparent', 'important');
    ancestor.style.setProperty('background-image', 'none', 'important');
    ancestor.style.setProperty('border-color', 'transparent', 'important');
    ancestor.style.setProperty('box-shadow', 'none', 'important');
    ancestor.style.setProperty('text-shadow', 'none', 'important');
    if (clearInheritedColor) {
      ancestor.style.setProperty('color', 'transparent', 'important');
      ancestor.style.setProperty('-webkit-text-fill-color', 'transparent', 'important');
    }

    branch = ancestor;
    ancestor = ancestor.parentElement;
  }
  return true;
}

/** Restore every element touched by isolateElementForTransparentScreenshot. */
export function restoreRasterIsolation(mark) {
  const nodes = document.querySelectorAll('[data-html-to-gum-isolation]');
  for (const el of nodes) {
    const token = el.getAttribute('data-html-to-gum-isolation') || '';
    if (!token.startsWith(`${mark}:`)) continue;
    const saved = el.getAttribute('data-html-to-gum-isolation-style');
    if (saved === '__HTMLTOGUM_NO_STYLE__') el.removeAttribute('style');
    else if (saved != null) el.setAttribute('style', saved);
    el.removeAttribute('data-html-to-gum-isolation');
    el.removeAttribute('data-html-to-gum-isolation-style');
  }
  document.querySelector(`[data-html-to-gum-shot="${mark}"]`)
    ?.removeAttribute('data-html-to-gum-shot');
}
