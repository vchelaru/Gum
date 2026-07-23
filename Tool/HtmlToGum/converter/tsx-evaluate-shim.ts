/**
 * tsx/esbuild keepNames injects `__name(fn, "…")` into compiled output.
 * Playwright serializes functions via `.toString()` into the page, so the browser
 * must define `__name` (identity) or evaluate throws ReferenceError.
 *
 * Uses string sources so installing the shim does not itself require `__name`.
 */
const SHIM_SOURCE = 'globalThis.__name=globalThis.__name||function(fn){return fn};';

export async function installTsxEvaluateShim(page: {
  addInitScript: (script: { content: string } | string | (() => void)) => Promise<void>;
  evaluate: (pageFunction: string | (() => unknown)) => Promise<unknown>;
}): Promise<void> {
  await page.addInitScript({ content: SHIM_SOURCE });
  await page.evaluate(SHIM_SOURCE);
}
