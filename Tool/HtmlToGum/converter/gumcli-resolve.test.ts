// @ts-nocheck
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, mkdirSync, writeFileSync, rmSync, utimesSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { resolveGumCliEntrypoint } from './gumcli-resolve.js';

function makeFakeCsprojDir() {
  const dir = mkdtempSync(join(tmpdir(), 'gumcli-resolve-test-'));
  writeFileSync(join(dir, 'Gum.Cli.csproj'), '<Project />');
  return dir;
}

function touchDll(projectDir, config, whenMs) {
  const dir = join(projectDir, 'bin', config, 'net8.0');
  mkdirSync(dir, { recursive: true });
  const path = join(dir, 'gumcli.dll');
  writeFileSync(path, 'fake');
  if (whenMs !== undefined) utimesSync(path, whenMs / 1000, whenMs / 1000);
  return path;
}

test('resolveGumCliEntrypoint: falls back to dotnet run when nothing is built', () => {
  const dir = makeFakeCsprojDir();
  try {
    const result = resolveGumCliEntrypoint(join(dir, 'Gum.Cli.csproj'));
    assert.equal(result.mode, 'run');
  } finally {
    rmSync(dir, { recursive: true, force: true });
  }
});

test('resolveGumCliEntrypoint: uses the DLL when only one config is built', () => {
  const dir = makeFakeCsprojDir();
  try {
    const dllPath = touchDll(dir, 'Debug');
    const result = resolveGumCliEntrypoint(join(dir, 'Gum.Cli.csproj'));
    assert.equal(result.mode, 'dll');
    assert.equal(result.dllPath, dllPath);
  } finally {
    rmSync(dir, { recursive: true, force: true });
  }
});

test('resolveGumCliEntrypoint: prefers the more recently built config when both exist', () => {
  const dir = makeFakeCsprojDir();
  try {
    const now = Date.now();
    touchDll(dir, 'Release', now - 10_000); // older
    const newerDebug = touchDll(dir, 'Debug', now); // newer
    const result = resolveGumCliEntrypoint(join(dir, 'Gum.Cli.csproj'));
    assert.equal(result.mode, 'dll');
    assert.equal(result.dllPath, newerDebug);
  } finally {
    rmSync(dir, { recursive: true, force: true });
  }
});
