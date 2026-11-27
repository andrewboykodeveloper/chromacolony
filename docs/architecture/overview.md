# Architecture overview

This page shows how Chromacolony is put together and what one frame does. Paths are relative to `Assets/_Project`.

## The scene and the assemblies

`Scenes/Game.unity` holds two objects:

- the main camera;
- `Game`, which carries the `GameRoot` and the `UIDocument`.

Everything else is made at run time, under four roots: the beach, the board, the crabs and the effects.

| Assembly | Folder | Holds |
|---|---|---|
| `Chromacolony.Core` | `Scripts/Core` | The rules, with no engine references (`noEngineReferences`). |
| `Chromacolony.Game` | `Scripts/Game` | The composition root, the presenters, the views and the services. |
| `Chromacolony.Editor` | `Scripts/Editor` | The import rules and the web build command. |
| `Chromacolony.Tests.EditMode` | `Tests/EditMode` | The rules, the data and the hit tests, without Play Mode. |
| `Chromacolony.Tests.PlayMode` | `Tests/PlayMode` | The game played through its interface. |

## The rules

`GameModel` plays one board.

- **Phases:** `Menu`, `Playing`, `Won` and `Stuck`. A board starts in `Playing` and ends in `Won` or `Stuck`. The extra place or the back helper takes a stuck board back to `Playing`, and leaving a board goes to `Menu`.
- **A tap** is `TrySend(column)`. The crew at the front of the column takes a free place, or the model raises `SendRefused`. A place is a slot in the code: `SlotCount`, `SlotAdded`.
- **Time** enters only through `Tick(dt)`, which `GameTicker` calls 60 times a second. At double speed (`Fast`), each tick counts twice. As time passes, the crews in places send their crabs, one at a time, to the pebbles of their colour that the crabs can reach.
- **Chance** enters only through an injected `IRandom`, for the shuffle helper. It is a PCG32 that the game seeds from the clock and the tests from a fixed number.

The model raises typed C# events: `BoardStarted`, `CrewSent`, `CrabSent`, `CrewWaiting`, `CrewDone`, `CrewRevealed`, `SendRefused`, `SlotAdded`, `QueueShuffled`, `CrewBack`, `BoardWon`, `BoardStuck` and `BoardLeft`, each once the model's state is final. Views and presenters may subscribe in any order: no presenter reads a view that follows the model when an event comes, and the board view aims its pointer and moves its pieces each frame.

Around the model:

- `BoardSession` starts a board and keeps its `Tutorial` up to date. When the board ends, it pays a win into the progress and raises `Ended`; when the player leaves it, it raises `Left`.
- `Tutorial` holds the lesson of a board and the column its pointer shows (`PointedColumn`).
- `HelperShop` says which helpers the player can take, from the board and the player's stock and coins. It also takes them: `TryUse` pays for the extra slot or the shuffle and applies it, or starts the back helper's pick, and `TryBack` ends the pick, sending the picked crew back, paid then.
- `Progress` holds the coins, the stock, the next board and the settings.
- `SaveStore` writes them to the saves.

## Wiring

`GameRoot` is the one composition root. It wires everything by hand, through constructors: no singleton, no service locator, no container.

- **`Awake`** reads the tables and builds the core objects, the views, the `AudioService` and five presenters. A presenter takes core objects and views, never another presenter. The root wires the buttons that lead from one screen to the next, and saves when a helper is used or a board is won.
- **`OnEnable`** binds the interface: the screens, the presenters' elements and handlers, and the crews' labels. The UI document builds its tree anew whenever its object is enabled, so this runs again then, and `OnDisable` takes the handlers back.
- **`Start`** shows the menu.
- **`OnDestroy`** destroys what the views made at run time, the textures included.

| Presenter | Owns |
|---|---|
| `MenuPresenter` | The menu and the crab that dances on it. |
| `SettingsPresenter` | The settings, the language and the way back to the pause. |
| `BoardPresenter` | A board in play: its start, the taps, the pick of a crew to send back, the tutorial's hint, the speed and the pause. |
| `HelperPresenter` | The three helpers, and the buy and no-coins dialogs. |
| `ResultPresenter` | The celebration, the coins' flight and the result dialog. |

### A frame

`GameRoot.Update` keeps one order:

1. It fits the camera, advances the audio's clock and the beach, and shows or hides the menu's crab.
2. It reads the back key. A paused board, or one held by the buy or no-coins dialog, stops here.
3. It hands a press to the board presenter, as a point in the world, unless the press is on a button, the menu or a dialog.
4. The ticker runs the model.
5. The crabs walk and dance. On a board, the board view:
   - rebuilds the picture after a win;
   - moves the crews with their labels and ropes;
   - pulses the glow on the pebbles a waiting crew needs;
   - nods the claw at its column.
6. The effects play.
7. The result runs: the coins fly, and once the board view is still, the dialog opens.

## Screens and dialogs

The interface is one layout, `UI/Main.uxml`, and one style sheet, `UI/Theme.uss`.

- `ScreenHost` shows one screen and at most one dialog by toggling the `hidden` class.
  - The screens are `menu` and `game`.
  - The dialogs are `pause`, `settings`, `buy`, `note` (for no coins) and `result`.
- A dialog fades in by its `open` class, added on the panel's second scheduler update after the dialog shows, so that the panel styles the shown dialog without it first.
- Element names are kept in `UiNames` and class names in `UiClasses`. An EditMode test checks both against the layout and the style sheet.

## Views and pooling

| View | Draws |
|---|---|
| `BoardView` | The castle, the places, the claw and the celebration. It builds a board on `BoardStarted`, and passes `SlotAdded` and `CrabSent` on to the crews, the mosaic and the crabs. A board's objects are built under one root and destroyed with it. |
| `CrewsView` | The crews in their columns and places: their buckets and signs, the crab that sits on a waiting crew, and the ropes between crews. It follows the crews' own events. |
| `MosaicView` | The pebbles, their glow, and the picture that puts itself together on a win. |
| `CrewLabels` | The crews' counts and the columns' "+N" labels, as interface elements placed from the camera. |
| `CrabView` | The crabs. Each is a copy of `Prefabs/Crab.prefab`, whose `CrabRig` names the parts that move. A crab goes back to a pool when it reaches the castle. |
| `Effects` | Puffs and sparkles, from a pool of sprite renderers. |
| `BeachView` | The beach behind the menu and the board. |

`SortingOrder` lists every sprite's draw order, from back to front. While a board is played, the views make no new objects or strings each frame. The crabs and the effects come from pools, and each count and "+N" text is made once.

## Data

The data loads at start, from text assets on the composition root's serialized fields:

- the numbers, from `Data/Config.csv`;
- the texts, from `Data/Localization.csv`, in English and Russian;
- the twenty boards, from `Data/Boards.txt`.

`Data/GameContent.asset` holds the sprites, the crab prefab and the clips, also on the composition root. Nothing loads from `Resources`.

A bad board, a bad number or a missing config key throws at start. A missing text shows as `[key]`, and a bad colour as magenta. EditMode tests check every key, both languages of every text, and every board.

## Saves

`SaveStore` writes the progress under a version line (`v1`). It can run a registered migration for an older version on load. There is one version so far, so none is registered. Within it, `Progress` also reads an earlier body with a single sound switch, which then sets the music and the voice as well.

- A file without a version line is copied aside under a free `.unreadable` name before the game starts afresh.
- A file from a newer version is kept, untouched.
- Every problem is logged.

`FileSaveStorage` writes into the folder that `SaveFolder` names, to a temporary file that then replaces the old one. The web page keeps the saves folder in the browser's storage.

## Tests

- **EditMode:** the rules, a solver over every shipped board, and a replay that must end at the same tick with the same outcome. Also the saves, the tables and keys, the crab prefab, and `BoardLayout`'s hit tests.
- **PlayMode:** boots the scene and plays it through its interface.
  - It presses buttons by their names in the layout, as a key does, and only those a player could see and reach. Run from a shell, the interface gets no mouse input.
  - It taps the columns, and presses over a button, through a virtual mouse. It presses Escape on a virtual keyboard.
  - It covers the menu, a won board, the lessons, the pause, the helpers and the settings.
  - It also covers switching the game object off and on, and destroying the game.
- **Isolation:** the PlayMode tests save in a folder of their own under the temporary cache, where `SaveFolder` points the game, and mute the listener.
