// @ts-nocheck
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { detectPageRejection } from './rejection.js';

test('detectPageRejection: http 403', () => {
  const r = detectPageRejection({ status: 403, title: 'Ok', html: '<html></html>' });
  assert.equal(r.rejected, true);
  assert.match(r.reason, /http 403/);
});

test('detectPageRejection: cloudflare title', () => {
  const r = detectPageRejection({ status: 200, title: 'Just a moment...', html: '<html></html>' });
  assert.equal(r.rejected, true);
  assert.match(r.reason, /title:/);
});

test('detectPageRejection: challenge body', () => {
  const r = detectPageRejection({
    status: 200,
    title: 'Digg',
    html: '<div id="x" class="cf-browser-verification">wait</div>',
  });
  assert.equal(r.rejected, true);
  assert.match(r.reason, /body:/);
});

test('detectPageRejection: MediaWiki hcaptcha config is not a challenge wall', () => {
  const r = detectPageRejection({
    status: 200,
    title: 'Wikipedia, the free encyclopedia',
    html: '<html><script>{"wgConfirmEditHCaptchaSiteKey":"abc","wgConfirmEditCaptchaNeededForGenericEdit":"hcaptcha"}</script><body><h1>Main Page</h1></body></html>',
  });
  assert.equal(r.rejected, false);
});

test('detectPageRejection: normal page passes', () => {
  const r = detectPageRejection({
    status: 200,
    title: 'Space Jam',
    html: '<html><body><h1>Welcome</h1><noscript>Please enable JavaScript</noscript></body></html>',
  });
  assert.equal(r.rejected, false);
});

test('detectPageRejection: http 200 empty is fine', () => {
  const r = detectPageRejection({ status: 200, title: '', html: '' });
  assert.equal(r.rejected, false);
});
