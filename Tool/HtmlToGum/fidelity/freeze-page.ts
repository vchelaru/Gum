// @ts-nocheck
// Freeze a live URL into fidelity/frozen/<id>/ for deterministic canaries.
//
// Saves page HTML with an injected <base href> so relative assets still resolve
// against the origin when convert loads the local file. Not a full offline mirror.
//
// Usage:
//   npx tsx freeze-page.ts https://developer.mozilla.org/en-US/docs/Web/CSS/background-size --id=mdn-background-size
//   npm run freeze -- <url> --id=<slug>
import { chromium } from 'playwright-core';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { mkdirSync, writeFileSync, existsSync } from 'node:fs';
import { installTsxEvaluateShim } from '../converter/tsx-evaluate-shim.js';

const __dirname = dirname(fileURLToPath(import.meta.url));
const frozenRoot = join(__dirname, 'frozen');

function parseArgs(argv) {
  const positional = [];
  let id = null;
  let width = 800;
  let height = 900;
  let out = null;
  for (const a of argv) {
    if (a.startsWith('--id=')) id = a.slice(5);
    else if (a.startsWith('--width=')) width = parseInt(a.slice(8), 10);
    else if (a.startsWith('--height=')) height = parseInt(a.slice(9), 10);
    else if (a.startsWith('--out=')) out = a.slice(6);
    else if (a.startsWith('--')) {
      console.error(`Unknown flag: ${a}`);
      process.exit(2);
    } else positional.push(a);
  }
  return { url: positional[0], id, width, height, out };
}

function slugFromUrl(url) {
  const u = new URL(url);
  const path = u.pathname.replace(/\/+/g, '/').replace(/^\//, '').replace(/\/$/, '') || 'root';
  return `${u.hostname}-${path}`.replace(/[^a-zA-Z0-9._-]+/g, '-').slice(0, 60);
}

async function main() {
  const { url, id, width, height, out } = parseArgs(process.argv.slice(2));
  if (!url || !/^https?:\/\//i.test(url)) {
    console.error('Usage: npx tsx freeze-page.ts <https://url> [--id=slug] [--width=800] [--height=900]');
    process.exit(2);
  }

  const freezeId = id || slugFromUrl(url);
  const dir = out ? resolve(out) : join(frozenRoot, freezeId);
  mkdirSync(dir, { recursive: true });

  const browser = await chromium.launch();
  try {
    const page = await browser.newPage({ viewport: { width, height } });
    await installTsxEvaluateShim(page);
    console.log(`freezing ${url}`);
    await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 60_000 });
    // Extra settle for late CSS/fonts; freeze is best-effort, not pixel-identical forever.
    await new Promise((r) => setTimeout(r, 1500));

    const baseHref = await page.evaluate(() => {
      const u = new URL(location.href);
      // Directory of the current path so relative CSS/images resolve.
      const dirPath = u.pathname.replace(/\/[^/]*$/, '/') || '/';
      return `${u.origin}${dirPath}`;
    });

    let html = await page.content();
    // Ensure a single <base> points at the live origin so file:// convert still fetches assets.
    if (/<base\s/i.test(html)) {
      html = html.replace(/<base\s[^>]*>/i, `<base href="${baseHref}">`);
    } else if (/<head[^>]*>/i.test(html)) {
      html = html.replace(/<head([^>]*)>/i, `<head$1><base href="${baseHref}">`);
    } else {
      html = `<base href="${baseHref}">` + html;
    }

    const indexPath = join(dir, 'index.html');
    writeFileSync(indexPath, html, 'utf8');

    const shotPath = join(dir, 'chromium-reference.png');
    await page.screenshot({ path: shotPath, fullPage: false });

    const meta = {
      id: freezeId,
      sourceUrl: url,
      baseHref,
      frozenAt: new Date().toISOString(),
      viewport: { width, height },
      index: 'index.html',
      chromiumReference: 'chromium-reference.png',
      note: 'HTML snapshot with <base href> to origin. Re-freeze when the live page layout changes materially.',
    };
    writeFileSync(join(dir, 'meta.json'), JSON.stringify(meta, null, 2));

    console.log(`wrote ${indexPath}`);
    console.log(`wrote ${shotPath}`);
    console.log(`meta: ${join(dir, 'meta.json')}`);
  } finally {
    await browser.close();
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
