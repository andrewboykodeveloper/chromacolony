---
status: accepted
date: 2026-11-26
---

# Build the interface with UI Toolkit

Recorded at 1.0, after the decision was taken.

## Context and Problem Statement

The interface is a handful of screens and dialogs over the world: the menu, the board's HUD and helpers, the pause, the settings, the buy and no-coins dialogs, and the result. It runs in a browser, laid out in portrait, inside the device's safe area. Which UI system should draw it?

## Considered Options

- uGUI, with a canvas and a prefab per dialog.
- UI Toolkit, with UXML and USS.

## Decision Outcome

Chosen option: "UI Toolkit, with UXML and USS", because one layout and one style sheet hold every screen and dialog as text that diffs plainly and that a test can check, with no canvas or prefab per dialog.

- `Main.uxml` holds every screen and dialog, and `Theme.uss` their look, with the interface's colours as USS variables.
- A screen or dialog shows and hides by the `hidden` class. Presenters look up their elements by name, once per binding.
- The crews' counts float over the world as labels in one layer, placed from the camera each frame.

uGUI would spread the same interface over a canvas and several prefabs, whose changes read poorly in a diff.

### Consequences

- Good: a change to the layout or the look reads plainly in a diff.
- Good: an EditMode test checks the element names against the layout, and the class names against the style sheet.
- Bad: the UI document builds its tree again each time its game object is enabled. So the game root binds the presenters again in its `OnEnable`, and they show their state on the new tree.
