using System.Collections;
using Chromacolony.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using MouseButton = UnityEngine.InputSystem.LowLevel.MouseButton;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Chromacolony.Tests.PlayMode
{
    public sealed class PointerInputTest
    {
        private InputSettings.BackgroundBehavior _background;
        private InputSettings.EditorInputBehaviorInPlayMode _editorInput;

        [SetUp]
        public void IgnoreFocus()
        {
            _background = InputSystem.settings.backgroundBehavior;
            _editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        }

        [TearDown]
        public void RestoreFocus()
        {
            InputSystem.settings.backgroundBehavior = _background;
            InputSystem.settings.editorInputBehaviorInPlayMode = _editorInput;
        }

        [UnityTest]
        public IEnumerator AMouseClickIsOnePressWithYDown()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                mouse.MakeCurrent();
                var input = new PointerInput();
                InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(100f, Screen.height - 40f) }.WithButton(MouseButton.Left, true));
                yield return null;
                Assert.That(input.TryGetPress(out var x, out var y), Is.True);
                Assert.That(x, Is.EqualTo(100f).Within(0.5f));
                Assert.That(y, Is.EqualTo(40f).Within(0.5f));

                yield return null;
                Assert.That(input.TryGetPress(out _, out _), Is.False, "a button held down is one press");
                InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(100f, Screen.height - 40f) });
                yield return null;
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
            }
        }

        [UnityTest]
        public IEnumerator ATouchIsAPressToo()
        {
            var screen = InputSystem.AddDevice<Touchscreen>();
            try
            {
                screen.MakeCurrent();
                var input = new PointerInput();
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 1, phase = TouchPhase.Began, position = new Vector2(200f, Screen.height - 300f) });
                yield return null;
                Assert.That(input.TryGetPress(out var x, out var y), Is.True);
                Assert.That(x, Is.EqualTo(200f).Within(0.5f));
                Assert.That(y, Is.EqualTo(300f).Within(0.5f));

                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 1, phase = TouchPhase.Ended, position = new Vector2(200f, Screen.height - 300f) });
                yield return null;
                Assert.That(input.TryGetPress(out _, out _), Is.False, "a finger lifted is no new press");
            }
            finally
            {
                InputSystem.RemoveDevice(screen);
            }
        }

        [UnityTest]
        public IEnumerator TheBackKeyCountsOnAKeyboardThatIsNotCurrent()
        {
            var back = InputSystem.AddDevice<Keyboard>();
            var other = InputSystem.AddDevice<Keyboard>();
            try
            {
                other.MakeCurrent();
                Assert.That(BackKey.WasPressedThisFrame(), Is.False);
                yield return null;
                InputSystem.QueueStateEvent(back, new KeyboardState(Key.Escape));
                InputSystem.QueueStateEvent(back, new KeyboardState());
                yield return null;
                Assert.That(BackKey.WasPressedThisFrame(), Is.True);
                yield return null;
                Assert.That(BackKey.WasPressedThisFrame(), Is.False, "one press counts once");
            }
            finally
            {
                InputSystem.RemoveDevice(back);
                InputSystem.RemoveDevice(other);
            }
        }
    }
}
