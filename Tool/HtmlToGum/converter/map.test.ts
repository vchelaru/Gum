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
