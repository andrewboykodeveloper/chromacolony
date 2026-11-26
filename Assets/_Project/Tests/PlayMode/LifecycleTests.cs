using System.Collections;
using Chromacolony.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Chromacolony.Tests.PlayMode
{
    public sealed class LifecycleTests
    {
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
        public IEnumerator SwitchingGameRootOffAndOnAgainBuildsTheGameOnce()
        {
            GameRoot root = _game.Single<GameRoot>();
            root.enabled = false;
            yield return null;
            root.enabled = true;
            yield return null;

            Assert.That(_game.LoopingSources(), Is.EqualTo(1), "one music source plays");
            _game.Press("menu-settings");
            yield return null;
            _game.Press("music-switch");
            yield return null;
            Assert.That(_game.MusicIsPlaying(), Is.False, "one press of the switch stops the music");
        }

        [UnityTest]
        public IEnumerator SwitchingTheGameObjectOffAndOnAgainKeepsTheInterfaceWorking()
        {
            _game.Press("play");
            yield return GameDriver.Frames(2);
            int crabs = _game.CrabsLeftInCrews();
            int coins = _game.Number("coins");
            string badge = _game.Text("helper-slot-badge");
            string hint = _game.Text("hint");
            Assert.That(_game.VoiceIsPlaying(), Is.True, "the first board says hello");
            yield return SwitchTheGameOffAndOn();

            Assert.That(_game.IsShown("game"), Is.True, "the board is still on the screen");
            Assert.That(_game.IsShown("menu"), Is.False);
            Assert.That(_game.Text("board-name"), Is.Not.Empty);
            Assert.That(_game.CrabsLeftInCrews(), Is.EqualTo(crabs), "the crews' counts are on the new tree");
            Assert.That(_game.Number("coins"), Is.EqualTo(coins));
            Assert.That(_game.Text("helper-slot-badge"), Is.EqualTo(badge));
            Assert.That(_game.Text("hint"), Is.EqualTo(hint));
            Assert.That(_game.VoiceIsPlaying(), Is.False, "the voice does not say its last line again");
            _game.Press("hud-pause");
            yield return null;
            Assert.That(_game.IsShown("pause"), Is.True, "the pause button opens the pause");
            _game.Press("pause-menu");
            yield return null;
            Assert.That(_game.IsShown("menu"), Is.True, "the pause goes back to the menu");
        }

        [UnityTest]
        public IEnumerator SwitchingTheGameObjectOffAndOnAgainKeepsAnOpenDialogOpen()
        {
            _game.Press("play");
            yield return null;
            _game.Press("hud-pause");
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(_game.Has("pause", "open"), Is.True, "the pause has faded in");

            yield return SwitchTheGameOffAndOn();

            Assert.That(_game.IsShown("pause"), Is.True, "the pause is still open");
            Assert.That(_game.Has("pause", "open"), Is.True, "the pause shows at once, without fading in again");
            _game.Press("resume");
            yield return null;
            Assert.That(_game.IsShown("pause"), Is.False, "the resume button closes the pause");
        }

        private IEnumerator SwitchTheGameOffAndOn()
        {
            GameObject game = _game.Single<GameRoot>().gameObject;
            game.SetActive(false);
            yield return null;
            game.SetActive(true);
            yield return null;
        }
    }
}
