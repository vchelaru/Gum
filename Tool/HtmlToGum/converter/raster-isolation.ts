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
 */
export function isolateElementForTransparentScreenshot({ rootSelector, path, mark }) {
  function isVisible(el) {
    const cs = getComputedStyle(el);
    return cs.opacity !== '0' && cs.display !== 'none' && cs.visibility !== 'hidden';
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
    ancestor.style.setProperty('color', 'transparent', 'important');
    ancestor.style.setProperty('-webkit-text-fill-color', 'transparent', 'important');

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
