// @ts-nocheck
// Per-phase wall clock for convert.ts, written to <out>/timings.json. The Gum plugin reads
// that file and merges it with its own phase timings into one comparable log entry, so a slow
// import can be attributed to a converter phase rather than guessed at.

/**
 * @param {() => number} now monotonic clock in milliseconds
 */
export function createPhaseTimer(now = () => performance.now()) {
  const phases = [];
  const start = now();

  /** Times `fn`, recording the phase whether it resolves or throws. */
  async function time(name, fn) {
    const startedAt = now();
    try {
      return await fn();
    } finally {
      phases.push({ name, ms: Math.round(now() - startedAt) });
    }
  }

  /** Records a duration measured elsewhere (e.g. summed across a loop). */
  function mark(name, ms) {
    phases.push({ name, ms: Math.round(ms) });
  }

  /** @param {Record<string, number>} counts page-size figures a run can be normalized against */
  function toJSON(counts = {}) {
    return { phases: [...phases], counts, totalMs: Math.round(now() - start) };
  }

  return { time, mark, toJSON };
}
