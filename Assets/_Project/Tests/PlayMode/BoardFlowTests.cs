using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Chromacolony.Tests.PlayMode
{
    public sealed class BoardFlowTests
    {
        private const int FirstBoard = 0;
        private const int MaxFramesToWin = 4000;
        private const string MoreClass = "crew-more";

        private GameDriver _game;

        [UnitySetUp]
        public IEnumerator BootTheGame()
        {
            _game = new GameDriver();
            yield return _game.Boot();
        }

        [UnityTearDown]
        public IEnumerator QuitTheGame()
        {
            yield return _game.Quit();
        }

        [UnityTest]
        public IEnumerator TappingTheColumnsWinsTheFirstBoardAndTheWinIsSavedAtOnce()
        {
            _game.Press("play");
            yield return null;
            string firstBoard = _game.Text("board-name");
            _game.Press("hud-speed");
            int columns = _game.ColumnCount(FirstBoard);
            bool lastCrabSeen = false;
            for (int frame = 0, column = 0; frame < MaxFramesToWin && !_game.IsShown("result"); frame += 2, column = (column + 1) % columns)
            {
                yield return _game.TapColumn(FirstBoard, column, 0);
                if (!lastCrabSeen && _game.CrabsLeftInCrews() == 0)
                {
                    lastCrabSeen = true;
                    Assert.That(_game.IsShown("result"), Is.False, "the result waits for the last crabs to carry their pebbles home");
                }
            }

            Assert.That(lastCrabSeen, Is.True, "every crab of the board set off");
            Assert.That(_game.IsShown("result"), Is.True, "the board ends");
            Assert.That(_game.IsShown("next"), Is.True, "the result offers the next board, as after a win");
            Assert.That(_game.IsShown("retry"), Is.False, "the result offers no retry, as after a win");
            int coins = _game.Number("coins");
            Assert.That(coins, Is.GreaterThan(0), "a win pays coins");
            string savedAtTheWin = _game.CopySaves();

            _game.Press("next");
            yield return null;
            string secondBoard = _game.Text("board-name");
            Assert.That(secondBoard, Is.Not.EqualTo(firstBoard), "the next board starts");

            yield return _game.Reopen(savedAtTheWin);
            _game.Press("play");
            yield return null;
            Assert.That(_game.Text("board-name"), Is.EqualTo(secondBoard), "the save written at the win leads to the second board");
            Assert.That(_game.Number("coins"), Is.EqualTo(coins), "the save written at the win keeps its coins");
        }

        [UnityTest]
        public IEnumerator TheFirstTapEndsTheLessonOfTheFirstBoard()
        {
            _game.Press("play");
            yield return null;
            Assert.That(_game.IsShown("hint"), Is.True);
            Assert.That(_game.Text("hint"), Is.Not.Empty);

            yield return _game.TapColumn(FirstBoard, 0, 0);
            yield return null;

            Assert.That(_game.IsShown("hint"), Is.False);
        }

        [UnityTest]
        public IEnumerator ATapOnACrewDeepInAColumnSendsItsFrontCrew()
        {
            _game.Press("play");
            yield return null;
            int crabs = _game.CrabsLeftInCrews();

            yield return _game.TapColumn(FirstBoard, 0, 2);
            yield return GameDriver.Frames(10);

            Assert.That(_game.IsShown("hint"), Is.False, "the tap sent a crew, which ends the first lesson");
            Assert.That(_game.CrabsLeftInCrews(), Is.LessThan(crabs), "the sent crew's crabs set off");
        }

        [UnityTest]
        public IEnumerator AColumnCountsOnlyTheCrewsWaitingBehindItsVisibleOnes()
        {
            _game.Press("play");
            yield return null;
            Assert.That(_game.ShownTexts(MoreClass), Is.EqualTo(new[] { "+1", "+1" }), "each column of four shows one more crew");

            yield return _game.TapColumn(FirstBoard, 0, 0);
            yield return null;
            Assert.That(_game.ShownTexts(MoreClass), Is.EqualTo(new[] { "+1" }), "a column of three shows no count");

            yield return _game.TapColumn(FirstBoard, 0, 0);
            yield return null;
            Assert.That(_game.ShownTexts(MoreClass), Is.EqualTo(new[] { "+1" }), "a column of two shows no count");
        }

        [UnityTest]
        public IEnumerator ThePauseHoldsTheCrabsAndResumeLetsThemGoOn()
        {
            _game.Press("play");
            yield return null;
            yield return _game.TapColumn(FirstBoard, 0, 0);
            yield return GameDriver.Frames(5);

            _game.Press("hud-pause");
            yield return null;
            Assert.That(_game.IsShown("pause"), Is.True);
            int crabs = _game.CrabsLeftInCrews();
            yield return GameDriver.Frames(30);
            Assert.That(_game.CrabsLeftInCrews(), Is.EqualTo(crabs), "no crab sets off during the pause");

            _game.Press("resume");
            yield return GameDriver.Frames(30);
            Assert.That(_game.IsShown("pause"), Is.False);
            Assert.That(_game.CrabsLeftInCrews(), Is.LessThan(crabs), "the crabs set off again after the pause");
        }

        [UnityTest]
        public IEnumerator TheExtraSlotCanBeTakenOnlyOnceOnABoard()
        {
            _game.Press("play");
            yield return null;
            int stock = _game.Number("helper-slot-badge");

            _game.Press("helper-slot");
            yield return null;
            Assert.That(_game.Number("helper-slot-badge"), Is.EqualTo(stock - 1), "the extra slot comes from the stock");
            Assert.That(_game.Has("helper-slot", GameDriver.OffClass), Is.True, "the helper is spent for this board");

            _game.Press("helper-slot");
            yield return null;
            Assert.That(_game.Number("helper-slot-badge"), Is.EqualTo(stock - 1), "a second try takes nothing from the stock");
        }

        [UnityTest]
        public IEnumerator ANoteHoldsTheBoardUntilItIsClosed()
        {
            _game.Press("play");
            yield return null;
            yield return _game.TapColumn(FirstBoard, 0, 0);
            for (int stock = _game.Number("helper-shuffle-badge"); stock > 0; stock--)
            {
                _game.Press("helper-shuffle");
                yield return null;
            }

            Assert.That(_game.Has("helper-shuffle-badge", GameDriver.PriceClass), Is.True, "the stock is spent and the badge shows the price");
            _game.Press("helper-shuffle");
            yield return null;
            Assert.That(_game.IsShown("note"), Is.True, "a new player cannot pay the price, and a note says so");

            int crabs = _game.CrabsLeftInCrews();
            yield return GameDriver.Frames(30);
            Assert.That(_game.CrabsLeftInCrews(), Is.EqualTo(crabs), "no crab sets off while the note is open");

            _game.Press("note-ok");
            yield return GameDriver.Frames(30);
            Assert.That(_game.CrabsLeftInCrews(), Is.LessThan(crabs), "the crabs set off again once the note is closed");
        }

        // A pause would leave the pick waiting, so the test holds whether or not the interface also takes the press.
        [UnityTest]
        public IEnumerator AMousePressOnAButtonDoesNotReachTheBoard()
        {
            _game.Press("play");
            yield return null;
            yield return _game.TapColumn(FirstBoard, 0, 0);
            _game.Press("helper-back");
            yield return null;
            Assert.That(_game.Has("helper-back", GameDriver.OnClass), Is.True, "the back helper waits for a crew to be picked");

            yield return _game.PressMouseOver("hud-pause");
            Assert.That(_game.Has("helper-back", GameDriver.OnClass), Is.True, "a press on the pause button leaves the pick waiting");
        }

        // The game hands a press to the board unless the interface can pick something there.
        [UnityTest]
        public IEnumerator OnlyTheButtonsOnTheGameScreenTakeThePointer()
        {
            _game.Press("play");
            yield return GameDriver.Frames(10);
            Assert.That(_game.PointerTakersOutsideButtons("game", "labels"), Is.Empty);
        }

        [UnityTest]
        public IEnumerator TheBackKeyStepsBackOneLayerAtATimeAndCancelsAPickLast()
        {
            _game.Press("play");
            yield return null;
            yield return _game.TapColumn(FirstBoard, 0, 0);
            _game.Press("helper-back");
            yield return null;
            Assert.That(_game.Has("helper-back", GameDriver.OnClass), Is.True, "the back helper waits for a crew to be picked");
            _game.Press("hud-pause");
            yield return null;
            _game.Press("pause-settings");
            yield return null;
            Assert.That(_game.IsShown("settings"), Is.True);
            int crabs = _game.CrabsLeftInCrews();

            yield return _game.PressBack();
            Assert.That(_game.IsShown("settings"), Is.False, "the first back closes the settings");
            Assert.That(_game.IsShown("pause"), Is.True, "the first back returns to the pause the settings came from");
            Assert.That(_game.CrabsLeftInCrews(), Is.EqualTo(crabs), "the board stays paused under the pause");
            Assert.That(_game.Has("helper-back", GameDriver.OnClass), Is.True, "the pick still waits after the first back");

            yield return _game.PressBack();
            yield return GameDriver.Frames(10);
            Assert.That(_game.IsShown("pause"), Is.False, "the second back closes the pause");
            Assert.That(_game.CrabsLeftInCrews(), Is.LessThan(crabs), "the board plays on after the second back");
            Assert.That(_game.Has("helper-back", GameDriver.OnClass), Is.True, "the pick still waits after the second back");

            yield return _game.PressBack();
            Assert.That(_game.Has("helper-back", GameDriver.OnClass), Is.False, "the third back calls the pick off");
        }
    }
}
