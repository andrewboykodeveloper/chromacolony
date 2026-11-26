using System;
using Chromacolony.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chromacolony.Game
{
    public sealed class MenuPresenter
    {
        // The crab shows off this far down the menu, as a share of the screen's height.
        private const float CrabShare = 0.44f;

        private readonly ScreenHost _screens;
        private readonly Localization _text;
        private readonly Progress _progress;
        private readonly SettingsPresenter _settings;
        private readonly Camera _camera;
        private readonly BoardView _board;
        private Label _title;
        private Button _play;

        public event Action PlayPressed;

        public MenuPresenter(ScreenHost screens, Localization text, Progress progress, SettingsPresenter settings, Camera camera, BoardView board)
        {
            _screens = screens;
            _text = text;
            _progress = progress;
            _settings = settings;
            _camera = camera;
            _board = board;
        }

        public void Bind(VisualElement root)
        {
            _title = root.Q<Label>(UiNames.Title);
            _play = root.Q<Button>(UiNames.Play);
            _play.clicked += () => PlayPressed?.Invoke();
            root.Q<Button>(UiNames.MenuSettings).clicked += () => _settings.Open(fromPause: false);
            ShowTexts();
        }

        public void ShowTexts()
        {
            _title.text = _text.Get(TextKeys.Title);
            _play.text = _progress.Board == 0 && !_progress.Finished ? _text.Get(TextKeys.Play) : _text.Format(TextKeys.PlayBoard, _progress.Board + 1);
        }

        public void Show()
        {
            ShowTexts();
            _screens.Show(UiNames.Menu);
        }

        public void Update()
        {
            bool onMenu = _screens.CurrentScreen == UiNames.Menu && _screens.CurrentDialog == null;
            float top = _camera.transform.position.y + _camera.orthographicSize;
            var spot = new Vector2(_camera.transform.position.x, top - _camera.orthographicSize * 2f * CrabShare);
            _board.ShowOff(spot, onMenu);
        }
    }
}
