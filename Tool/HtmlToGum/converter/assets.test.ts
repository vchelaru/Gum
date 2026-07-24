// @ts-nocheck
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { chromium } from 'playwright-core';
import { svgIntrinsicSize, rasterizeSvg } from './assets.js';
import { installTsxEvaluateShim } from './tsx-evaluate-shim.js';

test('svgIntrinsicSize: width/height attrs', () => {
  assert.deepEqual(svgIntrinsicSize('<svg width="24" height="32"></svg>'), { width: 24, height: 32 });
});

test('svgIntrinsicSize: falls back to viewBox when width/height missing', () => {
  assert.deepEqual(svgIntrinsicSize('<svg viewBox="0 0 100 50"></svg>'), { width: 100, height: 50 });
});

test('svgIntrinsicSize: falls back to a default when neither is present', () => {
  assert.deepEqual(svgIntrinsicSize('<svg></svg>'), { width: 128, height: 128 });
});

test('rasterizeSvg: fills the full canvas for an SVG without a viewBox', async () => {
  // Reproduces the IANA logo bug (PIL.Image.open() can't read raw SVG XML — it's vector,
  // not raster, so it needs a real rendering engine) plus a real edge case found while
  // fixing it: an SVG with no viewBox doesn't rescale via CSS width/height — it stays
  // pinned to its native top-left region, cropping everything past its original size.
  const svg = '<svg xmlns="http://www.w3.org/2000/svg" width="40" height="40">'
    + '<rect width="40" height="40" fill="#ff0000"/></svg>';
  const browser = await chromium.launch();
  try {
    const png = await rasterizeSvg(browser, Buffer.from(svg));
    assert.ok(png.length > 0, 'expected non-empty PNG bytes');

    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`<img id="i" style="width:80px;height:80px" src="data:image/png;base64,${png.toString('base64')}">`);
    const pixels = await page.evaluate(async () => {
      const img = document.getElementById('i');
      await img.decode();
      const c = document.createElement('canvas');
      c.width = 80; c.height = 80;
      const ctx = c.getContext('2d');
      ctx.drawImage(img, 0, 0, 80, 80);
      const sample = (x, y) => Array.from(ctx.getImageData(x, y, 1, 1).data);
      return { corner: sample(2, 2), center: sample(40, 40), farCorner: sample(78, 78) };
    });
    await page.close();
    assert.deepEqual(pixels.corner, [255, 0, 0, 255]);
    assert.deepEqual(pixels.center, [255, 0, 0, 255]);
    assert.deepEqual(pixels.farCorner, [255, 0, 0, 255]);
  } finally {
    await browser.close();
  }
});
