using System;
using Chromacolony.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chromacolony.Game
{
    public sealed class HelperPresenter
    {
        private readonly ScreenHost _screens;
        private readonly Localization _text;
        private readonly GameModel _model;
        private readonly Progress _progress;
        private readonly Prices _prices;
        private readonly HelperShop _shop;
        private readonly BoardView _boardView;
        private readonly AudioService _audio;
        private readonly IRandom _random;
        private readonly Action _save;
        private readonly Button[] _buttons = new Button[3];
        private readonly Label[] _badges = new Label[3];
        private Label _buyTitle;
        private Label _buyText;
        private Button _buy;
        private Button _cancel;
        private Label _noteText;
        private Button _ok;
        private Helper _buying;

        public event Action PickingChanged;

        public event Action Used;

        public bool Picking { get; private set; }

        public bool HoldsBoard => _screens.CurrentDialog == UiNames.Buy || _screens.CurrentDialog == UiNames.Note;

        public HelperPresenter(ScreenHost screens, Localization text, GameModel model, Progress progress, Prices prices, HelperShop shop,
            BoardView boardView, AudioService audio, IRandom random, Action save)
        {
            _screens = screens;
            _text = text;
            _model = model;
            _progress = progress;
            _prices = prices;
            _shop = shop;
            _boardView = boardView;
            _audio = audio;
            _random = random;
            _save = save;
        }

        public void Bind(VisualElement root, ButtonBindings bindings)
        {
            _buttons[(int)Helper.Slot] = root.Q<Button>(UiNames.HelperSlot);
            _buttons[(int)Helper.Shuffle] = root.Q<Button>(UiNames.HelperShuffle);
            _buttons[(int)Helper.Back] = root.Q<Button>(UiNames.HelperBack);
            _badges[(int)Helper.Slot] = root.Q<Label>(UiNames.HelperSlotBadge);
            _badges[(int)Helper.Shuffle] = root.Q<Label>(UiNames.HelperShuffleBadge);
            _badges[(int)Helper.Back] = root.Q<Label>(UiNames.HelperBackBadge);
            _buyTitle = root.Q<Label>(UiNames.BuyTitle);
            _buyText = root.Q<Label>(UiNames.BuyText);
            _buy = root.Q<Button>(UiNames.BuyYes);
            _cancel = root.Q<Button>(UiNames.BuyNo);
            _noteText = root.Q<Label>(UiNames.NoteText);
            _ok = root.Q<Button>(UiNames.NoteOk);

            bindings.Click(_buy, OnBuyConfirmed);
            bindings.Click(_cancel, _screens.CloseDialog);
            bindings.Click(_ok, _screens.CloseDialog);
            bindings.Click(_buttons[(int)Helper.Slot], () => OnHelperPressed(Helper.Slot));
            bindings.Click(_buttons[(int)Helper.Shuffle], () => OnHelperPressed(Helper.Shuffle));
            bindings.Click(_buttons[(int)Helper.Back], () => OnHelperPressed(Helper.Back));
            ShowTexts();
            Refresh();
            _buttons[(int)Helper.Back].EnableInClassList(UiClasses.On, Picking);
            if (_screens.CurrentDialog == UiNames.Buy)
            {
                ShowBuyText();
            }
        }

        public void ShowTexts()
        {
            _buyTitle.text = _text.Get(TextKeys.BuyTitle);
            _buy.text = _text.Get(TextKeys.Buy);
            _cancel.text = _text.Get(TextKeys.Cancel);
            _ok.text = _text.Get(TextKeys.Ok);
            _noteText.text = _text.Get(TextKeys.NoCoins);
        }

        public void Refresh()
        {
            for (int h = 0; h < _buttons.Length; h++)
            {
                var helper = (Helper)h;
                int stock = _progress.Stock(helper);
                _badges[h].text = stock > 0 ? stock.ToString() : _prices.Of(helper).ToString();
                _badges[h].EnableInClassList(UiClasses.Price, stock == 0);
                _buttons[h].EnableInClassList(UiClasses.Off, !_shop.Usable(helper));
            }
        }

        public void Pick(Vector2 world)
        {
            int slot = _boardView.SlotUnder(world);
            bool taken = slot >= 0 && _model.CanBack(slot) && _progress.Pay(Helper.Back, _prices);
            if (taken)
            {
                _model.Back(slot);
                _audio.Play(SoundKeys.Helper);
                _save();
            }

            SetPicking(false);
            if (taken)
            {
                Used?.Invoke();
            }
        }

        public void CancelPick()
        {
            SetPicking(false);
        }

        private void OnHelperPressed(Helper helper)
        {
            if (!_model.CanUse(helper) || _screens.CurrentDialog != null)
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
                    ShowBuyText();
                    _screens.Open(UiNames.Buy);
                    break;
                default:
                    _screens.Open(UiNames.Note);
                    break;
            }
        }

        private void OnBuyConfirmed()
        {
            _screens.CloseDialog();
            Use(_buying);
        }

        private void Use(Helper helper)
        {
            if (helper == Helper.Back)
            {
                SetPicking(true);
                return;
            }

            if (!_model.CanUse(helper) || !_progress.Pay(helper, _prices))
            {
                return;
            }

            _audio.Play(SoundKeys.Helper);
            if (helper == Helper.Slot)
            {
                _model.AddSlot();
            }
            else
            {
                _model.Shuffle(_random);
            }

            _save();
            Used?.Invoke();
        }

        private void ShowBuyText()
        {
            int price = _prices.Of(_buying);
            _buyText.text = _text.Format(TextKeys.BuyText, NameOf(_buying), price, _progress.Coins, _progress.Coins - price);
        }

        private void SetPicking(bool on)
        {
            Picking = on;
            _buttons[(int)Helper.Back].EnableInClassList(UiClasses.On, on);
            PickingChanged?.Invoke();
        }

        private string NameOf(Helper helper)
        {
            return helper switch
            {
                Helper.Slot => _text.Get(TextKeys.HelperSlot),
                Helper.Shuffle => _text.Get(TextKeys.HelperShuffle),
                Helper.Back => _text.Get(TextKeys.HelperBack),
                _ => throw new ArgumentOutOfRangeException(nameof(helper), helper, "no such helper"),
            };
        }
    }
}
