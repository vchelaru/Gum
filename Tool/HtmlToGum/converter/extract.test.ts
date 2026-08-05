// @ts-nocheck
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { chromium } from 'playwright-core';
import { extractBoxTree } from './extract.js';
import { installTsxEvaluateShim } from './tsx-evaluate-shim.js';

// Regression for the IANA "As described in RFC 2606 and RFC 6761, a number of..." bug:
// a #text node that continues on the same line as a preceding <a>, then itself wraps
// onto further lines, was collapsed into one leaf positioned at the union bounding box
// of all its lines — which starts back at the block's left margin, overlapping the <a>
// siblings that precede it on line 1.
test('extractBoxTree: a #text run that wraps across lines is split per rendered line', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`
      <div style="width:320px;font:16px/22px Arial;">
        <p>As described in <a href="#">RFC 2606</a> and <a href="#">RFC 6761</a>, a number of domains such as example.com and example.org are maintained for documentation purposes.</p>
      </div>
    `);
    const tree = await page.evaluate(extractBoxTree, 'p');
    await page.close();

    const textLeaves = tree.children.filter((c) => c.tag === '#text');
    // The trailing run must be split into more than one leaf (one per wrapped line).
    assert.ok(textLeaves.length > 2, `expected multiple split #text leaves, got ${textLeaves.length}`);

    // No leaf may start left of the paragraph's own left edge, and none may start at
    // the same (x, y) as the first "As described in " leaf — that exact overlap was
    // the visible bug.
    const first = textLeaves[0];
    for (const leaf of textLeaves.slice(1)) {
      assert.ok(
        !(leaf.rect.x === first.rect.x && leaf.rect.y === first.rect.y),
        `leaf "${leaf.text}" overlaps the first leaf's origin (${first.rect.x},${first.rect.y})`,
      );
    }

    // Reconstructing all children's text (in order) should reproduce the paragraph,
    // modulo whitespace collapsing.
    const rebuilt = tree.children.map((c) => c.text).join('').replace(/\s+/g, ' ').trim();
    const expected = 'As described in RFC 2606 and RFC 6761, a number of domains such as example.com '
      + 'and example.org are maintained for documentation purposes.';
    assert.equal(rebuilt, expected);
  } finally {
    await browser.close();
  }
});

// <input type="submit|button|reset"> labels live in the value attribute, not textContent
// (KORE Sign In: empty text → orange rect with no label).
test('extractBoxTree: submit/button input uses value as text', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`
      <form>
        <input id="go" type="submit" value="Sign In"
          style="width:120px;height:32px;background:#f58320;color:#fff;border:0;font:14px Arial;">
      </form>
    `);
    const tree = await page.evaluate(extractBoxTree, '#go');
    await page.close();
    assert.equal(tree.tag, 'input');
    assert.equal(tree.text, 'Sign In');
    assert.equal(tree.form?.role, 'submit');
    assert.equal(tree.form?.value, 'Sign In');
  } finally {
    await browser.close();
  }
});

test('extractBoxTree: text input does not treat empty value as a label', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`<input id="email" type="email" value="" placeholder="Email" style="width:200px;height:32px;">`);
    const tree = await page.evaluate(extractBoxTree, '#email');
    await page.close();
    assert.equal(tree.text || '', '');
    assert.equal(tree.form?.role, 'textbox');
    assert.equal(tree.form?.placeholder, 'Email');
    assert.equal(tree.form?.inputType, 'email');
  } finally {
    await browser.close();
  }
});

test('extractBoxTree: password / checkbox / select form metadata', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`
      <form id="f">
        <input id="pw" type="password" placeholder="Password" value="secret" style="width:100px;height:24px;">
        <input id="cb" type="checkbox" checked style="width:16px;height:16px;">
        <select id="sel" style="width:100px;height:24px;">
          <option>One</option>
          <option selected>Two</option>
        </select>
      </form>
    `);
    const tree = await page.evaluate(extractBoxTree, '#f');
    await page.close();
    const byId = (id) => {
      const stack = [tree];
      while (stack.length) {
        const n = stack.pop();
        if (n.id === id) return n;
        for (const c of n.children || []) stack.push(c);
      }
      return null;
    };
    assert.equal(byId('pw')?.form?.role, 'password');
    assert.equal(byId('pw')?.form?.value, 'secret');
    assert.equal(byId('cb')?.form?.role, 'checkbox');
    assert.equal(byId('cb')?.form?.checked, true);
    assert.equal(byId('sel')?.form?.role, 'combobox');
    assert.equal(byId('sel')?.form?.value, 'Two');
  } finally {
    await browser.close();
  }
});
