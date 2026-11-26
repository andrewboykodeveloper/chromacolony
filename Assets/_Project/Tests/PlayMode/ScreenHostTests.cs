using System.Collections;
using Chromacolony.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Chromacolony.Tests.PlayMode
{
    public sealed class ScreenHostTests
    {
        private const string DialogName = "test-dialog";

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
        public IEnumerator ADialogClosedBeforeItFadesInStaysClosed()
        {
            var host = new VisualElement();
            var dialog = new VisualElement { name = DialogName };
            dialog.AddToClassList(UiClasses.Dialog);
            host.Add(dialog);
            _game.Single<UIDocument>().rootVisualElement.Add(host);
            var screens = new ScreenHost();
            screens.Bind(host);

            screens.Open(DialogName);
            screens.CloseDialog();
            yield return new WaitForSecondsRealtime(0.2f);

            Assert.That(dialog.ClassListContains(UiClasses.Open), Is.False, "the closed dialog faded in");
            Assert.That(dialog.ClassListContains(UiClasses.Hidden), Is.True);
        }
    }
}
