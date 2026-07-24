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
