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
