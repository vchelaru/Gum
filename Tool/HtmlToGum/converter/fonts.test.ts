// @ts-nocheck
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, rmSync } from 'node:fs';
import { join } from 'node:path';
import { tmpdir } from 'node:os';
import { repairEmptyCustomFonts, looksLikeFontBuffer, resolveCssFontFamily } from './fonts.js';

test('looksLikeFontBuffer: accepts sfnt / woff magic, rejects HTML', () => {
  assert.equal(looksLikeFontBuffer(Buffer.from([0x00, 0x01, 0x00, 0x00, 0x00])), true);
  assert.equal(looksLikeFontBuffer(Buffer.from('wOFF....')), true);
  assert.equal(looksLikeFontBuffer(Buffer.from('wOF2....')), true);
  assert.equal(looksLikeFontBuffer(Buffer.from('OTTO....')), true);
  assert.equal(looksLikeFontBuffer(Buffer.from('<!DOCTYPE html>')), false);
  assert.equal(looksLikeFontBuffer(Buffer.from('')), false);
});

test('resolveCssFontFamily: skips -apple-system to Segoe UI (App Center stack)', () => {
  const stack = '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Helvetica, Ubuntu, Arial, sans-serif';
  assert.equal(resolveCssFontFamily(stack), 'Segoe UI');
});

test('resolveCssFontFamily: keeps a real custom face first', () => {
  assert.equal(resolveCssFontFamily('"Graphik Web", Arial, sans-serif'), 'Graphik Web');
});

test('resolveCssFontFamily: system-ui alone maps to Segoe UI', () => {
  assert.equal(resolveCssFontFamily('system-ui, sans-serif'), 'Segoe UI');
});

test('repairEmptyCustomFonts: rewrites Fonts/*.ttf with empty atlas to Arial', () => {
  const root = mkdtempSync(join(tmpdir(), 'htmltogum-fonts-'));
  try {
    mkdirSync(join(root, 'FontCache'));
    mkdirSync(join(root, 'Screens'));
    writeFileSync(
      join(root, 'FontCache', 'Font18poppins_w300_i0_ttf.fnt'),
      'info face="Poppins"\nchars count=2\nchar id=32\nchar id=160\n',
    );
    writeFileSync(
      join(root, 'FontCache', 'Font18Arial.fnt'),
      'info face="Arial"\nchars count=95\n',
    );
    writeFileSync(
      join(root, 'Screens', 'Index.gusx'),
      `<Variable Type="string" Name="Span26.Font" SetsValue="true">
      <Value xsi:type="xsd:string">Fonts/poppins_w300_i0.ttf</Value>
    </Variable>
    <Variable Type="string" Name="Title.Font" SetsValue="true">
      <Value xsi:type="xsd:string">Arial</Value>
    </Variable>`,
    );

    const { repaired } = repairEmptyCustomFonts(root);
    assert.deepEqual(repaired, ['Fonts/poppins_w300_i0.ttf']);
    const gusx = readFileSync(join(root, 'Screens', 'Index.gusx'), 'utf8');
    assert.match(gusx, /Span26\.Font[\s\S]*Arial/);
    assert.doesNotMatch(gusx, /Fonts\/poppins_w300_i0\.ttf/);
    assert.match(gusx, /Title\.Font[\s\S]*Arial/);
  } finally {
    rmSync(root, { recursive: true, force: true });
  }
});
