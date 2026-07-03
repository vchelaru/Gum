---
name: debugging
description: How to investigate a hard bug without tunnel-visioning on one method. Triggers: a hypothesis just failed, considering another automated repro attempt, "I'm stuck", a long-running investigation.
---

# Debugging Without Tunnel Vision

Trying new hypotheses but testing all of them with the same *method* is still tunnel vision, even
though each attempt feels like progress. Cap it: 2 attempts per category below, then switch
categories. (Seen in #3475 — three synthetic unit-test probes for a bug only visible in a real
window.)

## Categories

- **Static/automated** — unit tests, rendering-PNG tests, simplified scenarios
- **Live observation** — run + logs, breakpoints, user visually confirms
- **Historical/comparative** — bisect PRs/functionality, `git log`/`blame`, cross-backend diff
- **Structural isolation** — minimal repro outside the app/framework
- **External verification** — read the actual upstream source, its issue tracker
- **Specialized tooling** — GPU frame capture, other domain debuggers

## Stop immediately if

- A sanity-check of your *own method* fails — the method is invalid, not "try harder within it."
- The symptom only shows in a context your current tool can't observe (e.g. a headless test can't
  see a real window) — switch categories, don't refine the probe.

## Communicate

Narrate switches ("ruled out X, trying Y") instead of going silent. Keep working if confident;
only stop to discuss when genuinely unsure which category to try next.
