# Changelog

This file lists the notable changes to Chromacolony. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the versions follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

The first release lists its changes against the game as it was imported in the first commit.

## [Unreleased]

## [1.0.0] - 2026-11-26

The first release: twenty boards in English and Russian, built for the web.

### Added

- EditMode tests of the rules, with a solver over every shipped board and a deterministic replay.
- A web build command, from the menu or from a shell.
- An architecture overview, three decision records and this changelog.

### Changed

- The PlayMode tests play the game through its interface, and press only the buttons a player could see and reach, instead of calling the game root's members.
- The project lives under `Assets/_Project`, with one assembly per folder: the rules, the game, the editor and the two test suites.
- The rules raise C# events, and the core decides the tutorial's lesson and which helpers the player can take.
- One composition root builds the game, and presenters run the menu, the settings, a board, the helpers and the result.
- The crab is a prefab, and the board's crews, labels and mosaic are views of their own.
- While a board is played, no strings or particle objects are made each frame.
- On a screen wider than the beach picture, the sides show the tinted picture on every device. Before, a blurred copy ran on some devices only.
- The whole project follows one code style, set in `.editorconfig`.

### Fixed

- A board with an empty column loaded, then failed when it was won. Such a board is now rejected when the boards load.
- After a shuffle, the back helper returned a crew to the column it was first dealt into, not the one it was sent from.
- A press on a button also reached the board, so it called off a back helper that waited for a crew.
- A second press on the waiting back helper kept it waiting. It now calls the pick off.
- The lesson on places still said five after the extra place was taken.
- A save with no version line was overwritten. It is now set aside, and every save problem is logged.
- A save that kept failing logged a warning at every attempt. It is now reported once until a write succeeds.
- Reloading the web page lost the progress. The page now keeps the saves in the browser's storage.
- Switching the `Game` object off and on built a second game. The game is now built once, and its interface binds again to the tree the document rebuilds.
- A dialog closed right after it opened showed without its fade the next time.
- The objects and textures the views made outlived the game.

### Removed

- Code, images, style rules and packages the game did not use, unused Android and iOS texture overrides, and the input actions asset from Unity's project template.

[Unreleased]: https://github.com/andrewboykodeveloper/chromacolony/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/andrewboykodeveloper/chromacolony/releases/tag/v1.0.0
