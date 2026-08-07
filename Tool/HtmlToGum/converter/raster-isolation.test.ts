// @ts-nocheck
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { chromium } from 'playwright-core';
import {
  isolateElementForTransparentScreenshot,
  restoreRasterIsolation,
} from './raster-isolation.js';
import { installTsxEvaluateShim } from './tsx-evaluate-shim.js';

test('transparent SVG screenshot excludes ancestor paint and restores DOM', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage({ viewport: { width: 200, height: 100 } });
    await installTsxEvaluateShim(page);
    await page.setContent(`
      <div id="hero" style="position:relative;width:100px;height:20px;background:rgb(200,0,0)">
        <div id="sibling" style="position:absolute;inset:0;background:rgb(0,0,200)"></div>
        <svg id="wave" width="100" height="20" style="position:relative;display:block">
          <rect x="0" y="0" width="50" height="20" fill="white"></rect>
        </svg>
      </div>
    `);
    const originalHeroStyle = await page.locator('#hero').getAttribute('style');
    const originalSiblingStyle = await page.locator('#sibling').getAttribute('style');

    const found = await page.evaluate(
      isolateElementForTransparentScreenshot,
      { rootSelector: '#hero', path: [1], mark: 'test-shot' },
    );
    assert.equal(found, true);

    const png = await page.locator('[data-html-to-gum-shot="test-shot"]').screenshot({
      omitBackground: true,
    });

    // Decode the captured PNG in Chromium so the test can inspect alpha without another
    // image dependency. The SVG's unpainted right half must stay transparent, not red/blue.
    const sample = await page.evaluate(async (base64) => {
      const img = new Image();
      img.src = `data:image/png;base64,${base64}`;
      await img.decode();
      const canvas = document.createElement('canvas');
      canvas.width = img.width;
      canvas.height = img.height;
      const ctx = canvas.getContext('2d');
      ctx.drawImage(img, 0, 0);
      return {
        painted: Array.from(ctx.getImageData(25, 10, 1, 1).data),
        transparent: Array.from(ctx.getImageData(75, 10, 1, 1).data),
      };
    }, png.toString('base64'));
    assert.deepEqual(sample.painted, [255, 255, 255, 255]);
    assert.equal(sample.transparent[3], 0);

    await page.evaluate(restoreRasterIsolation, 'test-shot');
    assert.equal(await page.locator('#hero').getAttribute('style'), originalHeroStyle);
    assert.equal(await page.locator('#sibling').getAttribute('style'), originalSiblingStyle);
    assert.equal(await page.locator('[data-html-to-gum-shot]').count(), 0);
  } finally {
    await browser.close();
  }
});

test('isolation path skips NOSCRIPT like extractBoxTree (catfishing body)', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage({ viewport: { width: 200, height: 100 } });
    await installTsxEvaluateShim(page);
    await page.setContent(`
      <body style="background:rgb(0,80,40);margin:0;color:rgb(167,243,208)">
        <noscript>enable js</noscript>
        <div id="app">
          <svg id="icon" width="24" height="24" style="display:block">
            <circle cx="12" cy="12" r="8" fill="currentColor"></circle>
          </svg>
        </div>
      </body>
    `);

    // extract skips NOSCRIPT → path [0,0] is #app → #icon. Isolation must match.
    const found = await page.evaluate(
      isolateElementForTransparentScreenshot,
      { rootSelector: 'body', path: [0, 0], mark: 'noscript-shot' },
    );
    assert.equal(found, true);
    assert.equal(
      await page.locator('[data-html-to-gum-shot="noscript-shot"]').evaluate((el) => el.id),
      'icon',
    );

    const png = await page.locator('[data-html-to-gum-shot="noscript-shot"]').screenshot({
      omitBackground: true,
    });
    const sample = await page.evaluate(async (base64) => {
      const img = new Image();
      img.src = `data:image/png;base64,${base64}`;
      await img.decode();
      const canvas = document.createElement('canvas');
      canvas.width = img.width;
      canvas.height = img.height;
      const ctx = canvas.getContext('2d');
      ctx.drawImage(img, 0, 0);
      return {
        center: Array.from(ctx.getImageData(12, 12, 1, 1).data),
        corner: Array.from(ctx.getImageData(0, 0, 1, 1).data),
      };
    }, png.toString('base64'));
    // currentColor mint survives (pinned on target); corner stays transparent (no body green).
    assert.equal(sample.corner[3], 0);
    assert.ok(sample.center[3] > 200);
    assert.ok(sample.center[1] > 200); // green channel of mint

    await page.evaluate(restoreRasterIsolation, 'noscript-shot');
  } finally {
    await browser.close();
  }
});

test('text-heavy isolation clears ancestor bg but keeps multi-color spans', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage({ viewport: { width: 400, height: 100 } });
    await installTsxEvaluateShim(page);
    await page.setContent(`
      <body style="margin:0;background:rgb(0,80,40);color:rgb(200,200,200)">
        <div style="background-image:linear-gradient(rgb(255,0,0),rgb(255,0,0));width:400px;height:80px">
          <li id="cell" style="list-style:none;margin:0;padding:8px;font:20px serif">
            <span style="color:rgb(0,255,200)">Teal</span>
            <span style="color:rgb(255,220,100)">Gold</span>
          </li>
        </div>
      </body>
    `);
    const found = await page.evaluate(
      isolateElementForTransparentScreenshot,
      { rootSelector: 'body', path: [0, 0], mark: 'text-shot', clearInheritedColor: false },
    );
    assert.equal(found, true);
    const png = await page.locator('[data-html-to-gum-shot="text-shot"]').screenshot({
      omitBackground: true,
    });
    const sample = await page.evaluate(async (base64) => {
      const img = new Image();
      img.src = `data:image/png;base64,${base64}`;
      await img.decode();
      const canvas = document.createElement('canvas');
      canvas.width = img.width;
      canvas.height = img.height;
      const ctx = canvas.getContext('2d');
      ctx.drawImage(img, 0, 0);
      const data = ctx.getImageData(0, 0, img.width, img.height).data;
      let teal = 0;
      let gold = 0;
      let redBg = 0;
      for (let i = 0; i < data.length; i += 4) {
        const r = data[i];
        const g = data[i + 1];
        const b = data[i + 2];
        const a = data[i + 3];
        if (a < 200) continue;
        if (g > 180 && b > 150 && r < 80) teal++;
        if (r > 200 && g > 180 && b < 140) gold++;
        if (r > 200 && g < 80 && b < 80) redBg++;
      }
      return { teal, gold, redBg, w: img.width, h: img.height };
    }, png.toString('base64'));
    assert.ok(sample.teal > 10, `expected teal glyphs, got ${JSON.stringify(sample)}`);
    assert.ok(sample.gold > 10, `expected gold glyphs, got ${JSON.stringify(sample)}`);
    assert.equal(sample.redBg, 0, `ancestor red must not bake in: ${JSON.stringify(sample)}`);
    await page.evaluate(restoreRasterIsolation, 'text-shot');
  } finally {
    await browser.close();
  }
});
