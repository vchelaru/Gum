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
