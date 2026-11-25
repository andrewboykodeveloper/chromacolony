using System.Collections;
using Chromacolony.Core;
using Chromacolony.Foundation.Ports;
using Chromacolony.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Chromacolony.Tests
{
    public sealed class GameFlowTest
    {
        [SetUp]
        public void FreshSave()
        {
            GameRoot.StorageOverride = new FakeSaveStorage();
        }

        [TearDown]
        public void RealTimeAgain()
        {
            Time.captureDeltaTime = 0f;
            GameRoot.StorageOverride = null;
        }

        [UnityTest]
        public IEnumerator TheSceneStartsOnTheMenu()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Bootstrap.unity");
            yield return null;
            var root = Object.FindFirstObjectByType<GameRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Phase, Is.EqualTo(GamePhase.Menu));
            Assert.That(Object.FindFirstObjectByType<AudioListener>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator TheFirstBoardPlaysThroughToItsWin()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Bootstrap.unity");
            yield return null;
            var root = Object.FindFirstObjectByType<GameRoot>();
            var document = Object.FindFirstObjectByType<UIDocument>();
            Press(document.rootVisualElement.Q<Button>(UiNames.Play));
            yield return null;
            Assert.That(root.Phase, Is.EqualTo(GamePhase.Playing));
            Assert.That(root.Screens.CurrentScreen, Is.EqualTo(UiNames.Game));
            Assert.That(root.Board.ColumnAt(root.Board.Layout.QueueAt(1, 0)), Is.EqualTo(1));
            Assert.That(root.Board.ColumnAt(root.Board.Layout.QueueAt(0, 2)), Is.EqualTo(0));

            var model = root.Model;
            model.Fast = true;
            Time.captureDeltaTime = 1f / 30f;
            for (var frame = 0; frame < 1800 && model.Phase == GamePhase.Playing; frame++)
            {
                for (var c = 0; c < model.ColumnCount; c++)
                {
                    if (model.Column(c).Count > 0 && model.FreeSlots() > 0)
                    {
                        model.TrySend(c);
                    }
                }

                yield return null;
            }

            Assert.That(model.Phase, Is.EqualTo(GamePhase.Won));
            Assert.That(model.Mosaic.Left, Is.EqualTo(0));
            Assert.That(root.Board.CrabsWalking, Is.GreaterThan(0), "the last crabs are still carrying pebbles home");
            Assert.That(root.Screens.CurrentDialog, Is.Null, "the result waits for them");
            for (var frame = 0; frame < 600 && root.Screens.CurrentDialog == null; frame++)
            {
                yield return null;
            }

            Assert.That(root.Board.CrabsWalking, Is.EqualTo(0));
            Assert.That(root.Screens.CurrentDialog, Is.EqualTo(UiNames.Result));
            Assert.That(document.rootVisualElement.Q<Button>(UiNames.Next).style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(root.Progress.Coins, Is.GreaterThan(0), "a win pays");
            Assert.That(root.Progress.Board, Is.EqualTo(1), "and leads to the next board");
            Assert.That(root.Saved, Is.EqualTo(root.Progress.ToText()), "and is saved at once, so a restart keeps the board and the coins");
        }

        [UnityTest]
        public IEnumerator TheFirstBoardTeachesTheTapAndStopsOnceItIsDone()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Bootstrap.unity");
            yield return null;
            var root = Object.FindFirstObjectByType<GameRoot>();
            var ui = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
            Press(ui.Q<Button>(UiNames.Play));
            yield return null;
            var hint = ui.Q<Label>(UiNames.Hint);
            Assert.That(hint.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(hint.text, Is.Not.Empty);
            root.Model.TrySend(0);
            yield return null;
            Assert.That(hint.style.display.value, Is.EqualTo(DisplayStyle.None), "the lesson ends with the first tap");
        }

        [UnityTest]
        public IEnumerator AHelperFromTheStockOpensASixthSlot()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Bootstrap.unity");
            yield return null;
            var root = Object.FindFirstObjectByType<GameRoot>();
            var ui = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
            Press(ui.Q<Button>(UiNames.Play));
            yield return null;
            var stock = root.Progress.Stock(Helper.Slot);
            Press(ui.Q<Button>(UiNames.HelperSlot));
            yield return null;
            Assert.That(root.Model.SlotCount, Is.EqualTo(6));
            Assert.That(root.Progress.Stock(Helper.Slot), Is.EqualTo(stock - 1));
            Press(ui.Q<Button>(UiNames.HelperSlot));
            yield return null;
            Assert.That(root.Model.SlotCount, Is.EqualTo(6), "one extra slot a board");
            Assert.That(root.Progress.Stock(Helper.Slot), Is.EqualTo(stock - 1), "and nothing paid for the second try");
        }

        [UnityTest]
        public IEnumerator PauseStopsTheBoardAndResumeGoesOn()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Bootstrap.unity");
            yield return null;
            var root = Object.FindFirstObjectByType<GameRoot>();
            var ui = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
            Press(ui.Q<Button>(UiNames.Play));
            yield return null;
            Time.captureDeltaTime = 1f / 30f;
            root.Model.TrySend(InReach(root.Model));
            yield return null;
            Press(ui.Q<Button>(UiNames.HudPause));
            Assert.That(root.Paused, Is.True);
            Assert.That(root.Screens.CurrentDialog, Is.EqualTo(UiNames.Pause));
            var left = root.Model.Mosaic.Left;
            for (var frame = 0; frame < 30; frame++)
            {
                yield return null;
            }

            Assert.That(root.Model.Mosaic.Left, Is.EqualTo(left), "no crab sets off in the pause");
            Press(ui.Q<Button>(UiNames.Resume));
            for (var frame = 0; frame < 30; frame++)
            {
                yield return null;
            }

            Assert.That(root.Paused, Is.False);
            Assert.That(root.Model.Mosaic.Left, Is.LessThan(left));
        }

        [UnityTest]
        public IEnumerator TheMenusSettingsChangeTheLanguageAndTheSoundAtOnceAndKeepThem()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Bootstrap.unity");
            yield return null;
            var root = Object.FindFirstObjectByType<GameRoot>();
            var ui = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
            var screens = root.Screens;
            Assert.That(MusicClip(root), Is.Not.Null, "the music plays from the menu");
            Press(ui.Q<Button>(UiNames.MenuSettings));
            yield return null;
            Assert.That(screens.CurrentDialog, Is.EqualTo(UiNames.Settings));

            var other = root.Language == "ru" ? "en" : "ru";
            var title = ui.Q<Label>(UiNames.Title).text;
            Press(ui.Q<Button>(other == "ru" ? UiNames.LanguageRu : UiNames.LanguageEn));
            yield return null;
            Assert.That(root.Language, Is.EqualTo(other));
            Assert.That(ui.Q<Label>(UiNames.Title).text, Is.Not.EqualTo(title), "the menu's words change at once");
            Assert.That(root.Saved, Does.Contain("language=" + other));

            Press(ui.Q<Button>(UiNames.MusicSwitch));
            Press(ui.Q<Button>(UiNames.VoiceSwitch));
            yield return null;
            Assert.That(MusicClip(root), Is.Null, "the music stops at once");
            Assert.That(root.Progress.Music, Is.False);
            Assert.That(root.Progress.Voice, Is.False);
            Assert.That(root.Progress.Sound, Is.True, "the other switches stay as they were");
            Assert.That(root.Saved, Does.Contain("music=0").And.Contain("voice=0"), "and the save keeps them for the next start");

            Press(ui.Q<Button>(UiNames.SettingsDone));
            yield return null;
            Assert.That(screens.CurrentDialog, Is.Null);
            Assert.That(screens.CurrentScreen, Is.EqualTo(UiNames.Menu));
        }

        [UnityTest]
        public IEnumerator TheSettingsOpenedFromThePauseGoBackToIt()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Bootstrap.unity");
            yield return null;
            var root = Object.FindFirstObjectByType<GameRoot>();
            var ui = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
            Press(ui.Q<Button>(UiNames.Play));
            yield return null;
            Press(ui.Q<Button>(UiNames.HudPause));
            Press(ui.Q<Button>(UiNames.PauseSettings));
            yield return null;
            Assert.That(root.Screens.CurrentDialog, Is.EqualTo(UiNames.Settings));
            Press(ui.Q<Button>(UiNames.SoundsSwitch));
            yield return null;
            Assert.That(root.Progress.Sound, Is.False);
            Assert.That(root.Saved, Does.Contain("sound=0"));

            var board = ui.Q<Label>(UiNames.BoardName).text;
            Press(ui.Q<Button>(root.Language == "ru" ? UiNames.LanguageEn : UiNames.LanguageRu));
            yield return null;
            Assert.That(ui.Q<Label>(UiNames.BoardName).text, Is.Not.EqualTo(board), "the board's words change at once too");

            Press(ui.Q<Button>(UiNames.SettingsDone));
            yield return null;
            Assert.That(root.Screens.CurrentDialog, Is.EqualTo(UiNames.Pause), "Done goes back to the pause");
            Assert.That(root.Paused, Is.True);
        }

        [UnityTest]
        public IEnumerator TheBackKeyTakesTheSettingsBackToThePauseAndKeepsAPickWaiting()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Bootstrap.unity");
            yield return null;
            var root = Object.FindFirstObjectByType<GameRoot>();
            var ui = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
            Press(ui.Q<Button>(UiNames.Play));
            yield return null;
            root.Model.TrySend(InReach(root.Model));
            yield return null;
            Press(ui.Q<Button>(UiNames.HelperBack));
            Assert.That(root.PickingCrew, Is.True, "the back helper waits for a crew to pick");
            Press(ui.Q<Button>(UiNames.HudPause));
            Press(ui.Q<Button>(UiNames.PauseSettings));
            yield return null;

            root.Back();
            Assert.That(root.Screens.CurrentDialog, Is.EqualTo(UiNames.Pause), "back from the settings is the pause they came from");
            Assert.That(root.PickingCrew, Is.True);
            root.Back();
            Assert.That(root.Screens.CurrentDialog, Is.Null, "then the board resumes");
            Assert.That(root.Paused, Is.False);
            Assert.That(root.PickingCrew, Is.True, "with its pick still waiting");
            root.Back();
            Assert.That(root.PickingCrew, Is.False, "and only then is the pick called off");
        }

        [UnityTest]
        public IEnumerator ACardThatAsksThePlayerSomethingHoldsTheBoard()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Bootstrap.unity");
            yield return null;
            var root = Object.FindFirstObjectByType<GameRoot>();
            var ui = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
            Press(ui.Q<Button>(UiNames.Play));
            yield return null;
            Time.captureDeltaTime = 1f / 30f;
            root.Model.TrySend(InReach(root.Model));
            yield return null;

            var none = new Prices(coinsPerWin: 0, startStock: 0, slot: 1, shuffle: 1, back: 1);
            while (root.Progress.Stock(Helper.Shuffle) > 0)
            {
                root.Progress.Pay(Helper.Shuffle, none);
            }

            Press(ui.Q<Button>(UiNames.HelperShuffle));
            yield return null;
            Assert.That(root.Screens.CurrentDialog, Is.EqualTo(UiNames.Note));
            var left = root.Model.Mosaic.Left;
            for (var frame = 0; frame < 30; frame++)
            {
                yield return null;
            }

            Assert.That(root.Model.Mosaic.Left, Is.EqualTo(left), "no crab works while the card waits for an answer");
            Press(ui.Q<Button>(UiNames.NoteOk));
            for (var frame = 0; frame < 30; frame++)
            {
                yield return null;
            }

            Assert.That(root.Model.Mosaic.Left, Is.LessThan(left), "and the board goes on once it is answered");
        }

        private static AudioClip MusicClip(GameRoot root)
        {
            foreach (var source in root.GetComponents<AudioSource>())
            {
                if (source.loop)
                {
                    return source.clip;
                }
            }

            return null;
        }

        private static int InReach(GameModel model)
        {
            for (var c = 0; c < model.ColumnCount; c++)
            {
                if (model.Column(c).Count > 0 && model.Mosaic.AnyInReach(model.Column(c)[0].Color))
                {
                    return c;
                }
            }

            Assert.Fail("the board opens with no front crew in reach");
            return -1;
        }

        private static void Press(Button button)
        {
            using (var e = NavigationSubmitEvent.GetPooled())
            {
                e.target = button;
                button.SendEvent(e);
            }
        }
    }
}
