// @ts-nocheck
import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  parseColor, parseBoxShadow, parseHardTextShadows, mapTreeToScreen,
  resolveBackgroundImageLayout,
} from './map.js';

// ---- minimal BoxNode/BoxStyle fixtures for mapTreeToScreen ------------------
function baseStyle(overrides = {}) {
  return {
    display: 'block',
    backgroundImage: 'none',
    backgroundSize: 'auto',
    backgroundPosition: '0% 0%',
    backgroundRepeat: 'repeat',
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

test('mapTreeToScreen: flex row child with width:100% uses Absolute measured width (KORE login column)', () => {
  // CSS width:100% on a flex item resolves against the flex container, but Chromium's
  // *used* width is flex-constrained (e.g. 384 in a 768 row). Emitting PercentageOfParent
  // 100 made the column full-width and shifted the login form left.
  const illus = boxNode({
    id: 'illus',
    rect: { x: 0, y: 0, width: 404, height: 500 },
    style: baseStyle({ widthSpecified: '500px' }),
  });
  const formCol = boxNode({
    id: 'formCol',
    rect: { x: 404, y: 0, width: 384, height: 430 },
    style: baseStyle({ widthSpecified: '100%' }),
  });
  const root = boxNode({
    id: 'row',
    rect: { x: 0, y: 0, width: 768, height: 500 },
    style: baseStyle({ display: 'flex', flexDirection: 'row', justifyContent: 'center' }),
    children: [illus, formCol],
  });

  const { variables } = mapTreeToScreen(root);
  assert.equal(findVar(variables, 'FormCol.WidthUnits')?.value, 0); // Absolute
  assert.equal(findVar(variables, 'FormCol.Width')?.value, 384);
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

test('mapTreeToScreen: root tiled background offsets for viewport/canvas tile origin', () => {
  // Body margin → rect (8,8); CSS paints background from the canvas origin, so Wrap
  // tiles must start at local (-8,-8) with size parent+(8,8).
  const url = 'https://example.com/stars.gif';
  const assetMap = new Map([[url, 'Images/stars.gif']]);
  const child = boxNode({ id: 'main', rect: { x: 8, y: 8, width: 400, height: 300 } });
  const node = boxNode({
    id: 'body',
    tag: 'body',
    rect: { x: 8, y: 8, width: 400, height: 300 },
    naturalWidth: 111,
    naturalHeight: 111,
    style: baseStyle({
      backgroundImage: `url("${url}")`,
      backgroundRepeat: 'repeat',
      backgroundColor: 'rgb(0, 0, 0)',
    }),
    children: [child],
  });
  const { variables } = mapTreeToScreen(node, assetMap);
  assert.equal(findVar(variables, 'BodyBg.X')?.value, -8);
  assert.equal(findVar(variables, 'BodyBg.Y')?.value, -8);
  assert.equal(findVar(variables, 'BodyBg.Width')?.value, 8);
  assert.equal(findVar(variables, 'BodyBg.Height')?.value, 8);
  assert.equal(findVar(variables, 'BodyBg.TextureAddress')?.value, 2);
});

test('mapTreeToScreen: fixed/absolute descendant does not inflate a styled ancestor backdrop', () => {
  // OWASP header (periwinkle bg, 159px) contains a position:fixed cookie banner painted at
  // y=800. textOverflowPad must skip out-of-flow subtrees, else the header backdrop grows to
  // ~1544px and its fill tints the whole page. In-flow text near the box bottom still pads.
  const inFlowText = boxNode({
    id: 'logo', tag: 'span', text: 'OWASP', lineCount: 1,
    rect: { x: 18, y: 120, width: 120, height: 30 },
    style: baseStyle({ fontSize: 20 }),
  });
  const bannerText = boxNode({
    id: 'cookie', tag: 'p', text: 'This website uses cookies to analyze traffic.', lineCount: 3,
    rect: { x: 16, y: 810, width: 500, height: 60 },
    style: baseStyle({ fontSize: 16 }),
  });
  const fixedBanner = boxNode({
    id: 'disclaimer-container', tag: 'div',
    rect: { x: 0, y: 800, width: 800, height: 100 },
    style: baseStyle({ position: 'fixed', backgroundColor: 'rgba(20, 20, 20, 0.8)' }),
    children: [bannerText],
  });
  const header = boxNode({
    id: 'header', tag: 'header',
    rect: { x: 0, y: 0, width: 800, height: 159 },
    style: baseStyle({ backgroundColor: 'rgb(152, 175, 199)' }),
    children: [inFlowText, fixedBanner],
  });
  const { variables } = mapTreeToScreen(header, new Map());
  const h = findVar(variables, 'Header.Height')?.value;
  assert.ok(h < 200, `header backdrop should stay ~159px, got ${h}`);
});

test('mapTreeToScreen: repeating background-image tiles via DimensionsBased + Wrap', () => {
  // CSS default background-repeat is `repeat` (Space Jam starfield). EntireTexture would
  // stretch one tile across the box; DimensionsBased + Wrap repeats at intrinsic size.
  const url = 'https://example.com/stars.gif';
  const assetMap = new Map([[url, 'Images/stars.gif']]);
  const child = boxNode({
    id: 'main',
    rect: { x: 0, y: 0, width: 400, height: 300 },
  });
  const node = boxNode({
    id: 'body',
    tag: 'body',
    rect: { x: 0, y: 0, width: 400, height: 300 },
    naturalWidth: 111,
    naturalHeight: 111,
    style: baseStyle({
      backgroundImage: `url("${url}")`,
      backgroundSize: 'auto',
      backgroundRepeat: 'repeat',
      backgroundColor: 'rgb(0, 0, 0)',
    }),
    children: [child],
  });

  const { variables } = mapTreeToScreen(node, assetMap);
  assert.equal(findVar(variables, 'BodyBg.TextureAddress')?.value, 2); // DimensionsBased
  assert.equal(findVar(variables, 'BodyBg.Wrap')?.value, 'true');
});

test('mapTreeToScreen: empty leaf with repeating background-image also tiles', () => {
  // classify() promotes background-image-only leaves to 'image' (Tabler card-img-top).
  const url = 'https://example.com/stars.gif';
  const assetMap = new Map([[url, 'Images/stars.gif']]);
  const node = boxNode({
    id: 'panel',
    rect: { x: 0, y: 0, width: 400, height: 300 },
    naturalWidth: 111,
    naturalHeight: 111,
    style: baseStyle({
      backgroundImage: `url("${url}")`,
      backgroundRepeat: 'repeat',
      backgroundColor: 'rgb(0, 0, 0)',
    }),
  });
  const { variables } = mapTreeToScreen(node, assetMap);
  assert.equal(findVar(variables, 'PanelImg.TextureAddress')?.value, 2);
  assert.equal(findVar(variables, 'PanelImg.Wrap')?.value, 'true');
});

test('mapTreeToScreen: no-repeat background-image does not enable Wrap tiling', () => {
  const url = 'https://example.com/hero.png';
  const assetMap = new Map([[url, 'Images/hero.png']]);
  const node = boxNode({
    id: 'hero',
    rect: { x: 0, y: 0, width: 200, height: 100 },
    naturalWidth: 400,
    naturalHeight: 200,
    style: baseStyle({
      backgroundImage: `url("${url}")`,
      backgroundSize: 'auto',
      backgroundRepeat: 'no-repeat',
    }),
  });
  const root = boxNode({ id: 'root', rect: { x: 0, y: 0, width: 200, height: 100 }, children: [node] });

  const { variables } = mapTreeToScreen(root, assetMap);

  assert.equal(findVar(variables, 'HeroBg.TextureAddress'), undefined);
  assert.equal(findVar(variables, 'HeroBg.Wrap'), undefined);
});

test('mapTreeToScreen: plain background-image at default position gets no sprite crop', () => {
  // background-position:0% 0% (the default — no authored offset) is a plain full-image
  // background, not a sprite selection. With background-repeat:no-repeat it keeps
  // stretch-to-fill (no TextureAddress override) rather than a crop from size mismatch.
  const url = 'https://example.com/hero.png';
  const assetMap = new Map([[url, 'Images/hero.png']]);
  const node = boxNode({
    id: 'hero',
    rect: { x: 0, y: 0, width: 200, height: 100 },
    naturalWidth: 400,
    naturalHeight: 200,
    style: baseStyle({
      backgroundImage: `url("${url}")`,
      backgroundSize: 'auto',
      backgroundRepeat: 'no-repeat',
    }),
  });
  const root = boxNode({ id: 'root', rect: { x: 0, y: 0, width: 200, height: 100 }, children: [node] });

  const { variables } = mapTreeToScreen(root, assetMap);

  assert.equal(findVar(variables, 'HeroBg.TextureAddress'), undefined);
});

test('mapTreeToScreen: body backdrop height grows for BitmapFont text spill past Chromium box', () => {
  const url = 'https://example.com/stars.gif';
  const assetMap = new Map([[url, 'Images/stars.gif']]);
  const text = boxNode({
    id: 'copy',
    tag: 'font',
    text: 'SPACE JAM copyright line one\nline two',
    lineCount: 2,
    rect: { x: 100, y: 270, width: 200, height: 24 },
    style: baseStyle({ fontSize: 14, color: 'rgb(255, 0, 0)' }),
  });
  const body = boxNode({
    id: 'body',
    tag: 'body',
    rect: { x: 0, y: 0, width: 400, height: 300 },
    naturalWidth: 111,
    naturalHeight: 111,
    style: baseStyle({
      backgroundImage: `url("${url}")`,
      backgroundRepeat: 'repeat',
      backgroundColor: 'rgb(0, 0, 0)',
    }),
    children: [text],
  });

  const { variables } = mapTreeToScreen(body, assetMap);
  // guess = round(14 * 1.35 * 2) = 38; spill = 38-24 = 14; bottom spill past body = (270+24+14)-300 = 8
  assert.equal(findVar(variables, 'Body.Height')?.value, 308);
});

test('mapTreeToScreen: merges multi-line inline runs with Chromium newlines + BBCode bold', () => {
  // Space Jam sitemap: TD prose split into per-line #text + <b> Absolute leaves.
  const td = boxNode({
    id: 'cell',
    tag: 'td',
    rect: { x: 10, y: 10, width: 200, height: 60 },
    style: baseStyle({
      fontSize: 13,
      color: 'rgb(254, 255, 137)',
      fontFamily: 'Times New Roman',
      borderTopWidth: 1,
      borderRightWidth: 1,
      borderBottomWidth: 1,
      borderLeftWidth: 1,
      borderTopColor: 'rgb(154, 154, 154)',
      borderLeftColor: 'rgb(154, 154, 154)',
      borderBottomColor: 'rgb(238, 238, 238)',
      borderRightColor: 'rgb(238, 238, 238)',
      paddingTop: 10,
      paddingLeft: 10,
      paddingRight: 10,
      paddingBottom: 10,
    }),
    children: [
      boxNode({
        tag: '#text',
        text: 'Go behind the scenes of ',
        lineCount: 1,
        rect: { x: 21, y: 21, width: 140, height: 17 },
        style: baseStyle({ fontSize: 13, color: 'rgb(254, 255, 137)', fontFamily: 'Times New Roman' }),
      }),
      boxNode({
        tag: 'b',
        text: 'Space Jam',
        lineCount: 1,
        rect: { x: 161, y: 21, width: 60, height: 17 },
        style: baseStyle({
          fontSize: 13, fontWeight: '700', color: 'rgb(254, 255, 137)', fontFamily: 'Times New Roman',
        }),
      }),
      boxNode({
        tag: '#text',
        text: 'See how the new characters were developed.',
        lineCount: 1,
        rect: { x: 21, y: 39, width: 180, height: 17 },
        style: baseStyle({ fontSize: 13, color: 'rgb(254, 255, 137)', fontFamily: 'Times New Roman' }),
      }),
    ],
  });
  const { instances, variables } = mapTreeToScreen(td, new Map());
  const texts = instances.filter((i) => i.baseType === 'Text');
  assert.equal(texts.length, 1);
  const textVar = findVar(variables, `${texts[0].name}.Text`);
  assert.ok(textVar?.value.includes('[IsBold=true]Space Jam[/IsBold]')
    || textVar?.value.includes('[IsBold=True]Space Jam[/IsBold]'));
  assert.ok(textVar?.value.includes('\n'));
  assert.equal(textVar.value.split('\n').length, 2);
  // Host keeps chrome; label is Absolute inside the padding box at Chromium's glyph union.
  assert.ok(instances.some((i) => i.baseType === 'Container'));
  const label = texts[0];
  // content origin = host(10) + border(1) + pad(10) = 21; glyph y=21 → Y=0
  assert.equal(findVar(variables, `${label.name}.Y`)?.value, 0);
});

test('mapTreeToScreen: HTML table grey bevel uses per-side edge colors (not yellow stroke)', () => {
  // extract.ts rewrites presentational table borders to Chromium's painted greys;
  // asymmetric colors must become four edge Rectangles, not a uniform yellow Stroke.
  const cell = boxNode({
    id: 'cell',
    tag: 'td',
    rect: { x: 10, y: 10, width: 100, height: 40 },
    text: 'Jam Central',
    lineCount: 1,
    style: baseStyle({
      borderTopWidth: 1,
      borderRightWidth: 1,
      borderBottomWidth: 1,
      borderLeftWidth: 1,
      borderTopColor: 'rgb(154, 154, 154)',
      borderLeftColor: 'rgb(154, 154, 154)',
      borderBottomColor: 'rgb(238, 238, 238)',
      borderRightColor: 'rgb(238, 238, 238)',
      paddingTop: 10,
      paddingLeft: 10,
      paddingRight: 10,
      paddingBottom: 10,
      color: 'rgb(254, 255, 137)',
      fontSize: 13,
    }),
  });
  const { instances, variables } = mapTreeToScreen(cell, new Map());
  const borderEdges = instances.filter((i) => /Border(Top|Left|Bottom|Right)$/.test(i.name));
  assert.equal(borderEdges.length, 4);
  // Top/left dark grey, bottom/right light grey — not the yellow text color.
  assert.equal(findVar(variables, `${borderEdges.find((i) => i.name.endsWith('BorderTop')).name}.FillRed`)?.value, 154);
  assert.equal(findVar(variables, `${borderEdges.find((i) => i.name.endsWith('BorderBottom')).name}.FillRed`)?.value, 238);
  assert.ok(!variables.some((v) => v.name.endsWith('.StrokeRed') && v.value === 254));
});

test('mapTreeToScreen: padded single-line text label hugs width (no BitmapFont wrap)', () => {
  // TL nav links: padding → Container + Label; Label must RelativeToChildren or
  // "Calendar"/"Streams" wrap inside Chromium's padding-box width.
  const link = boxNode({
    id: 'nav',
    tag: 'a',
    text: 'Calendar',
    lineCount: 1,
    rect: { x: 100, y: 140, width: 66, height: 30 },
    style: baseStyle({
      fontSize: 11,
      color: 'rgb(255, 255, 255)',
      paddingTop: 9,
      paddingBottom: 9,
      paddingLeft: 10,
      paddingRight: 10,
      textAlign: 'left',
    }),
  });
  const { variables } = mapTreeToScreen(link, new Map());
  assert.equal(findVar(variables, 'NavLabel.WidthUnits')?.value, 4); // RelativeToChildren
  assert.equal(findVar(variables, 'NavLabel.Width'), undefined);
});

test('mapTreeToScreen: Absolute-parent right-aligned single-line text hugs + anchors right', () => {
  const label = boxNode({
    id: 'lp',
    tag: 'a',
    text: 'Liquipedia',
    lineCount: 1,
    rect: { x: 680, y: 4, width: 55, height: 12 },
    style: baseStyle({ fontSize: 11, color: 'rgb(255,255,255)', textAlign: 'right' }),
  });
  const root = boxNode({
    id: 'hdr',
    tag: 'div',
    rect: { x: 0, y: 0, width: 800, height: 40 },
    style: baseStyle({ position: 'relative' }),
    children: [label],
  });
  const { variables } = mapTreeToScreen(root, new Map());
  assert.equal(findVar(variables, 'Lp.WidthUnits')?.value, 4);
  assert.equal(findVar(variables, 'Lp.XOrigin')?.value, 2); // Right
  assert.equal(findVar(variables, 'Lp.X')?.value, 735); // 680+55
});

test('resolveBackgroundImageLayout: px width + auto height (KORE logo)', () => {
  // background-size: 100px; natural 300×141 → display ~100×47 at 0% 0%
  const layout = resolveBackgroundImageLayout(
    baseStyle({ backgroundSize: '100px', backgroundPosition: '0% 0%', backgroundRepeat: 'no-repeat' }),
    300, 141, 415, 50,
  );
  assert.deepEqual(layout, { x: 0, y: 0, width: 100, height: 47 });
});

test('resolveBackgroundImageLayout: px width + 50% 50% position (KORE hero)', () => {
  // background-size: 400px; natural 651×509 in 404×500 box → ~400×313 centered
  const layout = resolveBackgroundImageLayout(
    baseStyle({ backgroundSize: '400px', backgroundPosition: '50% 50%', backgroundRepeat: 'no-repeat' }),
    651, 509, 404, 500,
  );
  assert.equal(layout.width, 400);
  assert.equal(layout.height, 313); // round(400 * 509/651)
  assert.equal(layout.x, 2); // round((404-400)/2)
  assert.equal(layout.y, 94); // round((500-313)/2) = 93.5 → 94
});

test('resolveBackgroundImageLayout: contain fits inside the box', () => {
  const layout = resolveBackgroundImageLayout(
    baseStyle({ backgroundSize: 'contain', backgroundPosition: '0% 0%', backgroundRepeat: 'no-repeat' }),
    25, 17, 20, 20,
  );
  assert.deepEqual(layout, { x: 0, y: 0, width: 20, height: 14 });
});

test('resolveBackgroundImageLayout: auto + 50% 0% uses natural size (TL banner)', () => {
  const layout = resolveBackgroundImageLayout(
    baseStyle({ backgroundSize: 'auto', backgroundPosition: '50% 0%', backgroundRepeat: 'no-repeat' }),
    1200, 210, 800, 210,
  );
  assert.deepEqual(layout, { x: -200, y: 0, width: 1200, height: 210 });
});

test('resolveBackgroundImageLayout: cover stays on stretch path; % size too', () => {
  assert.equal(resolveBackgroundImageLayout(
    baseStyle({ backgroundSize: 'cover', backgroundRepeat: 'no-repeat' }), 100, 50, 200, 100,
  ), null);
  assert.equal(resolveBackgroundImageLayout(
    baseStyle({ backgroundSize: '100%', backgroundRepeat: 'no-repeat' }), 100, 50, 200, 100,
  ), null);
});

test('mapTreeToScreen: background-size px places Sprite instead of stretch-fill (KORE logo)', () => {
  const url = 'https://example.com/kore-logo.svg';
  const assetMap = new Map([[url, 'Images/logo.png']]);
  const node = boxNode({
    id: 'kc-header',
    tag: 'div',
    rect: { x: 466, y: 255, width: 415, height: 50 },
    naturalWidth: 300,
    naturalHeight: 141,
    style: baseStyle({
      backgroundImage: `url("${url}")`,
      backgroundSize: '100px',
      backgroundPosition: '0% 0%',
      backgroundRepeat: 'no-repeat',
      backgroundColor: 'rgba(0,0,0,0)',
    }),
    children: [],
  });
  const { instances, variables } = mapTreeToScreen(node, assetMap);
  const bg = instances.find((i) => i.baseType === 'Sprite');
  assert.ok(bg, `expected placed Sprite, got ${instances.map((i) => i.name + ':' + i.baseType)}`);
  assert.equal(findVar(variables, `${bg.name}.Width`)?.value, 100);
  assert.equal(findVar(variables, `${bg.name}.Height`)?.value, 47);
  assert.equal(findVar(variables, `${bg.name}.WidthUnits`)?.value, 0); // Absolute
  assert.equal(findVar(variables, `${bg.name}.X`)?.value, 0);
  assert.equal(findVar(variables, `${bg.name}.SourceFile`)?.value, 'Images/logo.png');
});

test('mapTreeToScreen: negative z-index abs under flex paints before Content (catfishing watermark)', () => {
  const url = 'https://example.com/watermark.png';
  const assetMap = new Map([[url, 'Images/wm.png']]);
  const watermark = boxNode({
    id: 'wm',
    tag: 'div',
    rect: { x: 0, y: 0, width: 800, height: 260 },
    naturalWidth: 800,
    naturalHeight: 260,
    style: baseStyle({
      position: 'absolute',
      zIndex: -10,
      backgroundImage: `url("${url}")`,
      backgroundSize: 'cover',
      backgroundRepeat: 'no-repeat',
    }),
  });
  const nav = boxNode({
    id: 'nav',
    tag: 'menu',
    rect: { x: 200, y: 0, width: 400, height: 64 },
    text: 'Archive',
    lineCount: 1,
    style: baseStyle({ display: 'block', color: 'rgb(167,243,208)', fontSize: 16 }),
  });
  const header = boxNode({
    id: 'header',
    tag: 'header',
    rect: { x: 0, y: 0, width: 800, height: 64 },
    style: baseStyle({
      display: 'flex',
      flexDirection: 'row',
      position: 'relative',
      backgroundColor: 'rgb(10,58,42)',
    }),
    children: [watermark, nav],
  });
  const { instances, variables } = mapTreeToScreen(header, assetMap);
  const headerName = instances[0]?.name;
  assert.ok(headerName);
  // Sibling order under Header: watermark (z<0) before HeaderContent (in-flow nav).
  const underHeader = instances.filter((i) => findVar(variables, `${i.name}.Parent`)?.value === headerName);
  const names = underHeader.map((i) => i.name);
  const wmIdx = names.findIndex((n) => n === 'Wm' || n.startsWith('Wm'));
  const contentIdx = names.findIndex((n) => /Content$/.test(n));
  assert.ok(wmIdx >= 0, `expected watermark under header, got ${names.join(',')}`);
  assert.ok(contentIdx >= 0, `expected Content under header, got ${names.join(',')}`);
  assert.ok(wmIdx < contentIdx, `watermark (${wmIdx}) must precede Content (${contentIdx}): ${names.join(',')}`);
});
