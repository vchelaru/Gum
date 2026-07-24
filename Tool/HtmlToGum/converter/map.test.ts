// @ts-nocheck
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { parseColor, parseBoxShadow, parseHardTextShadows } from './map.js';

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
