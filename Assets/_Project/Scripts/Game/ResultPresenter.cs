using System;
using Chromacolony.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chromacolony.Game
{
    /// <summary>
    /// The end of a board. Once the board view is still, a won board celebrates and its coins fly to the counter, and then the
    /// result dialog says what the player can do next.
    /// </summary>
    public sealed class ResultPresenter
    {
        private const string WonClass = "won";
        private const string PrimaryClass = "btn-primary";
        private const string SecondaryClass = "btn-secondary";

        private readonly ScreenHost _screens;
        private readonly Localization _text;
        private readonly GameModel _model;
        private readonly Progress _progress;
        private readonly Prices _prices;
        private readonly HelperShop _shop;
        private readonly int _boardCount;
        private readonly BoardView _boardView;
        private readonly Camera _camera;
        private readonly AudioService _audio;
        private readonly Action _save;
        private VisualElement _root;
        private VisualElement _dialog;
        private Label _title;
        private Label _message;
        private Button _next;
        private Button _useHelper;
        private Button _retry;
        private Button _menu;
        private Label _coins;
        private VisualElement _coinIcon;
        private CoinFlight _coinFlight;
        private int _boardIndex;
        private bool _pending;
        private bool _won;
        private bool _celebrated;
        private bool _coinsHeld;
        private int _coinsFrom;

        public event Action NextPressed;

        public event Action RetryPressed;

        public event Action MenuPressed;

        public ResultPresenter(ScreenHost screens, Localization text, GameModel model, Progress progress, Prices prices, HelperShop shop,
            int boardCount, BoardView boardView, Camera camera, AudioService audio, Action save)
        {
            _screens = screens;
            _text = text;
            _model = model;
            _progress = progress;
            _prices = prices;
            _shop = shop;
            _boardCount = boardCount;
            _boardView = boardView;
            _camera = camera;
            _audio = audio;
            _save = save;
        }

        public void Bind(VisualElement root, ButtonBindings bindings)
        {
            _root = root;
            _dialog = root.Q(UiNames.Result);
            _title = root.Q<Label>(UiNames.ResultTitle);
            _message = root.Q<Label>(UiNames.ResultText);
            _next = root.Q<Button>(UiNames.Next);
            _useHelper = root.Q<Button>(UiNames.UseHelper);
            _retry = root.Q<Button>(UiNames.Retry);
            _menu = root.Q<Button>(UiNames.ResultMenu);
            _coins = root.Q<Label>(UiNames.Coins);
            _coinIcon = root.Q(UiNames.CoinIcon);
            _coinFlight = new CoinFlight(root.Q(UiNames.Labels), _coins) { Landed = () => _audio.Play(SoundKeys.Coin) };

            bindings.Click(_next, () => NextPressed?.Invoke());
            bindings.Click(_useHelper, _screens.CloseDialog);
            bindings.Click(_retry, () => RetryPressed?.Invoke());
            bindings.Click(_menu, () => MenuPressed?.Invoke());
            ShowTexts();
            ShowCoins();
            if (_screens.CurrentDialog == UiNames.Result)
            {
                ShowContent();
            }
        }

        public void Unbind()
        {
            _coinFlight.Stop();
        }

        public void ShowTexts()
        {
            _next.text = _text.Get(TextKeys.Next);
            _useHelper.text = _text.Get(TextKeys.UseHelper);
            _retry.text = _text.Get(TextKeys.Retry);
            _menu.text = _text.Get(TextKeys.ToMenu);
        }

        public void Begin(bool won, int boardIndex)
        {
            _pending = true;
            _won = won;
            _boardIndex = boardIndex;
            if (won)
            {
                // The counter keeps the coins from before the win until they have flown to it.
                _coinsFrom = _progress.Coins;
                _coinsHeld = true;
                _progress.Won(boardIndex, _boardCount, _prices);
                _save();
            }
        }

        public void DropPending()
        {
            _pending = false;
        }

        public void Reset()
        {
            _pending = false;
            _celebrated = false;
            _coinFlight.Stop();
            _coinsHeld = false;
        }

        public void ShowCoins()
        {
            if (!_coinFlight.Flying)
            {
                _coins.text = (_coinsHeld ? _coinsFrom : _progress.Coins).ToString();
            }
        }

        public void Update(float dt)
        {
            _coinFlight.Update(dt);
            if (!_pending || _boardView.Busy)
            {
                return;
            }

            if (_won && !_celebrated && _model.Phase == GamePhase.Won)
            {
                _celebrated = true;
                _boardView.Celebrate();
                Vector2 from = WorldToPanel.Point(_camera, _boardView.Layout.Gate, _root);
                _coinsHeld = false;
                _coinFlight.Fly(from, _coinIcon.worldBound.center, _coinsFrom, _progress.Coins);
                return;
            }

            _pending = false;

            // A helper taken while the result waited may have changed how the board ends.
            if (_model.Phase == (_won ? GamePhase.Won : GamePhase.Stuck))
            {
                Show();
            }
        }

        private void Show()
        {
            ShowContent();
            if (_won)
            {
                _audio.Say(_boardIndex == _boardCount - 1 ? LineKeys.Thanks : LineKeys.Hooray);
            }
            else
            {
                _audio.Play(SoundKeys.Stuck);
                _audio.Say(LineKeys.Oops);
            }

            _screens.Open(UiNames.Result);
        }

        private void ShowContent()
        {
            bool helper = !_won && _shop.AnyUsable;
            bool allDone = _won && _boardIndex == _boardCount - 1;
            _title.text = _text.Get(_won ? TextKeys.Won : TextKeys.Stuck);
            _message.text = _won
                ? (allDone ? _text.Get(TextKeys.AllDone) : _text.Format(TextKeys.WonCoins, _prices.CoinsPerWin))
                : _text.Get(helper ? TextKeys.StuckHint : TextKeys.StuckRetry);

            _next.text = _text.Get(allDone ? TextKeys.PlayAgain : TextKeys.Next);
            _next.EnableInClassList(ScreenHost.HiddenClass, !_won);
            _useHelper.EnableInClassList(ScreenHost.HiddenClass, !helper);

            _retry.EnableInClassList(PrimaryClass, !_won && !helper);
            _retry.EnableInClassList(SecondaryClass, _won || helper);
            _retry.EnableInClassList(ScreenHost.HiddenClass, _won);
            _dialog.EnableInClassList(WonClass, _won);
        }
    }
}
