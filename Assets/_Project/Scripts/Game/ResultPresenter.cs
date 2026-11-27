using System;
using System.Globalization;
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
        private readonly ScreenHost _screens;
        private readonly Localization _text;
        private readonly Progress _progress;
        private readonly HelperShop _shop;
        private readonly BoardView _boardView;
        private readonly Camera _camera;
        private readonly AudioService _audio;
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
        private BoardEnded _end;
        private bool _pending;
        private bool _celebrated;
        private bool _coinsHeld;

        public event Action NextPressed;

        public event Action RetryPressed;

        public event Action MenuPressed;

        public ResultPresenter(ScreenHost screens, Localization text, Progress progress, BoardSession session, HelperShop shop,
            BoardView boardView, Camera camera, AudioService audio)
        {
            _screens = screens;
            _text = text;
            _progress = progress;
            _shop = shop;
            _boardView = boardView;
            _camera = camera;
            _audio = audio;

            session.Started += Reset;
            session.Left += Reset;
            session.Ended += OnEnded;
            shop.Used += _ => OnHelperUsed();
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

        public void Reset()
        {
            _pending = false;
            _celebrated = false;
            _coinFlight.Stop();
            _coinsHeld = false;
            ShowCoins();
        }

        public void Update(float dt)
        {
            _coinFlight.Update(dt);
            if (!_pending || _boardView.Busy)
            {
                return;
            }

            if (_end.Won && !_celebrated)
            {
                _celebrated = true;
                _boardView.Celebrate();
                Vector2 from = WorldToPanel.Point(_camera, _boardView.Layout.Gate, _root);
                _coinsHeld = false;
                _coinFlight.Fly(from, _coinIcon.worldBound.center, _end.CoinsBefore, _end.CoinsAfter);
                return;
            }

            _pending = false;
            Show();
        }

        private void OnEnded(BoardEnded end)
        {
            _end = end;
            _pending = true;

            // The counter keeps the coins from before a win until they have flown to it.
            _coinsHeld = end.Won;
            ShowCoins();
        }

        // Only a helper takes a stuck board back to play, so the result it waited to show no longer holds.
        private void OnHelperUsed()
        {
            _pending = false;
            ShowCoins();
        }

        private void ShowCoins()
        {
            if (!_coinFlight.Flying)
            {
                _coins.text = (_coinsHeld ? _end.CoinsBefore : _progress.Coins).ToString(CultureInfo.InvariantCulture);
            }
        }

        private void Show()
        {
            ShowContent();
            if (_end.Won)
            {
                _audio.Say(_end.Last ? LineKeys.Thanks : LineKeys.Hooray);
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
            bool won = _end.Won;
            bool helper = !won && _shop.AnyUsable;
            bool allDone = won && _end.Last;
            _title.text = _text.Get(won ? TextKeys.Won : TextKeys.Stuck);
            _message.text = won
                ? (allDone ? _text.Get(TextKeys.AllDone) : _text.Format(TextKeys.WonCoins, _end.CoinsAfter - _end.CoinsBefore))
                : _text.Get(helper ? TextKeys.StuckHint : TextKeys.StuckRetry);

            _next.text = _text.Get(allDone ? TextKeys.PlayAgain : TextKeys.Next);
            _next.EnableInClassList(UiClasses.Hidden, !won);
            _useHelper.EnableInClassList(UiClasses.Hidden, !helper);

            _retry.EnableInClassList(UiClasses.ButtonPrimary, !won && !helper);
            _retry.EnableInClassList(UiClasses.ButtonSecondary, won || helper);
            _retry.EnableInClassList(UiClasses.Hidden, won);
            _dialog.EnableInClassList(UiClasses.Won, won);
        }
    }
}
