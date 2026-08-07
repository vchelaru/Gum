// @ts-nocheck
// Same-origin link crawl for site-fidelity harness.
import { chromium } from 'playwright-core';
import { installTsxEvaluateShim } from '../converter/tsx-evaluate-shim.js';
import { probePageRejection } from './rejection.js';

/**
 * @typedef {{
 *   urls: string[],
 *   rejected: boolean,
 *   reason?: string,
 * }} CrawlResult
 */

/**
 * @param {string} seedUrl
 * @param {{
 *   maxPages?: number,
 *   pathPrefix?: string,
 *   viewport?: {width:number,height:number},
 *   gotoTimeoutMs?: number,
 *   waitUntil?: 'load'|'domcontentloaded'|'networkidle'|'commit',
 * }} [opts]
 * @returns {Promise<CrawlResult>}
 */
export async function crawlSite(seedUrl, opts = {}) {
  const maxPages = opts.maxPages ?? 25;
  const seed = new URL(seedUrl);
  const pathPrefix = opts.pathPrefix
    ?? seed.pathname.replace(/\/[^/]*$/, '/'); // directory of seed file
  const viewport = opts.viewport ?? { width: 800, height: 600 };
  // Discovery only — convert.ts still does a full networkidle render. Prefer a
  // short `load` so blocked/never-idle hosts abort in ~15s instead of 60s×N.
  const gotoTimeoutMs = opts.gotoTimeoutMs ?? 15_000;
  const waitUntil = opts.waitUntil ?? 'load';

  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport, deviceScaleFactor: 1 });
  await installTsxEvaluateShim(page);

  /** @type {string[]} */
  const ordered = [];
  const seen = new Set();
  /** @type {string[]} */
  const queue = [seed.href];
  let isSeed = true;

  function accept(href) {
    let u;
    try { u = new URL(href, seed); } catch { return null; }
    if (u.protocol !== 'http:' && u.protocol !== 'https:') return null;
    if (u.origin !== seed.origin) return null;
    if (!u.pathname.startsWith(pathPrefix)) return null;
    u.hash = '';
    // Normalize trailing index defaults lightly — keep distinct .htm paths.
    return u.href;
  }

  /** @type {CrawlResult} */
  const result = { urls: ordered, rejected: false };

  while (queue.length && ordered.length < maxPages) {
    const href = queue.shift();
    const canon = accept(href);
    if (!canon || seen.has(canon)) continue;
    seen.add(canon);

    try {
      const response = await page.goto(canon, { waitUntil, timeout: gotoTimeoutMs });
      await installTsxEvaluateShim(page);
      const status = response?.status() ?? null;

      const rejection = await probePageRejection(page, status);
      if (rejection.rejected) {
        result.rejected = true;
        result.reason = `${canon}: ${rejection.reason}`;
        console.warn(`crawl rejected ${result.reason}`);
        break;
      }

      // Classic frameset hubs (Space Jam *frames.html) have no <body> for convert —
      // enqueue each frame's src instead of treating the frameset as a page.
      const frameSrcs = await page.evaluate(() => {
        if (!/frameset/i.test(document.contentType || '')
          && !document.querySelector('frameset')) {
          return [];
        }
        return Array.from(document.querySelectorAll('frame[src], iframe[src]'))
          .map((el) => el.src)
          .filter(Boolean);
      });
      if (frameSrcs.length) {
        console.log(`frameset ${canon} → ${frameSrcs.length} frame src(s)`);
        for (const src of frameSrcs) {
          const next = accept(src);
          if (next && !seen.has(next)) queue.push(next);
        }
        isSeed = false;
        continue;
      }

      ordered.push(canon);

      const links = await page.evaluate(() =>
        Array.from(document.querySelectorAll('a[href]'))
          .map((a) => a.href)
          .filter(Boolean));
      for (const link of links) {
        const next = accept(link);
        if (next && !seen.has(next)) queue.push(next);
      }
      isSeed = false;
    } catch (e) {
      const msg = e?.message || String(e);
      console.warn(`crawl skip ${canon}: ${msg}`);
      // Seed (or first real page) navigation failure = host is blocked / dead.
      // Do not drain the rest of the queue burning the same timeout.
      if (isSeed || ordered.length === 0) {
        result.rejected = true;
        result.reason = `${canon}: ${msg.slice(0, 160)}`;
        break;
      }
    }
  }

  await browser.close();
  return result;
}

/** Derive a Gum-safe screen name from a page URL (leaf path segment). */
export function screenNameFromUrl(pageUrl) {
  const u = new URL(pageUrl);
  let path = u.pathname.replace(/\/+/g, '/');
  if (path.endsWith('/')) path += 'index';
  path = path.replace(/\.(html?|aspx?|php)$/i, '');
  const parts = path.split('/').filter(Boolean);
  const leaf = parts[parts.length - 1] || 'Page';
  let name = leaf.replace(/[^A-Za-z0-9_]+/g, '_').replace(/^_+|_+$/g, '');
  if (!/^[A-Za-z]/.test(name)) name = `Page_${name}`;
  name = name.split('_').map((p) => p.charAt(0).toUpperCase() + p.slice(1)).join('');
  return name.slice(0, 64) || 'Page';
}
