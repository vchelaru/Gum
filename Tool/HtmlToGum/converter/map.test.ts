// @ts-nocheck
import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  parseColor, parseBoxShadow, parseHardTextShadows, mapTreeToScreen,
} from './map.js';

// ---- minimal BoxNode/BoxStyle fixtures for mapTreeToScreen ------------------
function baseStyle(overrides = {}) {
  return {
    display: 'block',
    backgroundImage: 'none',
    backgroundSize: 'auto',
    backgroundPosition: '0% 0%',
    objectFit: 'fill',
    objectPosition: '50% 50%',
    listStyleType: 'none',
    flexDirection: 'row',
    flexWrap: 'nowrap',
    rowGap: 0,
    columnGap: 0,
    flexGrow: 0,
    order: 0,
    alignItems: 'normal',
    alignSelf: 'auto',
    justifyContent: 'normal',
    textAlign: 'left',
    paddingTop: 0,
    paddingRight: 0,
    paddingBottom: 0,
    paddingLeft: 0,
    marginTop: 0,
    marginRight: 0,
    marginBottom: 0,
    marginLeft: 0,
    zIndex: 0,
    gridTemplateColumns: 'none',
    gridTemplateRows: 'none',
    gridAutoFlow: 'row',
    gridColumnStart: 'auto',
    gridColumnEnd: 'auto',
    gridRowStart: 'auto',
    gridRowEnd: 'auto',
    gridColumnStartSpecified: '',
    gridColumnEndSpecified: '',
    gridRowStartSpecified: '',
    gridRowEndSpecified: '',
    gridAreaSpecified: '',
    gridColumnSpecified: '',
    gridRowSpecified: '',
    position: 'static',
    backgroundColor: 'rgba(0, 0, 0, 0)',
    borderTopLeftRadius: 0,
    borderTopWidth: 0,
    borderRightWidth: 0,
    borderBottomWidth: 0,
    borderLeftWidth: 0,
    borderTopColor: 'rgb(0, 0, 0)',
    borderRightColor: 'rgb(0, 0, 0)',
    borderBottomColor: 'rgb(0, 0, 0)',
    borderLeftColor: 'rgb(0, 0, 0)',
    boxShadow: 'none',
    textShadow: 'none',
    webkitTextStrokeWidth: 0,
    overflow: 'visible',
    opacity: 1,
    filter: 'none',
    needsRaster: false,
    rasterWholeSubtree: false,
    color: 'rgb(0, 0, 0)',
    fontSize: 16,
    fontWeight: '400',
    fontStyle: 'normal',
    fontFamily: 'sans-serif',
    widthSpecified: 'auto',
    heightSpecified: 'auto',
    borderImageSource: 'none',
    borderImageSlice: 100,
    borderImageRepeat: 'stretch',
    ...overrides,
  };
}
function boxNode(overrides = {}) {
  return {
    id: null,
    tag: 'div',
    rect: { x: 0, y: 0, width: 0, height: 0 },
    text: '',
    lineCount: 0,
    imgSrc: null,
    naturalWidth: 0,
    naturalHeight: 0,
    rasterSrc: null,
    style: baseStyle(),
    children: [],
    ...overrides,
  };
}
function findVar(variables, name) {
  return variables.find((v) => v.name === name);
}

test('parseColor: rgb()', () => {
  assert.deepEqual(parseColor('rgb(255, 0, 128)'), { r: 255, g: 0, b: 128, a: 255 });
});

test('parseColor: rgba() with alpha', () => {
  assert.deepEqual(parseColor('rgba(10, 20, 30, 0.5)'), { r: 10, g: 20, b: 30, a: 128 });
});

test('parseColor: color(srgb ...)', () => {
  assert.deepEqual(parseColor('color(srgb 1 0.5 0)'), { r: 255, g: 128, b: 0, a: 255 });
});

test('parseColor: oklch() white', () => {
  // oklch(1 0 0) is pure white regardless of hue (chroma 0).
  assert.deepEqual(parseColor('oklch(1 0 0)'), { r: 255, g: 255, b: 255, a: 255 });
});

test('parseColor: oklch() black', () => {
  assert.deepEqual(parseColor('oklch(0 0 0)'), { r: 0, g: 0, b: 0, a: 255 });
});

test('parseColor: oklch() from IANA header/footer background', () => {
  // Ground truth from Chromium canvas fillStyle (independent of our own math).
  assert.deepEqual(parseColor('oklch(0.95 0.005 220)'), { r: 235, g: 239, b: 241, a: 255 });
});

test('parseColor: oklch() with alpha', () => {
  const c = parseColor('oklch(0.5 0.1 180 / 0.5)');
  assert.equal(c.a, 128);
});

test('parseColor: unrecognized syntax returns null (not opaque garbage)', () => {
  assert.equal(parseColor('lab(50% 40 59.5)'), null);
});

test('parseColor: oklch() with percentage chroma', () => {
  // Ground truth from Chromium canvas fillStyle: 50% chroma == 0.2 (CSS Color 4 reference
  // range for oklch chroma is 100% = 0.4), both resolve to the same rgb().
  assert.deepEqual(parseColor('oklch(0.5 50% 180)'), parseColor('oklch(0.5 0.2 180)'));
  assert.deepEqual(parseColor('oklch(0.5 50% 180)'), { r: 0, g: 131, b: 104, a: 255 });
});

test('parseBoxShadow: oklch() color', () => {
  const shadow = parseBoxShadow('oklch(0.95 0.005 220) 0px 0px 4px 0px');
  assert.deepEqual(shadow.color, { r: 235, g: 239, b: 241, a: 255 });
});

test('parseHardTextShadows: oklch() color', () => {
  const shadows = parseHardTextShadows({ textShadow: 'oklch(0.58 0.14 251) 1px 1px 0px' });
  assert.equal(shadows.length, 1);
  assert.deepEqual(shadows[0].color, { r: 46, g: 125, b: 202, a: 255 });
});

test('mapTreeToScreen: flex row child margin-top offsets stretch cross-axis (IANA article/main)', () => {
  // Reproduces iana.org/help/example-domains: <article style="display:flex"> containing
  // <main style="margin-top:25px"> under default align-items:stretch. Chromium's flex
  // layout places main's border-box 25px below the container's cross-start (its margin
  // box, not its border box, aligns to cross-start) and shrinks its stretched height by
  // that margin. Previously the converter dropped the child's own cross-axis margin
  // entirely, rendering every stretched row child flush at Y=0.
  const child = boxNode({
    id: 'main',
    rect: { x: 0, y: 25, width: 80, height: 125 },
    style: baseStyle({ marginTop: 25, heightSpecified: 'auto' }),
  });
  const root = boxNode({
    id: 'root',
    rect: { x: 0, y: 0, width: 200, height: 150 },
    style: baseStyle({ display: 'flex', flexDirection: 'row' }),
    children: [child],
  });

  const { variables } = mapTreeToScreen(root);

  assert.equal(findVar(variables, 'Main.YUnits'), undefined); // default PixelsFromTop is fine once Y is set
  assert.equal(findVar(variables, 'Main.Y')?.value, 25);
  assert.equal(findVar(variables, 'Main.HeightUnits')?.value, 2); // RelativeToParent
  assert.equal(findVar(variables, 'Main.Height')?.value, -25);
});

// ---- inline-styled run merging (IANA "Public Technical Identifiers" bold spans) ------
function ianaParagraphFixture(runOverrides = {}) {
  const plainStyle = baseStyle({ fontSize: 12, color: 'rgb(10, 20, 30)' });
  const boldStyle = baseStyle({ fontSize: 12, fontWeight: '700', color: 'rgb(10, 20, 30)', ...runOverrides });
  const run1 = boxNode({
    tag: '#text',
    text: 'The IANA functions ... provided by ',
    rect: { x: 90, y: 778, width: 522, height: 17 },
    lineCount: 1,
    style: plainStyle,
  });
  const run2 = boxNode({
    tag: 'a',
    text: 'Public Technical Identifiers',
    rect: { x: 612, y: 778, width: 161, height: 17 },
    lineCount: 1,
    style: boldStyle,
  });
  const run3 = boxNode({
    tag: '#text',
    text: ', an affiliate of ',
    rect: { x: 773, y: 778, width: 83, height: 17 },
    lineCount: 1,
    style: plainStyle,
  });
  const run4 = boxNode({
    tag: 'a',
    text: 'ICANN',
    rect: { x: 856, y: 778, width: 40, height: 17 },
    lineCount: 1,
    style: boldStyle,
  });
  const p = boxNode({
    id: 'P1',
    tag: 'p',
    rect: { x: 90, y: 778, width: 806, height: 17 },
    style: plainStyle,
    children: [run1, run2, run3, run4],
  });
  return boxNode({
    id: 'root',
    rect: { x: 0, y: 0, width: 1000, height: 900 },
    children: [p],
  });
}

test('mapTreeToScreen: merges same-line inline-styled runs into one Text with BBCode (IANA bold links)', () => {
  // Reproduces iana.org/help/example-domains: a paragraph with plain text, a bold <a> run,
  // more plain text, and another bold <a> run — all on one visual line. Previously each run
  // became its own sibling Text (WidthUnits=RelativeToChildren) positioned at a fixed
  // Absolute X lifted from Chromium; Gum's own bitmap font renders each run at a different
  // pixel width than Chromium measured, so the next run's fixed X drifted from where the
  // previous run actually ended, producing a visible gap/overlap. Merging same-line runs
  // into one Text with BBCode markup lets Gum's own font engine lay out the whole line
  // consistently, the same way a single Text already measures run-by-run styling (#3520).
  const root = ianaParagraphFixture();

  const { instances, variables } = mapTreeToScreen(root);

  const textInstances = instances.filter((i) => i.baseType === 'Text');
  assert.equal(textInstances.length, 1, 'four same-line inline runs should merge into a single Text');
  const name = textInstances[0].name;
  assert.equal(
    findVar(variables, `${name}.Text`)?.value,
    'The IANA functions ... provided by [IsBold=true]Public Technical Identifiers[/IsBold], an affiliate of [IsBold=true]ICANN[/IsBold]',
  );
});

test('mapTreeToScreen: does not merge same-line runs when a run color differs from the base run', () => {
  // Color-changing runs (BBCode Color support) are out of scope for the merge — bail out
  // to the pre-existing per-run Absolute-position behavior rather than silently dropping
  // the color difference.
  const root = ianaParagraphFixture({ color: 'rgb(200, 0, 0)' });

  const { instances } = mapTreeToScreen(root);

  const textInstances = instances.filter((i) => i.baseType === 'Text');
  assert.equal(textInstances.length, 4, 'runs with a differing color should stay as separate Text instances');
});

// ---- CSS sprite-sheet icon crop (GeeksforGeeks social-icon strip) -------------------
test('mapTreeToScreen: background-position sprite offset crops the icon sub-region (GFG social icons)', () => {
  // Ground truth from GeeksforGeeks' live social_sprites_icons.svg: a 38x532 vertical
  // strip, one 38px-tall icon per multiple-of-38 offset. Each icon <div> is
  // background-size:100% (== natural width, so 1:1 scale) + background-position:0 -76px
  // for LinkedIn. Previously the missing crop stretched the *entire* 532px-tall strip into
  // the 38x38 box (visibly garbled), instead of showing just the LinkedIn slice.
  const url = 'https://media.geeksforgeeks.org/wp-content/cdn-uploads/social_sprites_icons.svg';
  const assetMap = new Map([[url, 'Images/social.png']]);
  const icon = boxNode({
    id: 'linkedin',
    rect: { x: 0, y: 0, width: 38, height: 38 },
    naturalWidth: 38,
    naturalHeight: 532,
    style: baseStyle({
      backgroundImage: `url("${url}")`,
      backgroundSize: '100%',
      backgroundPosition: '0px -76px',
    }),
  });
  const root = boxNode({ id: 'root', rect: { x: 0, y: 0, width: 38, height: 38 }, children: [icon] });

  const { variables } = mapTreeToScreen(root, assetMap);

  assert.equal(findVar(variables, 'Linkedin.TextureAddress')?.value, 1); // Custom
  assert.equal(findVar(variables, 'Linkedin.TextureLeft')?.value, 0);
  assert.equal(findVar(variables, 'Linkedin.TextureTop')?.value, 76);
  assert.equal(findVar(variables, 'Linkedin.TextureWidth')?.value, 38);
  assert.equal(findVar(variables, 'Linkedin.TextureHeight')?.value, 38);
});

test('mapTreeToScreen: sprite crop scales to the actual rasterized SVG pixel size', () => {
  // rasterizeSvg (assets.mjs) upscales an SVG source above its declared intrinsic size for
  // a crisper downscale (SVG_UPSCALE/SVG_MAX_DIM) — GFG's 38x532 social-icon SVG actually
  // gets rasterized to 73x1024 on disk (~1.9248x, clamped by SVG_MAX_DIM=1024). A crop
  // computed in naturalWidth/Height (38x532) units without rescaling samples the wrong
  // pixels once written as literal TextureLeft/Top into the 73x1024 file — this reproduces
  // the youtube icon (bottom of the strip) rendering as a garbled diagonal mush.
  const url = 'https://media.geeksforgeeks.org/wp-content/cdn-uploads/social_sprites_icons.svg';
  const assetMap = new Map([[url, 'Images/social.png']]);
  const assetSizeMap = new Map([[url, { width: 73, height: 1024 }]]);
  const icon = boxNode({
    id: 'youtube',
    rect: { x: 0, y: 0, width: 38, height: 38 },
    naturalWidth: 38,
    naturalHeight: 532,
    style: baseStyle({
      backgroundImage: `url("${url}")`,
      backgroundSize: '100%',
      backgroundPosition: '0px -152px',
    }),
  });
  const root = boxNode({ id: 'root', rect: { x: 0, y: 0, width: 38, height: 38 }, children: [icon] });

  const { variables } = mapTreeToScreen(root, assetMap, null, null, null, assetSizeMap);

  assert.equal(findVar(variables, 'Youtube.TextureLeft')?.value, 0);
  assert.equal(findVar(variables, 'Youtube.TextureTop')?.value, Math.round(152 * (1024 / 532)));
  assert.equal(findVar(variables, 'Youtube.TextureWidth')?.value, Math.round(38 * (73 / 38)));
  assert.equal(findVar(variables, 'Youtube.TextureHeight')?.value, Math.round(38 * (1024 / 532)));
});

test('mapTreeToScreen: a sprite tile at the sheet\'s default (0,0) offset still crops', () => {
  // GFG's facebook icon sits at the *top* of the strip (background-position: 0px 0px) — a
  // deliberate sprite-tile selection that happens to coincide with the default position.
  // Distinguishing signal: the same sprite URL is used elsewhere in the tree at a different
  // position (instagram, -38px), so this really is a sprite sheet — a lone background-image
  // at the default position (see the "plain background-image" test below) is left alone.
  // Previously this bailed like a plain background image, stretching the whole 532px-tall
  // strip into the 38x38 box instead of showing just the top (facebook) tile.
  const url = 'https://media.geeksforgeeks.org/wp-content/cdn-uploads/social_sprites_icons.svg';
  const assetMap = new Map([[url, 'Images/social.png']]);
  const facebook = boxNode({
    id: 'facebook',
    rect: { x: 0, y: 0, width: 38, height: 38 },
    naturalWidth: 38,
    naturalHeight: 532,
    style: baseStyle({ backgroundImage: `url("${url}")`, backgroundSize: '100%', backgroundPosition: '0px 0px' }),
  });
  const instagram = boxNode({
    id: 'instagram',
    rect: { x: 43, y: 0, width: 38, height: 38 },
    naturalWidth: 38,
    naturalHeight: 532,
    style: baseStyle({ backgroundImage: `url("${url}")`, backgroundSize: '100%', backgroundPosition: '0px -38px' }),
  });
  const root = boxNode({
    id: 'root', rect: { x: 0, y: 0, width: 81, height: 38 }, children: [facebook, instagram],
  });

  const { variables } = mapTreeToScreen(root, assetMap);

  assert.equal(findVar(variables, 'Facebook.TextureAddress')?.value, 1); // Custom
  assert.equal(findVar(variables, 'Facebook.TextureLeft')?.value, 0);
  assert.equal(findVar(variables, 'Facebook.TextureTop')?.value, 0);
  assert.equal(findVar(variables, 'Facebook.TextureWidth')?.value, 38);
  assert.equal(findVar(variables, 'Facebook.TextureHeight')?.value, 38);
});

test('mapTreeToScreen: plain background-image at default position gets no sprite crop', () => {
  // background-position:0% 0% (the default — no authored offset) is a plain full-image
  // background, not a sprite selection. Must keep stretch-to-fill (no TextureAddress
  // override) rather than emit a crop derived from a coincidental size mismatch.
  const url = 'https://example.com/hero.png';
  const assetMap = new Map([[url, 'Images/hero.png']]);
  const node = boxNode({
    id: 'hero',
    rect: { x: 0, y: 0, width: 200, height: 100 },
    naturalWidth: 400,
    naturalHeight: 200,
    style: baseStyle({ backgroundImage: `url("${url}")`, backgroundSize: 'auto' }),
  });
  const root = boxNode({ id: 'root', rect: { x: 0, y: 0, width: 200, height: 100 }, children: [node] });

  const { variables } = mapTreeToScreen(root, assetMap);

  assert.equal(findVar(variables, 'Hero.TextureAddress'), undefined);
});
