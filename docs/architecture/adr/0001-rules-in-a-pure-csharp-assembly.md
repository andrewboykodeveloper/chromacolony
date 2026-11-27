---
status: accepted
date: 2026-11-26
---

# Keep the rules in a C# assembly with no engine references

Recorded at 1.0, after the decision was taken.

## Context and Problem Statement

The rules decide everything that can be won or lost: the boards, the columns of crews, the slots, the crabs and the pebbles they carry, the helpers, the coins, the saves and the tutorial. The same taps at the same moments, with the same seed, must give the same board, and the rules must be testable without entering Play Mode. Where should they live?

## Considered Options

- In the MonoBehaviours that show them.
- In plain C# classes inside the game's assembly.
- In an assembly of their own that cannot reference the engine.

## Decision Outcome

Chosen option: "In an assembly of their own that cannot reference the engine", because the compiler then keeps engine types and frame timing out of the rules, and EditMode tests run them without a scene. The assembly is `Chromacolony.Core`, with `noEngineReferences` set.

- Time enters the rules only through `GameModel.Tick(dt)`, driven by a fixed step, and chance only through an injected `IRandom`, a seeded PCG32.
- The model raises C# events.
- The Unity side keeps the colours, the positions and the timing of the animations.

In the MonoBehaviours, the rules would need a scene and frames to test. In the game's assembly, nothing would stop an engine type from slipping in.

### Consequences

- Good: EditMode tests run the rules in milliseconds, including a solver over every shipped board and a replay that must end at the same tick with the same outcome.
- Good: the compiler rejects an engine type in the rules.
- Bad: values cross the boundary as plain types, such as a colour index instead of a `Color`, so the views translate them.
