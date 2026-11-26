using System.Collections;
using Chromacolony.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Chromacolony.Tests.PlayMode
{
    public sealed class SafeAreaTests
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

        // A safe area cut by a tenth at the top and the sides and by a twentieth at the bottom, as a notch and a home bar would.
        [Test]
        public void TheInsetsAreTheSafeAreasEdgesInThePanelsUnits()
        {
            VisualElement root = _game.Single<UIDocument>().rootVisualElement;
            Rect panel = root.worldBound;
            Assume.That(panel.width, Is.Not.EqualTo(Screen.width).Within(1f),
                "the panel does not scale this screen, so its units cannot be told from pixels");
            var screen = new Vector2(Screen.width, Screen.height);
            var safe = new Rect(screen.x * 0.1f, screen.y * 0.05f, screen.x * 0.8f, screen.y * 0.85f);

            (float top, float bottom, float left, float right) = SafeAreas.Insets(root.panel, safe, screen);

            Assert.That(top, Is.EqualTo(panel.height * 0.1f).Within(0.5f), "the top");
            Assert.That(bottom, Is.EqualTo(panel.height * 0.05f).Within(0.5f), "the bottom");
            Assert.That(left, Is.EqualTo(panel.width * 0.1f).Within(0.5f), "the left");
            Assert.That(right, Is.EqualTo(panel.width * 0.1f).Within(0.5f), "the right");
        }
    }
}
