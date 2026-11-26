using System.Collections.Generic;
using Chromacolony.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chromacolony.Game
{
    public sealed class GameRoot : MonoBehaviour
    {
        private const string OnClass = "on";
        private const string OffClass = "off";
        private const string PriceClass = "price";
        private const string WonClass = "won";
        private const string PrimaryClass = "btn-primary";
        private const string SecondaryClass = "btn-secondary";
        private const string SaveKey = "progress";

        private const string Russian = "ru";
        private const string English = "en";
        private const int SaveVersion = 1;

        [SerializeField] private UIDocument _document;
        [SerializeField] private TextAsset _configCsv;
        [SerializeField] private TextAsset _localizationCsv;
        [SerializeField] private TextAsset _boardsText;
        [SerializeField] private Material _crewMaterial;
        [SerializeField] private Material _spriteMaterial;
        [SerializeField] private GameContent _content;

        private readonly EventBus _events = new EventBus();
        private readonly Dictionary<string, float> _played = new Dictionary<string, float>();
        private readonly Button[] _helpers = new Button[3];
        private readonly Label[] _badges = new Label[3];
        private ScreenHost _screens;
        private Localization _text;
        private GameModel _model;
        private GameTicker _ticker;
        private BoardView _board;
        private BeachView _beach;
        private Effects _effects;
        private CoinFlight _coinFlight;
        private IAudio _audio;
        private float _soundGap;
        private float _pickGap;
        private float _coinGap;
        private VisualElement _ui;
        private VisualElement _resultDialog;
        private float _time;
        private bool _settingsFromPause;
        private bool _claw;
        private bool _celebrated;
        private bool _coinsHeld;
        private int _coinsFrom;
        private IInput _input;
        private IRandom _random;
        private Camera _camera;
        private SaveStore _saves;
        private Prices _prices;
        private Progress _progress;
        private IReadOnlyList<BoardSpec> _boards;
        private int _boardIndex;
        private bool _paused;
        private bool _picking;
        private bool _resultPending;
        private bool _resultWon;
        private Helper _buying;
        private Lesson _lesson;
        private Label _boardName;
        private Label _coins;
        private Label _hint;
        private Label _resultTitle;
        private Label _resultText;
        private Label _buyText;
        private Label _noteText;
        private Button _play;
        private Button _next;
        private Button _useHelper;
        private Button _retry;
        private Button _speed;
        private Button _languageRu;
        private Button _languageEn;
        private Button _musicSwitch;
        private Button _soundsSwitch;
        private Button _voiceSwitch;

        private void Awake()
        {
            var config = new ConfigTable(_configCsv.text);
            _boards = BoardText.Parse(_boardsText.text);
            _prices = new Prices(config.Int(ConfigKeys.CoinsPerWin), config.Int(ConfigKeys.StartStock), config.Int(ConfigKeys.PriceSlot),
                config.Int(ConfigKeys.PriceShuffle), config.Int(ConfigKeys.PriceBack));
            _saves = new SaveStore(new FileSaveStorage(), SaveVersion, problem => Debug.LogWarning("Saves: " + problem));
            _progress = Progress.FromText(_saves.Load(SaveKey), _prices, _boards.Count);
            _text = new Localization(_localizationCsv.text, StartLanguage());
            _model = new GameModel(_events, new Rules(config.Int(ConfigKeys.Slots), config.Float(ConfigKeys.CrabInterval)));
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
            _screens = new ScreenHost(root);
            SafeAreas.Keep(root.Q(UiNames.Safe), new ScreenSafeArea());
            _input = new PointerInput();
            ulong seed = (ulong)System.DateTime.Now.Ticks;
            _random = new Pcg32(seed);
            _beach = new BeachView(_camera, _spriteMaterial, config.Float(ConfigKeys.BeachShore), _content.Beach);
            _effects = new Effects(_spriteMaterial, new Pcg32(seed + 2), _content);
            _board = new BoardView(_camera, _model, _crewMaterial, _spriteMaterial, colors, root, root.Q(UiNames.Labels), _events, _effects, _content);
            _audio = new UnityAudio(gameObject, _content.Sounds, _content.VoiceLines, _content.Tracks);
            _soundGap = config.Float(ConfigKeys.SoundGap);
            _pickGap = config.Float(ConfigKeys.SoundGapPick);
            _coinGap = config.Float(ConfigKeys.SoundGapCoin);
            ApplyMusic();
            _board.Sound = Play;
            Bind(root);
            _coinFlight = new CoinFlight(root.Q(UiNames.Labels), _coins) { Landed = () => Play(SoundKeys.Coin) };
            _events.Subscribe<BoardWon>(_ => Result(true));
            _events.Subscribe<BoardStuck>(_ => Result(false));
            _events.Subscribe<SlotAdded>(_ => Refresh());
            _events.Subscribe<CrewSent>(e => Sent(e.Crew));
            _events.Subscribe<CrewDone>(_ => Done());
            _events.Subscribe<CrewRevealed>(_ => Learned(Lesson.Cover));
            _events.Subscribe<CrewBack>(_ => Refresh());
            ToMenu();
        }

        private string StartLanguage()
        {
            if (_progress.Language.Length > 0)
            {
                return _progress.Language;
            }

            return Application.systemLanguage == SystemLanguage.Russian ? Russian : English;
        }

        private void Bind(VisualElement root)
        {
            _ui = root;
            _boardName = root.Q<Label>(UiNames.BoardName);
            _coins = root.Q<Label>(UiNames.Coins);
            _hint = root.Q<Label>(UiNames.Hint);
            _resultTitle = root.Q<Label>(UiNames.ResultTitle);
            _resultText = root.Q<Label>(UiNames.ResultText);
            _buyText = root.Q<Label>(UiNames.BuyText);
            _noteText = root.Q<Label>(UiNames.NoteText);
            _play = root.Q<Button>(UiNames.Play);
            _next = root.Q<Button>(UiNames.Next);
            _useHelper = root.Q<Button>(UiNames.UseHelper);
            _retry = root.Q<Button>(UiNames.Retry);
            _speed = root.Q<Button>(UiNames.HudSpeed);
            _languageRu = root.Q<Button>(UiNames.LanguageRu);
            _languageEn = root.Q<Button>(UiNames.LanguageEn);
            _musicSwitch = root.Q<Button>(UiNames.MusicSwitch);
            _soundsSwitch = root.Q<Button>(UiNames.SoundsSwitch);
            _voiceSwitch = root.Q<Button>(UiNames.VoiceSwitch);
            _resultDialog = root.Q(UiNames.Result);
            _helpers[(int)Helper.Slot] = root.Q<Button>(UiNames.HelperSlot);
            _helpers[(int)Helper.Shuffle] = root.Q<Button>(UiNames.HelperShuffle);
            _helpers[(int)Helper.Back] = root.Q<Button>(UiNames.HelperBack);
            _badges[(int)Helper.Slot] = root.Q<Label>(UiNames.HelperSlotBadge);
            _badges[(int)Helper.Shuffle] = root.Q<Label>(UiNames.HelperShuffleBadge);
            _badges[(int)Helper.Back] = root.Q<Label>(UiNames.HelperBackBadge);

            ShowTexts();

            _play.clicked += () => StartBoard(_progress.Board);
            _next.clicked += () => StartBoard(_progress.Board);
            _retry.clicked += () => StartBoard(_boardIndex);
            _useHelper.clicked += _screens.CloseDialog;
            root.Q<Button>(UiNames.ResultMenu).clicked += ToMenu;
            root.Q<Button>(UiNames.HudPause).clicked += Pause;
            root.Q<Button>(UiNames.Resume).clicked += Resume;
            root.Q<Button>(UiNames.PauseRetry).clicked += () => StartBoard(_boardIndex);
            root.Q<Button>(UiNames.PauseMenu).clicked += ToMenu;
            root.Q<Button>(UiNames.BuyYes).clicked += Bought;
            root.Q<Button>(UiNames.BuyNo).clicked += _screens.CloseDialog;
            root.Q<Button>(UiNames.NoteOk).clicked += _screens.CloseDialog;
            _speed.clicked += ToggleSpeed;
            root.Q<Button>(UiNames.MenuSettings).clicked += () => OpenSettings(false);
            root.Q<Button>(UiNames.PauseSettings).clicked += () => OpenSettings(true);
            root.Q<Button>(UiNames.SettingsDone).clicked += CloseSettings;
            _languageRu.clicked += () => ChooseLanguage(Russian);
            _languageEn.clicked += () => ChooseLanguage(English);
            _musicSwitch.clicked += ToggleMusic;
            _soundsSwitch.clicked += ToggleSounds;
            _voiceSwitch.clicked += ToggleVoice;
            _helpers[(int)Helper.Slot].clicked += () => Ask(Helper.Slot);
            _helpers[(int)Helper.Shuffle].clicked += () => Ask(Helper.Shuffle);
            _helpers[(int)Helper.Back].clicked += () => Ask(Helper.Back);

            root.Query<Button>().ForEach(button => button.clicked += () => Play(SoundKeys.Click));
        }

        private void ShowTexts()
        {
            _ui.Q<Label>(UiNames.Title).text = _text.Get(TextKeys.Title);
            _next.text = _text.Get(TextKeys.Next);
            _useHelper.text = _text.Get(TextKeys.UseHelper);
            _retry.text = _text.Get(TextKeys.Retry);
            _ui.Q<Button>(UiNames.ResultMenu).text = _text.Get(TextKeys.ToMenu);
            _ui.Q<Label>(UiNames.PauseTitle).text = _text.Get(TextKeys.Pause);
            _ui.Q<Button>(UiNames.Resume).text = _text.Get(TextKeys.Resume);
            _ui.Q<Button>(UiNames.PauseSettings).text = _text.Get(TextKeys.Settings);
            _ui.Q<Button>(UiNames.PauseRetry).text = _text.Get(TextKeys.Retry);
            _ui.Q<Button>(UiNames.PauseMenu).text = _text.Get(TextKeys.ToMenu);
            _ui.Q<Label>(UiNames.BuyTitle).text = _text.Get(TextKeys.BuyTitle);
            _ui.Q<Button>(UiNames.BuyYes).text = _text.Get(TextKeys.Buy);
            _ui.Q<Button>(UiNames.BuyNo).text = _text.Get(TextKeys.Cancel);
            _ui.Q<Button>(UiNames.NoteOk).text = _text.Get(TextKeys.Ok);
            _ui.Q<Label>(UiNames.SettingsTitle).text = _text.Get(TextKeys.Settings);
            _ui.Q<Label>(UiNames.SettingsLanguage).text = _text.Get(TextKeys.Language);
            _ui.Q<Label>(UiNames.SettingsMusic).text = _text.Get(TextKeys.Music);
            _ui.Q<Label>(UiNames.SettingsSounds).text = _text.Get(TextKeys.Sounds);
            _ui.Q<Label>(UiNames.SettingsVoice).text = _text.Get(TextKeys.Voice);
            _ui.Q<Button>(UiNames.SettingsDone).text = _text.Get(TextKeys.Done);
            _languageRu.text = _text.Get(TextKeys.LanguageRu);
            _languageEn.text = _text.Get(TextKeys.LanguageEn);
            _play.text = PlayText();
            if (_screens.CurrentScreen == UiNames.Game)
            {
                _boardName.text = _text.Format(TextKeys.Board, _boardIndex + 1);
                Hint(_picking ? _text.Get(TextKeys.PickCrew) : LessonText());
            }
        }

        private string PlayText()
        {
            return _progress.Board == 0 && !_progress.Finished ? _text.Get(TextKeys.Play) : _text.Format(TextKeys.PlayBoard, _progress.Board + 1);
        }

        private void OpenSettings(bool fromPause)
        {
            if (_screens.CurrentDialog != (fromPause ? UiNames.Pause : null))
            {
                return;
            }

            _settingsFromPause = fromPause;
            ShowSettings();
            _screens.Open(UiNames.Settings);
        }

        private void CloseSettings()
        {
            if (_screens.CurrentDialog != UiNames.Settings)
            {
                return;
            }

            if (_settingsFromPause && _screens.CurrentScreen == UiNames.Game)
            {
                _screens.Open(UiNames.Pause);
            }
            else
            {
                _screens.CloseDialog();
            }
        }

        private void ShowSettings()
        {
            _musicSwitch.EnableInClassList(OnClass, _progress.Music);
            _soundsSwitch.EnableInClassList(OnClass, _progress.Sound);
            _voiceSwitch.EnableInClassList(OnClass, _progress.Voice);
            _languageRu.EnableInClassList(OnClass, _text.Language == Russian);
            _languageEn.EnableInClassList(OnClass, _text.Language == English);
        }

        private void ChooseLanguage(string language)
        {
            _progress.Language = language;
            Save();
            if (_text.Language != language)
            {
                _text = new Localization(_localizationCsv.text, language);
                ShowTexts();
            }

            ShowSettings();
        }

        private void ToggleMusic()
        {
            _progress.Music = !_progress.Music;
            ApplyMusic();
            ShowSettings();
            Save();
        }

        private void ToggleSounds()
        {
            _progress.Sound = !_progress.Sound;
            ShowSettings();
            Save();
        }

        private void ToggleVoice()
        {
            _progress.Voice = !_progress.Voice;
            ShowSettings();
            Save();
        }

        private void ApplyMusic()
        {
            _audio.PlayMusic(_progress.Music ? MusicKeys.Theme : null);
        }

        private void Play(string sound)
        {
            if (!_progress.Sound)
            {
                return;
            }

            bool pick = sound == SoundKeys.Pick1 || sound == SoundKeys.Pick2 || sound == SoundKeys.Pick3;
            string group = pick ? SoundKeys.Pick1 : sound;
            float gap = pick ? _pickGap : sound == SoundKeys.Coin ? _coinGap : _soundGap;
            if (_played.TryGetValue(group, out float last) && _time - last < gap)
            {
                return;
            }

            _played[group] = _time;
            _audio.Play(sound);
        }

        private void Say(string line)
        {
            if (_progress.Voice)
            {
                _audio.Say(LineKeys.For(line, _text.Language));
            }
        }

        private void Update()
        {
            WorldView.Fit(_camera, BoardLayout.HalfWidth, BoardLayout.HalfHeight);
            _time += Time.deltaTime;
            _beach.Update(Time.deltaTime);
            Bob();
            if (BackKey.WasPressedThisFrame())
            {
                Back();
            }

            if (_paused || Holding())
            {
                return;
            }

            ReadPress();
            _ticker.Update();
            float speed = _model.Fast ? 2f : 1f;
            _board.Update(Time.deltaTime, speed);
            _effects.Update(Time.deltaTime * speed);
            _coinFlight.Update(Time.deltaTime);
            if (_resultPending && !_board.Busy)
            {
                if (_resultWon && !_celebrated && _model.Phase == GamePhase.Won)
                {
                    _celebrated = true;
                    _board.Celebrate();
                    Vector2 from = WorldToPanel.Point(_camera, _board.Layout.Gate, _document.rootVisualElement.Q(UiNames.Root));
                    Rect icon = _document.rootVisualElement.Q(UiNames.CoinIcon).worldBound;
                    _coinsHeld = false;
                    _coinFlight.Fly(from, icon.center, _coinsFrom, _progress.Coins);
                    return;
                }

                _resultPending = false;
                if (_model.Phase == (_resultWon ? GamePhase.Won : GamePhase.Stuck))
                {
                    ShowResult();
                }
            }
        }

        private void Bob()
        {
            bool onMenu = _screens.CurrentScreen == UiNames.Menu && _screens.CurrentDialog == null;
            float top = _camera.transform.position.y + _camera.orthographicSize;
            var spot = new Vector2(_camera.transform.position.x, top - _camera.orthographicSize * 2f * MenuCrabShare);
            _board.ShowOff(spot, onMenu);
        }

        private const float MenuCrabShare = 0.44f;

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

        private void ReadPress()
        {
            if (!_input.TryGetPress(out float x, out float y) || _screens.CurrentDialog != null || _screens.CurrentScreen != UiNames.Game)
            {
                return;
            }

            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(x, _camera.pixelHeight - y, 0f));
            if (_picking)
            {
                int slot = _board.SlotAt(world);
                if (slot >= 0 && _model.CanBack(slot) && _progress.Pay(Helper.Back, _prices))
                {
                    _model.Back(slot);
                    Play(SoundKeys.Helper);
                    Save();
                }

                Picking(false);
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

        private void StartBoard(int index)
        {
            _paused = false;
            _resultPending = false;
            _celebrated = false;
            Quiet();
            Picking(false);
            _boardIndex = index % _boards.Count;
            _beach.OnBoard = true;
            _model.Start(_boards[_boardIndex]);
            _model.Fast = _progress.Fast;
            _speed.EnableInClassList(OnClass, _model.Fast);
            _boardName.text = _text.Format(TextKeys.Board, _boardIndex + 1);
            _screens.Show(UiNames.Game);
            if (_boardIndex == 0)
            {
                Say(LineKeys.Hello);
            }

            _lesson = Lessons.For(_boardIndex, _boards);
            Hint(LessonText());
            bool teaching = _lesson == Lesson.Tap || _lesson == Lesson.Slots || _lesson == Lesson.Inside;
            _claw = teaching;
            _board.Point(teaching ? FirstTap() : -1);
            Refresh();
        }

        private int FirstTap()
        {
            for (int c = 0; c < _model.ColumnCount; c++)
            {
                IReadOnlyList<Crew> column = _model.Column(c);
                if (column.Count > 0 && column[0].Link < 0 && _model.Mosaic.AnyInReach(column[0].Color))
                {
                    return c;
                }
            }

            return -1;
        }

        private void Sent(Crew crew)
        {
            Play(SoundKeys.Tap);
            _claw = false;
            _board.Point(-1);
            if (_lesson == Lesson.Tap || (_lesson == Lesson.Rope && crew.Link >= 0))
            {
                Learned(_lesson);
            }

            Refresh();
        }

        private void Done()
        {
            if (_lesson == Lesson.Slots || _lesson == Lesson.Inside)
            {
                Learned(_lesson);
            }

            Refresh();
        }

        private void Learned(Lesson lesson)
        {
            if (_lesson != lesson || lesson == Lesson.None)
            {
                return;
            }

            _lesson = Lesson.None;
            _claw = false;
            _board.Point(-1);
            if (!_picking)
            {
                Hint(null);
            }
        }

        private string LessonText()
        {
            switch (_lesson)
            {
                case Lesson.Tap:
                    return _text.Get(TextKeys.LessonTap);
                case Lesson.Slots:
                    return _text.Get(TextKeys.LessonSlots);
                case Lesson.Inside:
                    return _text.Get(TextKeys.LessonInside);
                case Lesson.Rope:
                    return _text.Get(TextKeys.LessonRope);
                case Lesson.Cover:
                    return _text.Get(TextKeys.LessonCover);
                default:
                    return null;
            }
        }

        private void ToMenu()
        {
            _paused = false;
            _resultPending = false;
            _celebrated = false;
            Quiet();
            Picking(false);
            _model.BackToMenu();
            _beach.OnBoard = false;
            _board.Clear();
            _play.text = PlayText();
            _screens.Show(UiNames.Menu);
        }

        private void Pause()
        {
            if (_screens.CurrentScreen != UiNames.Game || _screens.CurrentDialog != null)
            {
                return;
            }

            _paused = true;
            _screens.Open(UiNames.Pause);
        }

        private void Quiet()
        {
            _effects.Clear();
            _coinFlight.Stop();
            _coinsHeld = false;
        }

        private void Resume()
        {
            _paused = false;
            _screens.CloseDialog();
        }

        private void Back()
        {
            if (_screens.CurrentDialog == UiNames.Settings)
            {
                CloseSettings();
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
            else if (_picking)
            {
                Picking(false);
            }
            else
            {
                Pause();
            }
        }

        private void ToggleSpeed()
        {
            _model.Fast = !_model.Fast;
            _progress.Fast = _model.Fast;
            _speed.EnableInClassList(OnClass, _model.Fast);
            Save();
        }

        private void Ask(Helper helper)
        {
            if (!Usable(helper) || _screens.CurrentDialog != null)
            {
                return;
            }

            switch (_progress.PaymentFor(helper, _prices))
            {
                case Payment.Stock:
                    Use(helper);
                    break;
                case Payment.Coins:
                    _buying = helper;
                    int price = _prices.Of(helper);
                    _buyText.text = _text.Format(TextKeys.BuyText, Name(helper), price, _progress.Coins, _progress.Coins - price);
                    _screens.Open(UiNames.Buy);
                    break;
                default:
                    _noteText.text = _text.Get(TextKeys.NoCoins);
                    _screens.Open(UiNames.Note);
                    break;
            }
        }

        private void Bought()
        {
            _screens.CloseDialog();
            Use(_buying);
        }

        private void Use(Helper helper)
        {
            if (helper == Helper.Back)
            {
                Picking(true);
                return;
            }

            if (!Usable(helper) || !_progress.Pay(helper, _prices))
            {
                return;
            }

            Play(SoundKeys.Helper);
            if (helper == Helper.Slot)
            {
                _model.AddSlot();
            }
            else
            {
                _model.Shuffle(_random);

                if (_claw)
                {
                    _board.Point(FirstTap());
                }
            }

            _resultPending = false;
            Save();
            Refresh();
        }

        private bool Holding()
        {
            string dialog = _screens.CurrentDialog;
            return dialog == UiNames.Buy || dialog == UiNames.Note;
        }

        private void Picking(bool on)
        {
            _picking = on;
            _helpers[(int)Helper.Back].EnableInClassList(OnClass, on);
            Hint(on ? _text.Get(TextKeys.PickCrew) : LessonText());
        }

        private void Hint(string text)
        {
            _hint.text = text ?? string.Empty;
            _hint.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private bool Usable(Helper helper)
        {
            switch (helper)
            {
                case Helper.Slot:
                    return _model.CanAddSlot;
                case Helper.Shuffle:
                    return _model.CanShuffle;
                default:
                    return _model.AnyToBack();
            }
        }

        private bool AnyHelperToUse()
        {
            for (int h = 0; h < _helpers.Length; h++)
            {
                if (Usable((Helper)h) && _progress.PaymentFor((Helper)h, _prices) != Payment.None)
                {
                    return true;
                }
            }

            return false;
        }

        private string Name(Helper helper)
        {
            switch (helper)
            {
                case Helper.Slot:
                    return _text.Get(TextKeys.HelperSlot);
                case Helper.Shuffle:
                    return _text.Get(TextKeys.HelperShuffle);
                default:
                    return _text.Get(TextKeys.HelperBack);
            }
        }

        private void Refresh()
        {
            if (!_coinFlight.Flying)
            {
                _coins.text = (_coinsHeld ? _coinsFrom : _progress.Coins).ToString();
            }

            for (int h = 0; h < _helpers.Length; h++)
            {
                var helper = (Helper)h;
                int stock = _progress.Stock(helper);
                _badges[h].text = stock > 0 ? stock.ToString() : _prices.Of(helper).ToString();
                _badges[h].EnableInClassList(PriceClass, stock == 0);
                _helpers[h].EnableInClassList(OffClass, !Usable(helper) || _progress.PaymentFor(helper, _prices) == Payment.None);
            }
        }

        private void Result(bool won)
        {
            Picking(false);
            _resultPending = true;
            _resultWon = won;
            if (won)
            {
                _coinsFrom = _progress.Coins;
                _coinsHeld = true;
                _progress.Won(_boardIndex, _boards.Count, _prices);
                Save();
            }

            Refresh();
        }

        private void ShowResult()
        {
            _resultTitle.text = _text.Get(_resultWon ? TextKeys.Won : TextKeys.Stuck);
            bool helper = !_resultWon && AnyHelperToUse();
            bool allDone = _resultWon && _boardIndex == _boards.Count - 1;
            _resultText.text = _resultWon
                ? (allDone ? _text.Get(TextKeys.AllDone) : _text.Format(TextKeys.WonCoins, _prices.CoinsPerWin))
                : _text.Get(helper ? TextKeys.StuckHint : TextKeys.StuckRetry);

            _next.text = _text.Get(allDone ? TextKeys.PlayAgain : TextKeys.Next);
            _next.style.display = _resultWon ? DisplayStyle.Flex : DisplayStyle.None;
            _useHelper.style.display = helper ? DisplayStyle.Flex : DisplayStyle.None;

            _retry.EnableInClassList(PrimaryClass, !_resultWon && !helper);
            _retry.EnableInClassList(SecondaryClass, _resultWon || helper);
            _retry.style.display = _resultWon ? DisplayStyle.None : DisplayStyle.Flex;
            if (_resultWon)
            {
                Say(_boardIndex == _boards.Count - 1 ? LineKeys.Thanks : LineKeys.Hooray);
            }
            else
            {
                Play(SoundKeys.Stuck);
                Say(LineKeys.Oops);
            }

            _resultDialog.EnableInClassList(WonClass, _resultWon);
            _screens.Open(UiNames.Result);
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
