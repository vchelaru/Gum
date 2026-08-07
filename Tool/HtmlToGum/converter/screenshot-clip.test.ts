// @ts-nocheck
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { intersectScreenshotClip } from './screenshot-clip.js';

test('intersectScreenshotClip: keeps an in-bounds clip', () => {
  assert.deepEqual(
    intersectScreenshotClip({ x: 10.2, y: 20.8, width: 100.1, height: 50.9 }, 800, 900),
    { x: 10, y: 20, width: 101, height: 52 },
  );
});

test('intersectScreenshotClip: clamps to page edges', () => {
  assert.deepEqual(
    intersectScreenshotClip({ x: 750, y: 850, width: 100, height: 100 }, 800, 900),
    { x: 750, y: 850, width: 50, height: 50 },
  );
});

test('intersectScreenshotClip: negative origin clamps to 0', () => {
  assert.deepEqual(
    intersectScreenshotClip({ x: -40, y: -10, width: 80, height: 40 }, 800, 900),
    { x: 0, y: 0, width: 40, height: 30 },
  );
});

test('intersectScreenshotClip: negative y keeps bottom at y+h (mdbook body)', () => {
  // Bug: clamp y to 0 but keep full height → 742px clip past BodyBg (ends ~691).
  assert.deepEqual(
    intersectScreenshotClip({ x: 0, y: -50, width: 800, height: 741.3125 }, 800, 900),
    { x: 0, y: 0, width: 800, height: 692 },
  );
});

test('intersectScreenshotClip: fully off-page returns null', () => {
  assert.equal(intersectScreenshotClip({ x: 900, y: 0, width: 50, height: 50 }, 800, 900), null);
  assert.equal(intersectScreenshotClip({ x: -100, y: 0, width: 50, height: 50 }, 800, 900), null);
});

test('intersectScreenshotClip: zero / NaN size returns null', () => {
  assert.equal(intersectScreenshotClip({ x: 0, y: 0, width: 0, height: 10 }, 800, 900), null);
  assert.equal(intersectScreenshotClip({ x: 0, y: 0, width: NaN, height: 10 }, 800, 900), null);
});
