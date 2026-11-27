using System.Collections;
using Chromacolony.Game;
using NUnit.Framework;
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
        public IEnumerator DialogClosedBeforeItFadesInStaysClosed()
        {
            VisualElement dialog = AddDialog(out ScreenHost screens);

            screens.Open(DialogName);
            screens.CloseDialog();
            yield return GameDriver.Frames(GameDriver.DialogOpenFrames);

            Assert.That(dialog.ClassListContains(UiClasses.Open), Is.False, "the closed dialog faded in");
            Assert.That(dialog.ClassListContains(UiClasses.Hidden), Is.True);
        }

        [UnityTest]
        public IEnumerator DialogClosedHalfwayStaysClosed()
        {
            VisualElement dialog = AddDialog(out ScreenHost screens);

            screens.Open(DialogName);
            yield return null;
            screens.CloseDialog();
            yield return GameDriver.Frames(GameDriver.DialogOpenFrames);

            Assert.That(dialog.ClassListContains(UiClasses.Open), Is.False, "the dialog closed between the two updates faded in");
            Assert.That(dialog.ClassListContains(UiClasses.Hidden), Is.True);
        }

        private VisualElement AddDialog(out ScreenHost screens)
        {
            var host = new VisualElement();
            var dialog = new VisualElement { name = DialogName };
            dialog.AddToClassList(UiClasses.Dialog);
            host.Add(dialog);
            _game.Single<UIDocument>().rootVisualElement.Add(host);
            screens = new ScreenHost();
            screens.Bind(host);
            return dialog;
        }
    }
}
