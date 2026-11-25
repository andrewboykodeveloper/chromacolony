using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Chromacolony.Core;
using Chromacolony.Foundation;
using Chromacolony.Foundation.Unity;
using Chromacolony.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using MouseButton = UnityEngine.InputSystem.LowLevel.MouseButton;

namespace Chromacolony.Tests
{
    internal sealed class GameDriver
    {
        public const string OnClass = "on";
        public const string OffClass = "off";
        public const string PriceClass = "price";
        public const float FrameSeconds = 1f / 30f;

        private const string ScenePath = "Assets/Scenes/Bootstrap.unity";
        private const string BoardsFile = "Game/Data/Boards.txt";
        private const string ConfigFile = "Game/Data/Config.csv";
        private const string HiddenClass = "hidden";
        private const string CrewCountClass = "crew-count";

        private readonly IReadOnlyList<BoardSpec> _boards;
        private readonly int _slots;
        private readonly List<InputDevice> _disabled = new List<InputDevice>();
        private InputSettings _settings;
        private Mouse _mouse;
        private Keyboard _keyboard;
        private VisualElement _ui;
        private Camera _camera;

        public GameDriver()
        {
            _boards = BoardText.Parse(File.ReadAllText(Path.Combine(Application.dataPath, BoardsFile)));
            var config = new ConfigTable(File.ReadAllText(Path.Combine(Application.dataPath, ConfigFile)));
            _slots = config.Int(ConfigKeys.Slots);
        }

        public static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        public IEnumerator Boot()
        {
            PlayModeSaves.ClearTestSaves();
            Assert.That(Directory.Exists(FileSaveStorage.DefaultFolder), Is.False, "the game starts as a new player");
            Time.captureDeltaTime = FrameSeconds;
            IsolateInput();
            yield return Load();
        }

        // Reloads the game with only what was on disk when the copy was made, as if the game had been closed at that moment.
        public IEnumerator Reopen(string savesCopy)
        {
            yield return Unload();
            PlayModeSaves.ClearTestSaves();
            CopyFolder(savesCopy, FileSaveStorage.DefaultFolder);
            yield return Load();
        }

        public IEnumerator Quit()
        {
            try
            {
                yield return Unload();
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                RestoreInput();
            }
        }

        public string CopySaves()
        {
            string copy = Path.Combine(Application.temporaryCachePath, "saves copy " + Guid.NewGuid().ToString("N"));
            CopyFolder(FileSaveStorage.DefaultFolder, copy);
            return copy;
        }

        public void Press(string name)
        {
            VisualElement element = Find(name);
            Assert.That(element, Is.InstanceOf<Button>(), $"{name} is not a button");
            Assert.That(IsShown(element), Is.True, $"{name} is not on the screen");
            Assert.That(element.enabledInHierarchy, Is.True, $"{name} is disabled");
            VisualElement picked = element.panel.Pick(element.worldBound.center);
            Assert.That(picked == element || element.Contains(picked), Is.True, $"{name} is covered by {picked?.name ?? "nothing pickable"}");
            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = element;
                element.SendEvent(submit);
            }
        }

        public int ColumnCount(int board)
        {
            return _boards[board].Columns.Count;
        }

        public IEnumerator TapColumn(int board, int column, int depth)
        {
            BoardSpec spec = _boards[board];
            var layout = new BoardLayout(spec.Width, spec.Height, _slots, spec.Columns.Count);
            Vector3 world = layout.QueueAt(column, depth);
            Vector3 screen = _camera.WorldToScreenPoint(world);
            Assert.That(screen.x >= 0f && screen.x < Screen.width && screen.y >= 0f && screen.y < Screen.height, Is.True,
                $"column {column} at depth {depth} is off the screen");
            VisualElement picked = _ui.panel.Pick(WorldToPanel.Point(_camera, world, _ui));
            Assert.That(picked == null || (!(picked is Button) && picked.GetFirstAncestorOfType<Button>() == null), Is.True,
                $"column {column} at depth {depth} is under {picked?.name}");

            InputSystem.QueueStateEvent(_mouse, new MouseState { position = screen }.WithButton(MouseButton.Left, true));
            yield return null;
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = screen });
            yield return null;
        }

        public IEnumerator PressBack()
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Escape));
            yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
        }

        public bool IsShown(string name)
        {
            return IsShown(Find(name));
        }

        public bool Has(string name, string className)
        {
            return Find(name).ClassListContains(className);
        }

        public string Text(string name)
        {
            VisualElement element = Find(name);
            Assert.That(element, Is.InstanceOf<TextElement>(), $"{name} shows no text");
            return ((TextElement)element).text;
        }

        public int Number(string name)
        {
            return int.Parse(Text(name), NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        public int CrabsLeftInCrews()
        {
            int crabs = 0;
            _ui.Query<Label>(className: CrewCountClass).ForEach(label =>
            {
                if (int.TryParse(label.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int count))
                {
                    crabs += count;
                }
            });
            return crabs;
        }

        public bool MusicIsPlaying()
        {
            foreach (AudioSource source in Components<AudioSource>())
            {
                if (source.loop && source.isPlaying)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasListener()
        {
            return Components<AudioListener>().Count > 0;
        }

        public int LoopingSources()
        {
            int looping = 0;
            foreach (AudioSource source in Components<AudioSource>())
            {
                looping += source.loop && source.isPlaying ? 1 : 0;
            }

            return looping;
        }

        public T Single<T>()
            where T : Component
        {
            List<T> found = Components<T>();
            Assert.That(found.Count, Is.EqualTo(1), $"the scene has one {typeof(T).Name}");
            return found[0];
        }

        // A hidden element must also be hidden by its resolved style: a class that no style sheet applies hides nothing.
        private static bool IsShown(VisualElement element)
        {
            for (VisualElement e = element; e != null; e = e.parent)
            {
                bool hidden = e.resolvedStyle.display == DisplayStyle.None || e.resolvedStyle.visibility == Visibility.Hidden;
                Assert.That(!e.ClassListContains(HiddenClass) || hidden, Is.True, $"the {HiddenClass} class has no effect on {e.name}");
                if (hidden)
                {
                    return false;
                }
            }

            return true;
        }

        private static void CopyFolder(string from, string to)
        {
            if (!Directory.Exists(from))
            {
                return;
            }

            Directory.CreateDirectory(to);
            foreach (string file in Directory.GetFiles(from))
            {
                File.Copy(file, Path.Combine(to, Path.GetFileName(file)), true);
            }
        }

        private void IsolateInput()
        {
            InputSettings original = InputSystem.settings;
            _settings = original;
            InputSettings copy = UnityEngine.Object.Instantiate(original);
            copy.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            copy.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = copy;
            foreach (InputDevice device in InputSystem.devices)
            {
                if (device.enabled)
                {
                    InputSystem.DisableDevice(device);
                    _disabled.Add(device);
                }
            }

            _mouse = InputSystem.AddDevice<Mouse>();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse.MakeCurrent();
        }

        private void RestoreInput()
        {
            if (_mouse != null)
            {
                InputSystem.RemoveDevice(_mouse);
            }

            if (_keyboard != null)
            {
                InputSystem.RemoveDevice(_keyboard);
            }

            foreach (InputDevice device in _disabled)
            {
                if (device.added)
                {
                    InputSystem.EnableDevice(device);
                }
            }

            _disabled.Clear();
            if (_settings != null)
            {
                InputSettings copy = InputSystem.settings;
                InputSystem.settings = _settings;
                if (copy != _settings)
                {
                    UnityEngine.Object.Destroy(copy);
                }
            }
        }

        private VisualElement Find(string name)
        {
            VisualElement element = _ui.Q(name);
            Assert.That(element, Is.Not.Null, $"no element {name} in the layout");
            return element;
        }

        private List<T> Components<T>()
            where T : Component
        {
            var found = new List<T>();
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                found.AddRange(root.GetComponentsInChildren<T>());
            }

            return found;
        }

        private IEnumerator Load()
        {
            _ui = null;
            _camera = null;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.TryGetComponent(out UIDocument document))
                {
                    _ui = document.rootVisualElement;
                }

                if (root.TryGetComponent(out Camera camera))
                {
                    _camera = camera;
                }
            }

            Assert.That(_ui, Is.Not.Null, "the scene has no UI document");
            Assert.That(_camera != null, Is.True, "the scene has no camera");
        }

        private IEnumerator Unload()
        {
            Scene game = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Empty " + Guid.NewGuid().ToString("N")));
            yield return SceneManager.UnloadSceneAsync(game);
        }
    }
}
