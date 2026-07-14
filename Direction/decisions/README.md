# Decision Records (ADRs)

An **Architecture Decision Record (ADR)** captures one significant decision: the context that
forced it, the decision made, and the consequences. ADRs are **append-only** — once accepted, a
record is not rewritten. If a decision is reversed or changed, write a *new* ADR and mark this
one `Superseded by NNNN`.

This convention exists so the *reasoning* behind direction survives context clears, the passage
of time, and new collaborators — you can always answer "why is it this way?" without
re-litigating it.

## Conventions

- Filename: `NNNN-short-kebab-title.md`, zero-padded and sequential (`0001-…`, `0002-…`).
- Copy `0000-template.md` to start a new record.
- Status flow: `Proposed` → `Accepted` → optionally `Superseded by NNNN` / `Deprecated`.
- One decision per ADR. Small and focused beats comprehensive.

## Index

| #    | Title                                            | Status   | Date       |
|------|--------------------------------------------------|----------|------------|
| [0001](0001-track-direction-in-checked-in-markdown.md) | Track project direction in checked-in Markdown | Accepted | 2026-06-20 |
| [0002](0002-target-code-first-frameworks-not-engines.md) | Target code-first C# frameworks, not full engines | Accepted | 2026-06-20 |
| [0003](0003-decouple-tool-ui-from-logic.md) | Decouple the Gum tool's UI from its application logic | Accepted | 2026-06-20 |
| [0004](0004-viewmodels-expose-neutral-presentation-state.md) | ViewModels expose resolved display state in neutral types | Accepted | 2026-06-20 |
| [0005](0005-headless-presentation-assembly.md) | Home for the headless presentation layer: a dedicated `Gum.Presentation` assembly | Accepted | 2026-06-24 |
| [0006](0006-runtimes-declare-capabilities-through-igumservice.md) | Runtimes declare platform capabilities through `IGumService` | Accepted | 2026-07-10 |
| [0007](0007-converge-skia-property-dispatch.md) | Converge the Skia property dispatcher via runtime-type-first dispatch | Accepted | 2026-07-13 |
| [0008](0008-sequence-runtime-dispatch-convergence.md) | Sequence the runtime-type-first dispatch convergence: parity, then redispatch, then converge | Accepted | 2026-07-13 |
