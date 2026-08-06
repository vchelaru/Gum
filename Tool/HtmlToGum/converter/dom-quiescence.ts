// @ts-nocheck
/**
 * Waits until the DOM stops mutating (no MutationObserver callbacks for `quietMs`) or
 * `maxWaitMs` elapses, whichever comes first. `networkidle` only tracks network requests,
 * not client-side re-renders/hydration — a React/Vue-style page can still be actively
 * mutating its DOM (lazy-loaded sections settling, skeleton loaders being replaced) after
 * the network goes quiet. Extracting mid-mutation risks capturing a transient, structurally
 * inconsistent DOM (e.g. a section rendered twice for one frame during hydration) that the
 * converter would faithfully — but wrongly — carry through to the emitted Gum project.
 */
export async function waitForDomQuiescence(page, { quietMs = 400, maxWaitMs = 4000 } = {}) {
  await page.evaluate(({ quietMs, maxWaitMs }) => new Promise((resolve) => {
    const start = performance.now();
    let lastMutation = performance.now();
    const observer = new MutationObserver(() => { lastMutation = performance.now(); });
    observer.observe(document.documentElement, {
      childList: true, subtree: true, attributes: true, characterData: true,
    });
    const check = () => {
      const now = performance.now();
      if (now - lastMutation >= quietMs || now - start >= maxWaitMs) {
        observer.disconnect();
        resolve(undefined);
        return;
      }
      requestAnimationFrame(check);
    };
    requestAnimationFrame(check);
  }), { quietMs, maxWaitMs });
}

/**
 * Clears every currently pending timeout/interval so nothing can mutate the page again
 * after this point — auto-rotating hero carousels, blinking cursors, "N minutes ago"
 * refreshers, ad rotators.
 */
export async function freezeTimers(page) {
  await page.evaluate(() => {
    const maxTimeoutId = setTimeout(() => {}, 0);
    for (let id = 0; id <= maxTimeoutId; id++) {
      clearTimeout(id);
      clearInterval(id);
    }
    const maxRafId = requestAnimationFrame(() => {});
    for (let id = 0; id <= maxRafId; id++) {
      cancelAnimationFrame(id);
    }
    // Block anything that tries to schedule more work after the freeze.
    const noopTimer = () => 0;
    window.setTimeout = noopTimer;
    window.setInterval = noopTimer;
    window.requestAnimationFrame = noopTimer;
  });
}

/**
 * Pure heuristic used by stabilizeDynamicMedia (and unit-tested without Playwright).
 * @param {{ slideGroupSizes?: number[], hasIntervalHint?: boolean }} signals
 */
export function suspectRotatingMedia(signals = {}) {
  const sizes = signals.slideGroupSizes || [];
  if (sizes.some((n) => n >= 2)) return true;
  if (signals.hasIntervalHint) return true;
  return false;
}

/**
 * Lock the page into one paint for box-tree extract + later reference screenshots.
 * Auto-rotating heroes (Team Liquid `.newsitem` slideshow, CSS carousels, setInterval
 * rotators) otherwise swap between extract and chromium.png and produce huge spurious
 * diffs — not converter bugs. Agents must not spend probes on that race; this runs in
 * convert before extract.
 *
 * @returns {Promise<{ suspectedRotatingMedia: boolean, pinnedSlideGroups: number, pausedAnimations: number }>}
 */
export async function stabilizeDynamicMedia(page) {
  const meta = await page.evaluate(() => {
    const slideSelectors = [
      // Explicit mutually-exclusive / rotator patterns only — do not use bare `.slide`
      // (hits non-carousel layouts).
      { sel: '.newsitem', requireHiddenSibling: false },
      { sel: '.swiper-slide', requireHiddenSibling: false },
      { sel: '.carousel-item', requireHiddenSibling: false },
      { sel: '.carousel__slide', requireHiddenSibling: false },
      { sel: '.slider-item', requireHiddenSibling: false },
      { sel: '[data-slideshow] > *', requireHiddenSibling: true },
      { sel: '[class*="slideshow"] > *', requireHiddenSibling: true },
    ];
    /** @type {number[]} */
    const slideGroupSizes = [];
    let pinnedSlideGroups = 0;

    for (const { sel, requireHiddenSibling } of slideSelectors) {
      let nodes;
      try { nodes = Array.from(document.querySelectorAll(sel)); } catch { continue; }
      if (nodes.length < 2) continue;
      const visible = nodes.filter((el) => {
        const s = getComputedStyle(el);
        if (s.display === 'none' || s.visibility === 'hidden' || s.opacity === '0') return false;
        if (el.classList.contains('hidden') || el.classList.contains('hide')) return false;
        if (el.getAttribute('aria-hidden') === 'true') return false;
        return true;
      });
      const hidden = nodes.length - visible.length;
      if (requireHiddenSibling && hidden === 0) continue;
      if (visible.length === 0 && hidden < 2) continue;
      slideGroupSizes.push(nodes.length);

      let kept = false;
      for (const el of nodes) {
        const s = getComputedStyle(el);
        const isHidden = s.display === 'none'
          || s.visibility === 'hidden'
          || el.classList.contains('hidden')
          || el.classList.contains('hide')
          || el.getAttribute('aria-hidden') === 'true';
        if (!isHidden && !kept) {
          kept = true;
          continue;
        }
        el.style.setProperty('display', 'none', 'important');
        el.setAttribute('aria-hidden', 'true');
      }
      if (kept) pinnedSlideGroups++;
    }

    let pausedAnimations = 0;
    try {
      const anims = document.getAnimations?.({ subtree: true }) || [];
      for (const a of anims) {
        try { a.pause(); pausedAnimations++; } catch { /* ignore */ }
      }
    } catch { /* older Chromium */ }

    const style = document.createElement('style');
    style.setAttribute('data-html-to-gum-stabilize', '1');
    style.textContent = '*, *::before, *::after { animation-play-state: paused !important; transition: none !important; }';
    document.documentElement.appendChild(style);

    // Cookie / consent overlays shift every re-run and paint over real content — hide
    // fixed/sticky banners so fidelity measures the page, not the GDPR toast.
    let hiddenOverlays = 0;
    const overlaySel = [
      '[id*="cookie" i]',
      '[class*="cookie" i]',
      '[id*="consent" i]',
      '[class*="consent" i]',
      '#onetrust-banner-sdk',
      '#cookieok',
      '.cc-window',
      '[aria-label*="cookie" i]',
    ].join(',');
    const hideFixed = (el) => {
      let cur = el;
      while (cur && cur !== document.documentElement) {
        const s = getComputedStyle(cur);
        if (s.position === 'fixed' || s.position === 'sticky') {
          if (s.display !== 'none' && s.visibility !== 'hidden') {
            cur.style.setProperty('display', 'none', 'important');
            hiddenOverlays++;
          }
          return;
        }
        cur = cur.parentElement;
      }
    };
    try {
      for (const el of document.querySelectorAll(overlaySel)) hideFixed(el);
      // Nested fixed banners (OWASP #disclaimer-container under <header>) are not
      // body > * — walk every fixed/sticky node and match cookie-copy heuristics.
      for (const el of document.querySelectorAll('body *')) {
        const s = getComputedStyle(el);
        if (s.position !== 'fixed' && s.position !== 'sticky') continue;
        if (s.display === 'none' || s.visibility === 'hidden') continue;
        const t = (el.innerText || '').slice(0, 280);
        if (/(this website uses cookies|we use cookies|uses cookies to|cookie consent|accept cookies)/i.test(t)) {
          hideFixed(el);
        }
      }
    } catch { /* invalid selector on older engines */ }

    return {
      slideGroupSizes,
      pinnedSlideGroups,
      pausedAnimations,
      hiddenOverlays,
      hasIntervalHint: slideGroupSizes.length > 0,
    };
  });

  await freezeTimers(page);

  const suspectedRotatingMedia = suspectRotatingMedia({
    slideGroupSizes: meta.slideGroupSizes,
    hasIntervalHint: meta.hasIntervalHint || meta.pinnedSlideGroups > 0,
  });

  return {
    suspectedRotatingMedia,
    pinnedSlideGroups: meta.pinnedSlideGroups,
    pausedAnimations: meta.pausedAnimations,
    hiddenOverlays: meta.hiddenOverlays || 0,
  };
}
