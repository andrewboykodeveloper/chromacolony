using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Chromacolony.Tests.PlayMode
{
    public sealed class SmokeTests
    {
        private readonly string[] _dialogs = { "result", "pause", "buy", "settings", "note" };

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
        public IEnumerator TheGameBootsToItsMenuWithMusicAndNoDialogOpen()
        {
            Assert.That(_game.IsShown("menu"), Is.True);
            Assert.That(_game.IsShown("game"), Is.False);
            foreach (string dialog in _dialogs)
            {
                Assert.That(_game.IsShown(dialog), Is.False, $"the {dialog} dialog is closed");
            }

            Assert.That(_game.Text("title"), Is.Not.Empty.And.Not.StartsWith("["), "the title's text is in the localization table");
            Assert.That(_game.Text("play"), Is.Not.Empty.And.Not.StartsWith("["), "the play button's text is in the localization table");
            Assert.That(_game.MusicIsPlaying(), Is.True, "the music plays on the menu");
            Assert.That(_game.HasListener(), Is.True, "the scene has an audio listener");
            yield return null;
        }
    }
}
