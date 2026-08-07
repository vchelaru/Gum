// @ts-nocheck
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { screenNameFromUrl } from './crawl.js';

test('screenNameFromUrl: jam.htm → Jam', () => {
  assert.equal(screenNameFromUrl('https://www.spacejam.com/1996/jam.htm'), 'Jam');
});

test('screenNameFromUrl: nested leaf only', () => {
  assert.equal(
    screenNameFromUrl('https://www.spacejam.com/1996/cmp/jumpstation.htm'),
    'Jumpstation',
  );
});

test('screenNameFromUrl: leading digit gets Page_ prefix', () => {
  assert.equal(
    screenNameFromUrl('https://example.com/1996/404.htm'),
    'Page404',
  );
});
