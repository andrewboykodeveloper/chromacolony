using System.Collections;
using Chromacolony.Unity;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Chromacolony.Tests
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
    }
}
