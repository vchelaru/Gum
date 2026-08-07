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
test('extractBoxTree: hidden menu labels are not leaf text (web.dev language selector)', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`
      <div id="lang" style="width:140px;height:36px;overflow:hidden;font:14px Arial;">
        <button type="button" style="display:none">Language</button>
        <ul style="display:none">
          <li><a href="/">English</a></li>
          <li><a href="/?hl=de">Deutsch</a></li>
          <li><a href="/?hl=es">Español</a></li>
        </ul>
      </div>
    `);
    const tree = await page.evaluate(extractBoxTree, '#lang');
    await page.close();
    assert.equal(tree.tag, 'div');
    assert.equal((tree.text || '').includes('Deutsch'), false);
    assert.equal((tree.text || '').includes('Español'), false);
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

// Font Awesome / icon-font ::before glyphs use width/height:auto (no box chrome) — still
// must rasterize the host or Gum emits an empty bordered square.
test('extractBoxTree: multi-line custom-font paragraph needsRaster (Pocket Graphik)', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`
      <style>
        @font-face {
          font-family: "Graphik Web";
          src: local("Arial");
        }
        p {
          font-family: "Graphik Web", sans-serif;
          font-size: 16px;
          width: 280px;
          line-height: 1.4;
        }
      </style>
      <p id="body">After careful consideration, we've made the difficult decision to phase out Pocket - our read-it-later and content discovery app across platforms.</p>
    `);
    const tree = await page.evaluate(extractBoxTree, '#body');
    await page.close();
    assert.equal(tree.tag, 'p');
    assert.equal(tree.style?.needsRaster, true);
    assert.equal((tree.text || ''), '');
  } finally {
    await browser.close();
  }
});

test('extractBoxTree: narrow wrapping Arial link needsRaster (TL Community News)', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`
      <a id="news" href="#" style="display:block;font:12px/1.2 Arial;width:200px;">
        Weekly Cups (July 20-26): Early returns on 5.0.16b balance patch notes
      </a>
    `);
    const tree = await page.evaluate(extractBoxTree, '#news');
    await page.close();
    assert.equal(tree.tag, 'a');
    assert.equal(tree.style?.needsRaster, true);
  } finally {
    await browser.close();
  }
});

test('extractBoxTree: centered multi-line system-font paragraph needsRaster', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`
      <p id="copy" style="width:546px;text-align:center;font:18px/1.5 Helvetica,Arial,sans-serif;">
        You can run Pi-hole in a container, or deploy it directly to a supported operating
        system via our automated installer.
      </p>
    `);
    const tree = await page.evaluate(extractBoxTree, '#copy');
    await page.close();
    assert.equal(tree.style?.needsRaster, true);
    assert.equal(tree.style?.rasterWholeSubtree, true);
  } finally {
    await browser.close();
  }
});

test('extractBoxTree: wide left-aligned system-font prose stays structured', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`
      <p id="article" style="width:546px;text-align:left;font:18px/1.5 Arial,sans-serif;">
        This wide article paragraph intentionally wraps across lines but remains structured
        text so documentation and news pages do not become a collection of raster blocks.
      </p>
    `);
    const tree = await page.evaluate(extractBoxTree, '#article');
    await page.close();
    assert.equal(tree.style?.needsRaster, false);
    assert.equal(tree.text.includes('wide article paragraph'), true);
  } finally {
    await browser.close();
  }
});

test('extractBoxTree: empty ::before background rasterizes host chrome, not children', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`
      <style>
        #hero {
          position: relative;
          width: 800px;
          height: 300px;
          background: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='8' height='8'%3E%3Crect width='8' height='8' fill='red'/%3E%3C/svg%3E") center/cover;
        }
        #hero::before {
          content: "";
          position: absolute;
          inset: 0;
          background: rgb(15, 1, 0);
          opacity: .61;
        }
      </style>
      <div id="hero"><h1>Structured heading</h1></div>
    `);
    const tree = await page.evaluate(extractBoxTree, '#hero');
    await page.close();
    assert.equal(tree.style?.needsRaster, true);
    assert.equal(tree.style?.rasterWholeSubtree, false);
    assert.equal(tree.style?.rasterOmitBackground, false);
    assert.equal(tree.children.some((child) => child.tag === 'h1'), true);
  } finally {
    await browser.close();
  }
});

// Browsers paint a white "canvas" when neither <html> nor <body> sets an opaque background.
// Gum has no such default, so the root must inherit that white or the screenshot is
// transparent where Chromium is white (OWASP: whole content band scored as a full miss).
test('extractBoxTree: transparent body inherits white canvas background', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`<body style="margin:0;"><header style="background:rgb(152,175,199);height:40px;">h</header><p>content</p></body>`);
    const tree = await page.evaluate(extractBoxTree, 'body');
    await page.close();
    assert.equal(tree.tag, 'body');
    assert.equal(tree.style?.backgroundColor, 'rgb(255, 255, 255)');
  } finally {
    await browser.close();
  }
});

test('extractBoxTree: explicit body background is not overridden with white', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`<body style="margin:0;background:rgb(10,20,30);"><p>x</p></body>`);
    const tree = await page.evaluate(extractBoxTree, 'body');
    await page.close();
    assert.equal(tree.style?.backgroundColor, 'rgb(10, 20, 30)');
  } finally {
    await browser.close();
  }
});

test('extractBoxTree: transparent body inherits html canvas color, not white', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`<html style="background:rgb(30,30,30);"><body style="margin:0;"><p>x</p></body></html>`);
    const tree = await page.evaluate(extractBoxTree, 'body');
    await page.close();
    assert.equal(tree.style?.backgroundColor, 'rgb(30, 30, 30)');
  } finally {
    await browser.close();
  }
});

// Multi-line <pre><code>: whitespace collapse turned newlines into spaces and Gum soft-
// wrapped mid-token (tabsoverspaces). Bake the pre host so Chromium line breaks stick.
test('extractBoxTree: multi-line pre/code is rasterized whole', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`
      <pre id="block" style="margin:0;padding:8px;background:#f4f4f4;font:14px Consolas,monospace;"><code>public class C
{
    public Foo A { get; } = new Foo();
    public Foo B => new Foo();
}
</code></pre>
    `);
    const tree = await page.evaluate(extractBoxTree, '#block');
    await page.close();
    assert.equal(tree.tag, 'pre');
    assert.equal(tree.style?.needsRaster, true);
    assert.equal(tree.style?.rasterWholeSubtree, true);
    assert.equal(tree.children?.length ?? 0, 0);
    assert.equal(tree.text || '', '');
  } finally {
    await browser.close();
  }
});

test('extractBoxTree: preformatted leaf keeps newlines when not rasterized', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    // Single visual line — no raster — but spaces must not collapse away.
    await page.setContent(`
      <code id="one" style="white-space:pre;font:14px Consolas,monospace;">a  b</code>
    `);
    const tree = await page.evaluate(extractBoxTree, '#one');
    await page.close();
    assert.equal(tree.tag, 'code');
    assert.equal(tree.style?.needsRaster, false);
    assert.equal(tree.text, 'a  b');
  } finally {
    await browser.close();
  }
});

// Large downscaled <img>: Gum stretch-resample ≠ Chromium (Embrace hero). Bake paint.
test('extractBoxTree: large downscaled img is rasterized', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`<img id="hero" alt="">`);
    await page.evaluate(() => {
      const c = document.createElement('canvas');
      c.width = 1600;
      c.height = 900;
      const ctx = c.getContext('2d');
      ctx.fillStyle = '#336699';
      ctx.fillRect(0, 0, 1600, 900);
      ctx.fillStyle = '#ffcc00';
      ctx.fillRect(100, 100, 400, 200);
      const img = document.getElementById('hero');
      img.src = c.toDataURL('image/png');
      img.style.width = '800px';
      img.style.height = '450px';
    });
    await page.waitForFunction(() => {
      const img = document.getElementById('hero');
      return img.complete && img.naturalWidth === 1600;
    });
    const tree = await page.evaluate(extractBoxTree, '#hero');
    await page.close();
    assert.equal(tree.tag, 'img');
    assert.equal(tree.naturalWidth, 1600);
    assert.equal(tree.style?.needsRaster, true);
    assert.equal(tree.style?.rasterWholeSubtree, true);
  } finally {
    await browser.close();
  }
});

test('extractBoxTree: near-native img stays structured Sprite', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`<img id="pic" width="200" height="150" alt="">`);
    await page.evaluate(() => {
      const c = document.createElement('canvas');
      c.width = 200;
      c.height = 150;
      const ctx = c.getContext('2d');
      ctx.fillStyle = '#123';
      ctx.fillRect(0, 0, 200, 150);
      const img = document.getElementById('pic');
      img.src = c.toDataURL('image/png');
      img.style.width = '200px';
      img.style.height = '150px';
    });
    await page.waitForFunction(() => {
      const img = document.getElementById('pic');
      return img.complete && img.naturalWidth === 200;
    });
    const tree = await page.evaluate(extractBoxTree, '#pic');
    await page.close();
    assert.equal(tree.tag, 'img');
    assert.equal(tree.style?.needsRaster, false);
  } finally {
    await browser.close();
  }
});

test('extractBoxTree: CSS mask-image icons needRaster (crates.io UnoCSS/Iconify)', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    const mask = "url(\"data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><circle cx='12' cy='12' r='10'/></svg>\")";
    await page.setContent(`
      <span id="icon" style="
        display:inline-block;width:22px;height:22px;
        background-color:#fff;
        -webkit-mask-image:${mask};
        mask-image:${mask};
        -webkit-mask-size:100% 100%;
        mask-size:100% 100%;
      "></span>
    `);
    const tree = await page.evaluate(extractBoxTree, '#icon');
    await page.close();
    assert.equal(tree.style?.needsRaster, true);
    assert.equal(tree.style?.rasterOmitBackground, true);
  } finally {
    await browser.close();
  }
});

test('extractBoxTree: sr-only text is skipped (crates.io theme / search)', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`
      <style>
        .sr-only {
          position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px;
          overflow: hidden; clip: rect(0,0,0,0); white-space: nowrap; border: 0;
        }
      </style>
      <button id="btn" style="width:48px;height:35px;position:relative;">
        <span class="sr-only">Search</span>
        <span id="glyph" style="display:inline-block;width:16px;height:16px;background:#fff;"></span>
      </button>
    `);
    const tree = await page.evaluate(extractBoxTree, '#btn');
    await page.close();
    const texts = [];
    (function walk(n) {
      if (n.text) texts.push(n.text);
      for (const c of n.children || []) walk(c);
    })(tree);
    assert.equal(texts.some((t) => /Search/i.test(t)), false);
    assert.equal((tree.children || []).length, 1);
  } finally {
    await browser.close();
  }
});

test('extractBoxTree: text sibling next to picture is kept (crates.io brand)', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`
      <a id="brand" href="/" style="display:flex;align-items:center;gap:8px;font:20px Arial;color:#fff;background:#083;padding:8px;">
        <picture style="display:contents">
          <img src="data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7" width="20" height="20" alt="">
        </picture>
        crates.io
      </a>
    `);
    const tree = await page.evaluate(extractBoxTree, '#brand');
    await page.close();
    const texts = [];
    (function walk(n) {
      if (n.text) texts.push(n.text);
      for (const c of n.children || []) walk(c);
    })(tree);
    assert.equal(texts.some((t) => /crates\.io/i.test(t)), true);
    // picture[display:contents] must be flattened so img is a direct flex child
    assert.equal((tree.children || []).some((c) => c.tag === 'picture'), false);
    assert.equal((tree.children || []).some((c) => c.tag === 'img'), true);
  } finally {
    await browser.close();
  }
});

test('extractBoxTree: ::after glyph content needsRaster (crates.io menu caret)', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`
      <style>
        .arrow::after { content: "▼"; color: #fff; font-size: 8px; }
      </style>
      <span id="arrow" class="arrow" style="display:inline-block;width:8px;height:9px;"></span>
    `);
    const tree = await page.evaluate(extractBoxTree, '#arrow');
    await page.close();
    assert.equal(tree.style?.needsRaster, true);
  } finally {
    await browser.close();
  }
});

test('extractBoxTree: huge border-radius clamps to half min side (pill input)', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`
      <input id="q" type="text" placeholder="Search"
        style="width:200px;height:40px;border:0;border-radius:5000px;background:#fff;">
    `);
    const tree = await page.evaluate(extractBoxTree, '#q');
    await page.close();
    assert.ok(tree.style.borderTopLeftRadius <= 20 + 0.5);
    assert.ok(tree.style.borderTopLeftRadius >= 19);
    assert.equal(tree.style?.needsRaster, true);
  } finally {
    await browser.close();
  }
});

test('extractBoxTree: large single-line custom-font H1 needsRaster (crates.io title)', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`
      <style>
        @font-face { font-family: 'FakeFira'; src: local('Arial'); }
      </style>
      <h1 id="t" style="width:600px;font:700 30px FakeFira,sans-serif;color:#fff;margin:0;">
        The Rust community's crate registry
      </h1>
    `);
    const tree = await page.evaluate(extractBoxTree, '#t');
    await page.close();
    assert.equal(tree.style?.needsRaster, true);
  } finally {
    await browser.close();
  }
});

test('extractBoxTree: multi-line custom-font DIV prose needsRaster (crates.io hero)', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`
      <style>
        @font-face { font-family: 'FakeFira'; src: local('Arial'); }
      </style>
      <div id="hero" style="width:280px;font:16px FakeFira,sans-serif;color:#333;">
        Instantly publish your crates and install them. Use the API to interact and find
        out more information about available crates. Become a contributor.
      </div>
    `);
    const tree = await page.evaluate(extractBoxTree, '#hero');
    await page.close();
    assert.equal(tree.style?.needsRaster, true);
  } finally {
    await browser.close();
  }
});

test('extractBoxTree: custom-font data TABLE needsRaster (kernel.org Oxygen)', async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await installTsxEvaluateShim(page);
    await page.setContent(`
      <style>
        @font-face { font-family: 'Oxygen'; src: local('Arial'); }
      </style>
      <table id="rel" style="width:500px;height:120px;font:14px Oxygen,sans-serif;border-collapse:collapse;">
        <tr><th>mainline</th><td>6.11</td><td>2026-08-06</td><td>[tarball]</td></tr>
        <tr><th>stable</th><td>6.10.3</td><td>2026-08-05</td><td>[tarball] [patch]</td></tr>
        <tr><th>longterm</th><td>6.6.40</td><td>2026-08-04</td><td>[tarball] [patch]</td></tr>
        <tr><th>longterm</th><td>6.1.100</td><td>2026-08-03</td><td>[tarball] [patch]</td></tr>
        <tr><th>longterm</th><td>5.15.160</td><td>2026-08-02</td><td>[tarball] [patch]</td></tr>
      </table>
    `);
    const tree = await page.evaluate(extractBoxTree, '#rel');
    await page.close();
    assert.equal(tree.style?.needsRaster, true);
  } finally {
    await browser.close();
  }
});
