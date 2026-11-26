using System;
using System.Collections.Generic;
using Chromacolony.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chromacolony.Game
{
    public sealed class GameRoot : MonoBehaviour
    {
        private const string SaveKey = "progress";
        private const int SaveVersion = 1;

        [SerializeField] private UIDocument _document;
        [SerializeField] private TextAsset _configCsv;
        [SerializeField] private TextAsset _localizationCsv;
        [SerializeField] private TextAsset _boardsText;
        [SerializeField] private Material _crewMaterial;
        [SerializeField] private Material _spriteMaterial;
        [SerializeField] private GameContent _content;

        private readonly ButtonBindings _bindings = new ButtonBindings();
        private SaveStore _saves;
        private Progress _progress;
        private GameModel _model;
        private GameTicker _ticker;
        private IInput _input;
        private Camera _camera;
        private ScreenHost _screens;
        private IVisualElementScheduledItem _safeArea;
        private BeachView _beach;
        private Effects _effects;
        private BoardView _boardView;
        private AudioService _audio;
        private SettingsPresenter _settings;
        private MenuPresenter _menu;
        private ResultPresenter _result;
        private HelperPresenter _helpers;
        private BoardPresenter _board;

        private void Awake()
        {
            var config = new ConfigTable(_configCsv.text);
            IReadOnlyList<BoardSpec> boards = BoardText.Parse(_boardsText.text);
            var prices = new Prices(config.Int(ConfigKeys.CoinsPerWin), config.Int(ConfigKeys.StartStock), config.Int(ConfigKeys.PriceSlot),
                config.Int(ConfigKeys.PriceShuffle), config.Int(ConfigKeys.PriceBack));
            _saves = new SaveStore(new FileSaveStorage(), SaveVersion, problem => Debug.LogWarning("Saves: " + problem));
            _progress = Progress.FromText(_saves.Load(SaveKey), prices, boards.Count);
            var text = new Localization(_localizationCsv.text, StartLanguage());
            _model = new GameModel(new Rules(config.Int(ConfigKeys.Slots), config.Float(ConfigKeys.CrabInterval)));
            var shop = new HelperShop(_model, _progress, prices);
            _ticker = new GameTicker(1f / 60f, _model.Tick);
            _camera = Camera.main;
            _camera.backgroundColor = Hex(config.String(ConfigKeys.ColorNight));
            Color[] colors =
            {
                Hex(config.String(ConfigKeys.ColorR)), Hex(config.String(ConfigKeys.ColorO)), Hex(config.String(ConfigKeys.ColorY)),
                Hex(config.String(ConfigKeys.ColorG)), Hex(config.String(ConfigKeys.ColorT)), Hex(config.String(ConfigKeys.ColorB)),
                Hex(config.String(ConfigKeys.ColorP)), Hex(config.String(ConfigKeys.ColorK)),
            };

            _screens = new ScreenHost();
            _input = new PointerInput();
            ulong seed = (ulong)DateTime.Now.Ticks;
            var random = new Pcg32(seed);
            _beach = new BeachView(_camera, _spriteMaterial, config.Float(ConfigKeys.BeachShore), _content.Beach);
            _effects = new Effects(_spriteMaterial, new Pcg32(seed + 2), _content);
            _boardView = new BoardView(_camera, _model, _crewMaterial, _spriteMaterial, colors, _effects, _content);
            _audio = new AudioService(new UnityAudio(gameObject, _content.Sounds, _content.VoiceLines, _content.Tracks), _progress, text,
                config.Float(ConfigKeys.SoundGap), config.Float(ConfigKeys.SoundGapPick), config.Float(ConfigKeys.SoundGapCoin));
            _audio.ApplyMusic();
            _boardView.Sound = _audio.Play;

            _settings = new SettingsPresenter(_screens, text, _progress, _audio, Save);
            _menu = new MenuPresenter(_screens, text, _progress, _camera, _boardView);
            _result = new ResultPresenter(_screens, text, _model, _progress, prices, shop, boards.Count, _boardView, _camera, _audio, Save);
            _helpers = new HelperPresenter(_screens, text, _model, _progress, prices, shop, _boardView, _audio, random, Save);
            _board = new BoardPresenter(_screens, text, _model, boards, _progress, _boardView, _beach, _effects, _audio, _helpers, _result,
                Save);
            _settings.LanguageChanged += ShowTexts;
            _menu.PlayPressed += () => _board.StartBoard(_progress.Board);
            _result.NextPressed += () => _board.StartBoard(_progress.Board);
            _result.RetryPressed += _board.Retry;
            _result.MenuPressed += _board.ToMenu;
            _board.LeftBoard += _menu.Show;
        }

        // The UI document on this object runs its OnEnable first (Unity gives it execution order -100) and builds its tree anew
        // each time the object is enabled, so the interface binds here, to the tree it has now. Nothing switches the document off
        // on its own.
        private void OnEnable()
        {
            VisualElement root = _document.rootVisualElement.Q(UiNames.Root);
            _screens.Bind(root);
            _safeArea = SafeAreas.Keep(root.Q(UiNames.Safe), new ScreenSafeArea());
            _boardView.Attach(root, root.Q(UiNames.Labels));
            _settings.Bind(root, _bindings);
            _menu.Bind(root, _bindings);
            _result.Bind(root, _bindings);
            _helpers.Bind(root, _bindings);
            _board.Bind(root, _bindings);

            // After every other handler, so that a switch that turns the sounds off is not heard.
            root.Query<Button>().ForEach(button => _bindings.Click(button, () => _audio.Play(SoundKeys.Click)));
        }

        private void Start()
        {
            _board.ToMenu();
        }

        private void Update()
        {
            WorldView.Fit(_camera, BoardLayout.HalfWidth, BoardLayout.HalfHeight);
            _audio.Update(Time.deltaTime);
            _beach.Update(Time.deltaTime);
            _menu.Update();
            if (BackKey.WasPressedThisFrame())
            {
                GoBack();
            }

            if (_board.Paused || _helpers.HoldsBoard)
            {
                return;
            }

            if (_input.TryGetPress(out float x, out float y))
            {
                _board.Tap(_camera.ScreenToWorldPoint(new Vector3(x, _camera.pixelHeight - y, 0f)));
            }

            _ticker.Update();
            float speed = _model.Fast ? 2f : 1f;
            _boardView.Update(Time.deltaTime, speed);
            _effects.Update(Time.deltaTime * speed);
            _result.Update(Time.deltaTime);
        }

        private void OnDisable()
        {
            _bindings.Clear();
            _safeArea.Pause();
            _result.Unbind();
        }

        // The views made their objects, sprites and textures at run time, and a texture made so outlives the scene.
        private void OnDestroy()
        {
            _boardView.Dispose();
            _effects.Dispose();
            _beach.Dispose();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Save();
            }
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        private void GoBack()
        {
            if (_screens.CurrentDialog == UiNames.Settings)
            {
                _settings.Close();
            }
            else if (_screens.CurrentDialog == UiNames.Pause)
            {
                _board.Resume();
            }
            else if (_screens.CurrentDialog == UiNames.Result)
            {
                _board.ToMenu();
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
                _board.Pause();
            }
        }

        private string StartLanguage()
        {
            if (_progress.Language.Length > 0)
            {
                return _progress.Language;
            }

            return Application.systemLanguage == SystemLanguage.Russian ? Languages.Russian : Languages.English;
        }

        private void ShowTexts()
        {
            _menu.ShowTexts();
            _result.ShowTexts();
            _helpers.ShowTexts();
            _board.ShowTexts();
        }

        private void Save()
        {
            _saves.Save(SaveKey, _progress.ToText());
        }

        private static Color Hex(string rgb)
        {
            return ColorUtility.TryParseHtmlString("#" + rgb, out Color color) ? color : Color.magenta;
        }
    }
}
