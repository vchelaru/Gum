// @ts-nocheck
// One-off: fetch a static snapshot of geeksforgeeks.org's home page so the
// header-duplication repro (see samples/repro/) stops being a moving target
// (live site content/layout changes between fetches). Scrolls through the
// page first so lazy-loaded cards are present in the captured DOM.
//
// Usage: npx tsx snapshot-gfg.ts
import { chromium } from 'playwright-core';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { mkdirSync, writeFileSync } from 'node:fs';
import { installTsxEvaluateShim } from './tsx-evaluate-shim.js';

const __dirname = dirname(fileURLToPath(import.meta.url));
const outPath = join(__dirname, '..', 'samples', 'repro', 'gfg-snapshot-full.html');

async function main() {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
  await installTsxEvaluateShim(page);
  await page.goto('https://www.geeksforgeeks.org/', { waitUntil: 'networkidle' });
  await installTsxEvaluateShim(page);

  // Scroll through the whole page so lazy-loaded/idle-loaded cards render
  // before we snapshot — otherwise we might pin a partially-loaded DOM.
  await page.evaluate(async () => {
    const step = 800;
    const delay = (ms) => new Promise((r) => setTimeout(r, ms));
    let last = -1;
    while (true) {
      window.scrollBy(0, step);
      await delay(300);
      const y = window.scrollY;
      if (y === last) break;
      last = y;
    }
    window.scrollTo(0, 0);
  });
  await page.waitForLoadState('networkidle');
  await page.evaluate(() => document.fonts.ready);

  const html = await page.content();
  mkdirSync(dirname(outPath), { recursive: true });
  writeFileSync(outPath, html);
  console.log(`Wrote ${outPath} (${html.length} bytes)`);

  await browser.close();
}

main().catch((e) => { console.error(e); process.exit(1); });
