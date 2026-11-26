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

        private SaveStore _saves;
        private Progress _progress;
        private GameModel _model;
        private GameTicker _ticker;
        private IInput _input;
        private Camera _camera;
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

            VisualElement root = _document.rootVisualElement.Q(UiNames.Root);
            var screens = new ScreenHost(root);
            SafeAreas.Keep(root.Q(UiNames.Safe), new ScreenSafeArea());
            _input = new PointerInput();
            ulong seed = (ulong)DateTime.Now.Ticks;
            var random = new Pcg32(seed);
            _beach = new BeachView(_camera, _spriteMaterial, config.Float(ConfigKeys.BeachShore), _content.Beach);
            _effects = new Effects(_spriteMaterial, new Pcg32(seed + 2), _content);
            _boardView = new BoardView(_camera, _model, _crewMaterial, _spriteMaterial, colors, root, root.Q(UiNames.Labels), _effects, _content);
            _audio = new AudioService(new UnityAudio(gameObject, _content.Sounds, _content.VoiceLines, _content.Tracks), _progress, text,
                config.Float(ConfigKeys.SoundGap), config.Float(ConfigKeys.SoundGapPick), config.Float(ConfigKeys.SoundGapCoin));
            _audio.ApplyMusic();
            _boardView.Sound = _audio.Play;

            _settings = new SettingsPresenter(screens, text, _progress, _audio, Save);
            _menu = new MenuPresenter(screens, text, _progress, _settings, _camera, _boardView);
            _result = new ResultPresenter(screens, text, _model, _progress, prices, shop, boards.Count, _boardView, _camera, _audio, Save);
            _helpers = new HelperPresenter(screens, text, _model, _progress, prices, shop, _boardView, _audio, random, Save);
            _board = new BoardPresenter(screens, text, _model, boards, _progress, _boardView, _beach, _effects, _audio, _menu, _settings,
                _helpers, _result, Save);
            _settings.LanguageChanged += ShowTexts;

            _settings.Bind(root);
            _menu.Bind(root);
            _result.Bind(root);
            _helpers.Bind(root);
            _board.Bind(root);

            // After every other handler, so that a switch that turns the sounds off is not heard.
            root.Query<Button>().ForEach(button => button.clicked += () => _audio.Play(SoundKeys.Click));
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
                _board.GoBack();
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
