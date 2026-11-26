using System;
using System.Collections.Generic;
using Chromacolony.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chromacolony.Game
{
    /// <summary>
    /// A board in play: starting it, the taps on its columns, the tutorial, the speed, the pause and the way back to the menu.
    /// </summary>
    public sealed class BoardPresenter
    {
        private const string OnClass = "on";

        private readonly ScreenHost _screens;
        private readonly Localization _text;
        private readonly GameModel _model;
        private readonly IReadOnlyList<BoardSpec> _boards;
        private readonly Progress _progress;
        private readonly BoardView _board;
        private readonly BeachView _beach;
        private readonly Effects _effects;
        private readonly AudioService _audio;
        private readonly MenuPresenter _menu;
        private readonly SettingsPresenter _settings;
        private readonly HelperPresenter _helpers;
        private readonly ResultPresenter _result;
        private readonly Action _save;
        private Tutorial _tutorial = new Tutorial(Lesson.None);
        private int _boardIndex;
        private Label _boardName;
        private Label _hint;
        private Button _speed;
        private Label _pauseTitle;
        private Button _resume;
        private Button _pauseSettings;
        private Button _pauseRetry;
        private Button _pauseMenu;

        public bool Paused { get; private set; }

        public BoardPresenter(ScreenHost screens, Localization text, GameModel model, IReadOnlyList<BoardSpec> boards, Progress progress,
            BoardView board, BeachView beach, Effects effects, AudioService audio, MenuPresenter menu, SettingsPresenter settings,
            HelperPresenter helpers, ResultPresenter result, Action save)
        {
            _screens = screens;
            _text = text;
            _model = model;
            _boards = boards;
            _progress = progress;
            _board = board;
            _beach = beach;
            _effects = effects;
            _audio = audio;
            _menu = menu;
            _settings = settings;
            _helpers = helpers;
            _result = result;
            _save = save;

            model.BoardWon += () => OnBoardEnded(true);
            model.BoardStuck += () => OnBoardEnded(false);
            model.SlotAdded += _ => Refresh();
            model.CrewSent += e => OnCrewSent(e.Crew);
            model.CrewDone += _ => OnCrewDone();
            model.CrewRevealed += _ => OnCrewRevealed();
            model.CrewBack += _ => Refresh();
            model.QueueShuffled += ShowTutorial;
            menu.PlayPressed += () => StartBoard(_progress.Board);
            result.NextPressed += () => StartBoard(_progress.Board);
            result.RetryPressed += () => StartBoard(_boardIndex);
            result.MenuPressed += ToMenu;
            helpers.PickingChanged += () => ShowHint(_helpers.Picking ? _text.Get(TextKeys.PickCrew) : LessonText());
            helpers.Used += OnHelperUsed;
        }

        public void Bind(VisualElement root, ButtonBindings bindings)
        {
            _boardName = root.Q<Label>(UiNames.BoardName);
            _hint = root.Q<Label>(UiNames.Hint);
            _speed = root.Q<Button>(UiNames.HudSpeed);
            _pauseTitle = root.Q<Label>(UiNames.PauseTitle);
            _resume = root.Q<Button>(UiNames.Resume);
            _pauseSettings = root.Q<Button>(UiNames.PauseSettings);
            _pauseRetry = root.Q<Button>(UiNames.PauseRetry);
            _pauseMenu = root.Q<Button>(UiNames.PauseMenu);

            bindings.Click(root.Q<Button>(UiNames.HudPause), Pause);
            bindings.Click(_speed, ToggleSpeed);
            bindings.Click(_resume, Resume);
            bindings.Click(_pauseSettings, () => _settings.Open(fromPause: true));
            bindings.Click(_pauseRetry, () => StartBoard(_boardIndex));
            bindings.Click(_pauseMenu, ToMenu);
            ShowTexts();
            _speed.EnableInClassList(OnClass, _model.Fast);
        }

        public void ShowTexts()
        {
            _pauseTitle.text = _text.Get(TextKeys.Pause);
            _resume.text = _text.Get(TextKeys.Resume);
            _pauseSettings.text = _text.Get(TextKeys.Settings);
            _pauseRetry.text = _text.Get(TextKeys.Retry);
            _pauseMenu.text = _text.Get(TextKeys.ToMenu);
            if (_screens.CurrentScreen == UiNames.Game)
            {
                _boardName.text = _text.Format(TextKeys.Board, _boardIndex + 1);
                ShowHint(_helpers.Picking ? _text.Get(TextKeys.PickCrew) : LessonText());
            }
        }

        public void StartBoard(int index)
        {
            Paused = false;
            _result.Stop();
            _effects.Clear();
            _helpers.CancelPick();
            _boardIndex = index % _boards.Count;
            _beach.OnBoard = true;
            _model.Start(_boards[_boardIndex]);
            _model.Fast = _progress.Fast;
            _speed.EnableInClassList(OnClass, _model.Fast);
            _boardName.text = _text.Format(TextKeys.Board, _boardIndex + 1);
            _screens.Show(UiNames.Game);
            if (_boardIndex == 0)
            {
                _audio.Say(LineKeys.Hello);
            }

            // After the model starts, so the reveals of the opening crews cannot teach this board's lesson.
            _tutorial = new Tutorial(Lessons.For(_boardIndex, _boards));
            ShowTutorial();
            Refresh();
        }

        public void ToMenu()
        {
            Paused = false;
            _result.Stop();
            _effects.Clear();
            _helpers.CancelPick();
            _model.BackToMenu();
            _beach.OnBoard = false;
            _board.Clear();
            _menu.Show();
        }

        public void Tap(Vector2 world)
        {
            if (_screens.CurrentDialog != null || _screens.CurrentScreen != UiNames.Game)
            {
                return;
            }

            if (_helpers.Picking)
            {
                _helpers.Pick(world);
                return;
            }

            if (_model.Phase != GamePhase.Playing)
            {
                return;
            }

            int column = _board.ColumnAt(world);
            if (column >= 0)
            {
                _model.TrySend(column);
            }
        }

        public void GoBack()
        {
            if (_screens.CurrentDialog == UiNames.Settings)
            {
                _settings.Close();
            }
            else if (_screens.CurrentDialog == UiNames.Pause)
            {
                Resume();
            }
            else if (_screens.CurrentDialog == UiNames.Result)
            {
                ToMenu();
            }
            else if (_screens.CurrentDialog != null)
            {
                _screens.CloseDialog();
            }
            else if (_helpers.Picking)
            {
                _helpers.CancelPick();
            }
            else
            {
                Pause();
            }
        }

        private void Pause()
        {
            if (_screens.CurrentScreen != UiNames.Game || _screens.CurrentDialog != null)
            {
                return;
            }

            Paused = true;
            _screens.Open(UiNames.Pause);
        }

        private void Resume()
        {
            Paused = false;
            _screens.CloseDialog();
        }

        private void ToggleSpeed()
        {
            _model.Fast = !_model.Fast;
            _progress.Fast = _model.Fast;
            _speed.EnableInClassList(OnClass, _model.Fast);
            _save();
        }

        private void OnCrewSent(Crew crew)
        {
            _audio.Play(SoundKeys.Tap);
            _tutorial.OnCrewSent(crew);
            ShowTutorial();
            Refresh();
        }

        private void OnCrewDone()
        {
            _tutorial.OnCrewDone();
            ShowTutorial();
            Refresh();
        }

        private void OnCrewRevealed()
        {
            _tutorial.OnCrewRevealed();
            ShowTutorial();
        }

        private void OnBoardEnded(bool won)
        {
            _helpers.CancelPick();
            _result.Begin(won, _boardIndex);
            Refresh();
        }

        private void OnHelperUsed()
        {
            _result.Cancel();
            Refresh();
        }

        private void ShowTutorial()
        {
            _board.Point(_tutorial.PointedColumn(_model));
            if (!_helpers.Picking)
            {
                ShowHint(LessonText());
            }
        }

        private string LessonText()
        {
            return _tutorial.Lesson switch
            {
                Lesson.Tap => _text.Get(TextKeys.LessonTap),
                Lesson.Slots => _text.Get(TextKeys.LessonSlots),
                Lesson.Inside => _text.Get(TextKeys.LessonInside),
                Lesson.Rope => _text.Get(TextKeys.LessonRope),
                Lesson.Cover => _text.Get(TextKeys.LessonCover),
                _ => null,
            };
        }

        private void ShowHint(string text)
        {
            _hint.text = text ?? string.Empty;
            _hint.EnableInClassList(ScreenHost.HiddenClass, string.IsNullOrEmpty(text));
        }

        private void Refresh()
        {
            _result.ShowCoins();
            _helpers.Refresh();
        }
    }
}
