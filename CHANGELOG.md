# Changelog

This file lists the changes a player of Chromacolony can see. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the versions follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.1] - 2026-11-27

The same game as 1.0.0: no change a player can see. The code under it was reworked.

## [1.0.0] - 2026-11-26

The first release: twenty boards in English and Russian, played in a web browser. The changes below are against the game as the first commit imported it.

### Changed

- On a screen wider than the beach picture, the sides show the tinted picture on every device. Before, a blurred copy showed on some devices only.

### Fixed

- Reloading the web page lost the progress. The page now keeps the saves in the browser's storage.
- After a shuffle, the back helper returned a crew to the column it was first dealt into, not the one it was sent from.
- A press on a button also reached the board, so it called off a back helper that waited for a crew.
- A second press on the waiting back helper kept it waiting. It now calls the pick off.
- The lesson on the second board still said "Only 5 places" after "+1 place for this board" made it six.
- A dialog closed right after it opened showed without its fade the next time.

[Unreleased]: https://github.com/andrewboykodeveloper/chromacolony/compare/v1.0.1...HEAD
[1.0.1]: https://github.com/andrewboykodeveloper/chromacolony/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/andrewboykodeveloper/chromacolony/releases/tag/v1.0.0
