using System;
using Chromacolony.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chromacolony.Game
{
    public sealed class BoardPresenter
    {
        private readonly ScreenHost _screens;
        private readonly Localization _text;
        private readonly GameModel _model;
        private readonly BoardSession _session;
        private readonly HelperShop _shop;
        private readonly Progress _progress;
        private readonly BoardView _boardView;
        private readonly BeachView _beach;
        private readonly Effects _effects;
        private readonly AudioService _audio;
        private readonly Action _save;
        private Label _boardName;
        private Label _hint;
        private Button _speed;
        private Label _pauseTitle;
        private Button _resume;
        private Button _pauseRetry;
        private Button _pauseMenu;

        public event Action LeftBoard;

        public bool Paused { get; private set; }

        public BoardPresenter(ScreenHost screens, Localization text, GameModel model, BoardSession session, HelperShop shop,
            Progress progress, BoardView boardView, BeachView beach, Effects effects, AudioService audio, Action save)
        {
            _screens = screens;
            _text = text;
            _model = model;
            _session = session;
            _shop = shop;
            _progress = progress;
            _boardView = boardView;
            _beach = beach;
            _effects = effects;
            _audio = audio;
            _save = save;

            session.Started += OnStarted;
            session.TutorialChanged += ShowTutorial;
            shop.PickingChanged += ShowHint;
            model.CrewSent += _ => _audio.Play(SoundKeys.Tap);
        }

        public void Bind(VisualElement root, ButtonBindings bindings)
        {
            _boardName = root.Q<Label>(UiNames.BoardName);
            _hint = root.Q<Label>(UiNames.Hint);
            _speed = root.Q<Button>(UiNames.HudSpeed);
            _pauseTitle = root.Q<Label>(UiNames.PauseTitle);
            _resume = root.Q<Button>(UiNames.Resume);
            _pauseRetry = root.Q<Button>(UiNames.PauseRetry);
            _pauseMenu = root.Q<Button>(UiNames.PauseMenu);

            bindings.Click(root.Q<Button>(UiNames.HudPause), Pause);
            bindings.Click(_speed, ToggleSpeed);
            bindings.Click(_resume, Resume);
            bindings.Click(_pauseRetry, Retry);
            bindings.Click(_pauseMenu, ToMenu);
            ShowTexts();
            _speed.EnableInClassList(UiClasses.On, _model.Fast);
        }

        public void ShowTexts()
        {
            _pauseTitle.text = _text.Get(TextKeys.Pause);
            _resume.text = _text.Get(TextKeys.Resume);
            _pauseRetry.text = _text.Get(TextKeys.Retry);
            _pauseMenu.text = _text.Get(TextKeys.ToMenu);
            if (_screens.CurrentScreen == UiNames.Game)
            {
                _boardName.text = _text.Format(TextKeys.Board, _session.Board + 1);
                ShowHint();
            }
        }

        public void StartBoard(int index)
        {
            Paused = false;
            _effects.Clear();
            _beach.OnBoard = true;
            _session.Start(index);
        }

        public void Retry()
        {
            StartBoard(_session.Board);
        }

        public void ToMenu()
        {
            Paused = false;
            _effects.Clear();
            _model.BackToMenu();
            _beach.OnBoard = false;
            _boardView.Clear();
            LeftBoard?.Invoke();
        }

        public void Tap(Vector2 world)
        {
            if (_screens.CurrentDialog != null || _screens.CurrentScreen != UiNames.Game)
            {
                return;
            }

            if (_shop.Picking)
            {
                _shop.TryBack(_boardView.SlotUnder(world));
                return;
            }

            if (_model.Phase != GamePhase.Playing)
            {
                return;
            }

            int column = _boardView.ColumnUnder(world);
            if (column >= 0)
            {
                _model.TrySend(column);
            }
        }

        public void Pause()
        {
            if (_screens.CurrentScreen != UiNames.Game || _screens.CurrentDialog != null)
            {
                return;
            }

            Paused = true;
            _screens.Open(UiNames.Pause);
        }

        public void Resume()
        {
            Paused = false;
            _screens.CloseDialog();
        }

        private void OnStarted()
        {
            _speed.EnableInClassList(UiClasses.On, _model.Fast);
            _boardName.text = _text.Format(TextKeys.Board, _session.Board + 1);
            _screens.Show(UiNames.Game);
            if (_session.Board == 0)
            {
                _audio.Say(LineKeys.Hello);
            }

            ShowTutorial();
        }

        private void ToggleSpeed()
        {
            _model.Fast = !_model.Fast;
            _progress.Fast = _model.Fast;
            _speed.EnableInClassList(UiClasses.On, _model.Fast);
            _save();
        }

        private void ShowTutorial()
        {
            _boardView.Point(_session.PointedColumn());
            ShowHint();
        }

        private void ShowHint()
        {
            string text = _shop.Picking ? _text.Get(TextKeys.PickCrew) : LessonText();
            _hint.text = text ?? string.Empty;
            _hint.EnableInClassList(UiClasses.Hidden, string.IsNullOrEmpty(text));
        }

        private string LessonText()
        {
            return _session.Lesson switch
            {
                Lesson.Tap => _text.Get(TextKeys.LessonTap),
                Lesson.Slots => _text.Format(TextKeys.LessonSlots, _model.SlotCount),
                Lesson.Inside => _text.Get(TextKeys.LessonInside),
                Lesson.Rope => _text.Get(TextKeys.LessonRope),
                Lesson.Cover => _text.Get(TextKeys.LessonCover),
                _ => null,
            };
        }
    }
}
