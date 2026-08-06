// @ts-nocheck
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { chromium } from 'playwright-core';
import { waitForDomQuiescence, freezeTimers, stabilizeDynamicMedia, suspectRotatingMedia } from './dom-quiescence.js';
import { installTsxEvaluateShim } from './tsx-evaluate-shim.js';

test('suspectRotatingMedia: slide groups', () => {
  assert.equal(suspectRotatingMedia({ slideGroupSizes: [5] }), true);
  assert.equal(suspectRotatingMedia({ slideGroupSizes: [1] }), false);
  assert.equal(suspectRotatingMedia({}), false);
});

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

// Regression for a spurious pixel diff on Team Liquid's front page: its auto-rotating
// hero banner (setInterval) swaps to a new slide between box tree extraction and the
// (necessarily later) reference screenshot, so the two captures disagree on which slide
// is showing even though nothing about the converter's own output is wrong.
test('freezeTimers: stops a pending setInterval from firing again', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent('<div id="counter">0</div>');
    await page.evaluate(() => {
      window.tickCount = 0;
      setInterval(() => {
        window.tickCount++;
        document.getElementById('counter').textContent = String(window.tickCount);
      }, 30);
    });

    await page.waitForFunction(() => window.tickCount >= 2);
    await freezeTimers(page);
    const countAtFreeze = await page.evaluate(() => window.tickCount);
    await page.waitForTimeout(200); // several more intervals would have fired by now
    const countAfterWait = await page.evaluate(() => window.tickCount);
    await page.close();

    assert.equal(countAfterWait, countAtFreeze, 'interval kept firing after freezeTimers');
  } finally {
    await browser.close();
  }
});

// Team Liquid-style mutually exclusive hero slides must pin to one slide before extract.
test('stabilizeDynamicMedia: pins .newsitem slides and blocks new intervals', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`
      <div class="newsitem"><img class="mainimage" src="a.jpg" width="10" height="10"></div>
      <div class="newsitem hidden" style="display:none"><img class="mainimage" src="b.jpg" width="10" height="10"></div>
      <div class="newsitem hidden" style="display:none"><img class="mainimage" src="c.jpg" width="10" height="10"></div>
    `);
    await page.evaluate(() => {
      window.rotates = 0;
      const items = [...document.querySelectorAll('.newsitem')];
      let i = 0;
      setInterval(() => {
        window.rotates++;
        items.forEach((el, idx) => {
          el.style.display = idx === i ? '' : 'none';
          el.classList.toggle('hidden', idx !== i);
        });
        i = (i + 1) % items.length;
      }, 40);
    });
    await page.waitForFunction(() => window.rotates >= 2);
    const meta = await stabilizeDynamicMedia(page);
    assert.equal(meta.suspectedRotatingMedia, true);
    assert.ok(meta.pinnedSlideGroups >= 1);
    const rotatesAtPin = await page.evaluate(() => window.rotates);
    await page.waitForTimeout(200);
    const rotatesAfter = await page.evaluate(() => window.rotates);
    assert.equal(rotatesAfter, rotatesAtPin, 'carousel interval kept firing after stabilize');
    const visible = await page.evaluate(() =>
      [...document.querySelectorAll('.newsitem')].filter((el) => getComputedStyle(el).display !== 'none').length);
    assert.equal(visible, 1, 'expected exactly one visible newsitem after pin');
    await page.close();
  } finally {
    await browser.close();
  }
});

// OWASP nests #disclaimer-container (fixed) under <header>, so body>* heuristics miss it.
test('stabilizeDynamicMedia: hides nested fixed cookie disclaimer under header', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`
      <header style="height:40px;background:#abc;">
        <nav>Home</nav>
        <div id="disclaimer-container" style="position:fixed;left:0;right:0;bottom:0;height:80px;background:rgba(20,20,20,0.8);color:#fff;">
          <p>This website uses cookies to analyze our traffic and only share that information with our analytics partners.</p>
          <a href="#">Accept</a>
        </div>
      </header>
      <main><p>content</p></main>
    `);
    const meta = await stabilizeDynamicMedia(page);
    assert.ok(meta.hiddenOverlays >= 1, `expected to hide nested cookie banner, got ${meta.hiddenOverlays}`);
    const visible = await page.evaluate(() => {
      const el = document.getElementById('disclaimer-container');
      return el && getComputedStyle(el).display !== 'none';
    });
    assert.equal(visible, false, 'nested fixed cookie banner should be display:none');
    await page.close();
  } finally {
    await browser.close();
  }
});
