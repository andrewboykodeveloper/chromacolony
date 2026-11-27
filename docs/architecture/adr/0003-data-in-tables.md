---
status: accepted
date: 2026-11-26
---

# Keep the numbers, the texts and the boards in text files

Recorded at 1.0, after the decision was taken.

## Context and Problem Statement

The game's numbers (the slots, the crabs' pace, the prices, the colours, the gaps between sounds), its texts in two languages and its twenty boards are what changes most while a game is tuned. Where should they be kept?

## Considered Options

- Constants in code.
- A ScriptableObject per board and one for the numbers.
- Text files in `StreamingAssets`, read at start.
- Text files as text assets, loaded at start.

## Decision Outcome

Chosen option: "Text files as text assets, loaded at start", because a tuning change is one line in a diff, the data ships in the build with no extra web request, and a test can check every key and board. They are text assets on the composition root's serialized fields.

- `Config.csv` holds the numbers, and `Localization.csv` the texts, one column per language.
- `Boards.txt` holds the boards in a small text format, one block per board.
- They are parsed at start. A bad board or number fails loudly. A missing text shows as `[key]` until a test catches it.

Constants would put a tuning change in the code, and each try would wait for a recompile. ScriptableObjects can be tuned live in Play Mode, but a board kept in one would change as serialized YAML, which reads poorly in a diff. In `StreamingAssets`, each file would need a web request before the game could start.

### Consequences

- Good: a tuning change is one line, and it reads plainly in a diff.
- Good: EditMode tests check that every key in code has its row, that every text has both languages with the same placeholders, and that every shipped board parses and can be solved.
- Good: the data loads at once on the web.
- Bad: a mistake in a text file shows at load or in the tests, not in the Inspector.
- Bad: the files are read at start, so a change shows the next time Play Mode starts, not live as a ScriptableObject's would.
