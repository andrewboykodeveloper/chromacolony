using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Chromacolony.Tests
{
    public sealed class SettingsTests
    {
        private const int FirstBoard = 0;

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
        public IEnumerator TheMenuSettingsChangeTheLanguageAndTheSoundAtOnceAndSaveThem()
        {
            Assert.That(_game.MusicIsPlaying(), Is.True, "the music plays on the menu");
            _game.Press("menu-settings");
            yield return null;
            Assert.That(_game.IsShown("settings"), Is.True);

            _game.Press("language-en");
            yield return null;
            string english = _game.Text("title");
            Assert.That(_game.Has("language-en", GameDriver.OnClass), Is.True);
            _game.Press("language-ru");
            yield return null;
            string russian = _game.Text("title");
            Assert.That(russian, Is.Not.EqualTo(english), "the title changes language at once");
            Assert.That(_game.Has("language-ru", GameDriver.OnClass), Is.True);
            Assert.That(_game.Has("language-en", GameDriver.OnClass), Is.False);

            _game.Press("music-switch");
            _game.Press("voice-switch");
            yield return null;
            Assert.That(_game.MusicIsPlaying(), Is.False, "the music stops at once");
            Assert.That(_game.Has("music-switch", GameDriver.OnClass), Is.False);
            Assert.That(_game.Has("voice-switch", GameDriver.OnClass), Is.False);
            Assert.That(_game.Has("sounds-switch", GameDriver.OnClass), Is.True, "the sounds switch is untouched");
            string saved = _game.CopySaves();

            _game.Press("settings-done");
            yield return null;
            Assert.That(_game.IsShown("settings"), Is.False);
            Assert.That(_game.IsShown("menu"), Is.True);

            yield return _game.Reopen(saved);
            Assert.That(_game.Text("title"), Is.EqualTo(russian), "the saved language is used after a restart");
            Assert.That(_game.MusicIsPlaying(), Is.False, "the saved silence is kept after a restart");
            _game.Press("menu-settings");
            yield return null;
            Assert.That(_game.Has("language-ru", GameDriver.OnClass), Is.True);
            Assert.That(_game.Has("music-switch", GameDriver.OnClass), Is.False);
            Assert.That(_game.Has("voice-switch", GameDriver.OnClass), Is.False);
            Assert.That(_game.Has("sounds-switch", GameDriver.OnClass), Is.True);
        }

        [UnityTest]
        public IEnumerator TheSettingsOpenedFromThePauseGoBackToItAndKeepTheBoardPaused()
        {
            _game.Press("play");
            yield return null;
            yield return _game.TapColumn(FirstBoard, 0, 0);
            _game.Press("hud-pause");
            yield return null;
            _game.Press("pause-settings");
            yield return null;
            Assert.That(_game.IsShown("settings"), Is.True);
            int crabs = _game.CrabsLeftInCrews();

            _game.Press("sounds-switch");
            yield return null;
            Assert.That(_game.Has("sounds-switch", GameDriver.OnClass), Is.False);
            _game.Press("language-en");
            yield return null;
            string english = _game.Text("board-name");
            _game.Press("language-ru");
            yield return null;
            Assert.That(_game.Text("board-name"), Is.Not.EqualTo(english), "the board's name changes language at once");
            string saved = _game.CopySaves();

            _game.Press("settings-done");
            yield return GameDriver.Frames(30);
            Assert.That(_game.IsShown("settings"), Is.False, "done closes the settings");
            Assert.That(_game.IsShown("pause"), Is.True, "done returns to the pause the settings came from");
            Assert.That(_game.CrabsLeftInCrews(), Is.EqualTo(crabs), "the board stays paused");

            yield return _game.Reopen(saved);
            _game.Press("menu-settings");
            yield return null;
            Assert.That(_game.Has("sounds-switch", GameDriver.OnClass), Is.False, "the sounds switch is saved");
        }
    }
}
