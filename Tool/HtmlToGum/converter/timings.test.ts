// @ts-nocheck
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createPhaseTimer } from './timings.js';

/** Deterministic clock: each read advances by the next queued step. */
function fakeClock(steps) {
  let t = 0;
  let i = 0;
  return () => {
    const v = t;
    t += steps[i] ?? 0;
    i++;
    return v;
  };
}

test('createPhaseTimer: records phases in order and returns the wrapped value', async () => {
  // Reads: start=0, p1 t0=10, p1 t1=20, p2 t0=25, p2 t1=100, toJSON=140.
  const timer = createPhaseTimer(fakeClock([10, 10, 5, 75, 40]));

  const value = await timer.time('goto', async () => 'page');
  await timer.time('extract', async () => {});
  timer.mark('external', 12.4);

  assert.equal(value, 'page');
  assert.deepEqual(timer.toJSON({ nodes: 3 }), {
    phases: [
      { name: 'goto', ms: 10 },
      { name: 'extract', ms: 75 },
      { name: 'external', ms: 12 },
    ],
    counts: { nodes: 3 },
    totalMs: 140,
  });
});

test('createPhaseTimer: records a phase even when the wrapped work throws', async () => {
  const timer = createPhaseTimer(fakeClock([0, 30, 0]));

  await assert.rejects(() => timer.time('image download', async () => {
    throw new Error('boom');
  }));

  assert.deepEqual(timer.toJSON().phases, [{ name: 'image download', ms: 30 }]);
});
