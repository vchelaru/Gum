// @ts-nocheck
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { chromium } from 'playwright-core';
import { waitForDomQuiescence } from './dom-quiescence.js';
import { installTsxEvaluateShim } from './tsx-evaluate-shim.js';

// Regression for a page.evaluate(extractBoxTree) capturing a mid-mutation DOM (e.g. a
// React/hydration-style page still settling lazy-loaded sections after `networkidle`
// fires, which only tracks network requests, not client-side re-renders). Reproduced on
// geeksforgeeks.org: a live fetch occasionally captured the sticky header duplicated in
// the DOM, which a second, later fetch of the same URL did not — consistent with catching
// the page mid-render rather than a real, stable structural bug.
test('waitForDomQuiescence: resolves only after mutations actually stop', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent('<div id="root"></div>');
    await page.evaluate(() => {
      const root = document.getElementById('root');
      let n = 0;
      const id = setInterval(() => {
        root.appendChild(document.createElement('span'));
        n++;
        if (n >= 6) clearInterval(id); // stops mutating after ~300ms (6 * 50ms)
      }, 50);
    });

    const start = Date.now();
    await waitForDomQuiescence(page, { quietMs: 100, maxWaitMs: 2000 });
    const elapsed = Date.now() - start;

    const spanCount = await page.evaluate(() => document.querySelectorAll('span').length);
    await page.close();

    assert.equal(spanCount, 6, 'all mutations should have completed before quiescence resolved');
    // Mutations stop at ~300ms; quietMs=100 means it shouldn't resolve before ~300ms, and
    // shouldn't take anywhere near the 2000ms cap.
    assert.ok(elapsed >= 300, `resolved too early (${elapsed}ms) — did not actually wait for mutations to stop`);
    assert.ok(elapsed < 1500, `resolved too late (${elapsed}ms) — should settle shortly after the last mutation, not hit the cap`);
  } finally {
    await browser.close();
  }
});

test('waitForDomQuiescence: bounded by maxWaitMs when mutations never stop', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent('<div id="root"></div>');
    await page.evaluate(() => {
      const root = document.getElementById('root');
      setInterval(() => { root.appendChild(document.createElement('span')); }, 30);
    });

    const start = Date.now();
    await waitForDomQuiescence(page, { quietMs: 100, maxWaitMs: 500 });
    const elapsed = Date.now() - start;
    await page.close();

    assert.ok(elapsed >= 480 && elapsed < 1200, `expected to bail out near the 500ms cap, took ${elapsed}ms`);
  } finally {
    await browser.close();
  }
});
