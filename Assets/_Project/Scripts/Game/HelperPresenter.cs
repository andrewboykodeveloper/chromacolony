using System;
using System.Globalization;
using Chromacolony.Core;
using UnityEngine.UIElements;

namespace Chromacolony.Game
{
    public sealed class HelperPresenter
    {
        private readonly ScreenHost _screens;
        private readonly Localization _text;
        private readonly HelperShop _shop;
        private readonly AudioService _audio;
        private readonly Button[] _buttons = new Button[Helpers.Count];
        private readonly Label[] _badges = new Label[Helpers.Count];
        private Label _buyTitle;
        private Label _buyText;
        private Button _buy;
        private Button _cancel;
        private Label _noteText;
        private Button _ok;
        private Helper _buying;

        public bool HoldsBoard => _screens.CurrentDialog == UiNames.Buy || _screens.CurrentDialog == UiNames.Note;

        public HelperPresenter(ScreenHost screens, Localization text, HelperShop shop, AudioService audio)
        {
            _screens = screens;
            _text = text;
            _shop = shop;
            _audio = audio;

            shop.Changed += Refresh;
            shop.Used += _ => _audio.Play(SoundKeys.Helper);
            shop.PickingChanged += ShowPicking;
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
            ShowPicking();
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

        private void Refresh()
        {
            for (int h = 0; h < _buttons.Length; h++)
            {
                var helper = (Helper)h;
                int stock = _shop.Stock(helper);
                int shown = stock > 0 ? stock : _shop.Price(helper);
                _badges[h].text = shown.ToString(CultureInfo.InvariantCulture);
                _badges[h].EnableInClassList(UiClasses.Price, stock == 0);
                _buttons[h].EnableInClassList(UiClasses.Off, !_shop.Usable(helper));
            }
        }

        private void OnHelperPressed(Helper helper)
        {
            if (helper == Helper.Back && _shop.Picking)
            {
                _shop.StopPicking();
                return;
            }

            if (!_shop.Allowed(helper) || _screens.CurrentDialog != null)
            {
                return;
            }

            switch (_shop.PaymentFor(helper))
            {
                case Payment.Stock:
                    _shop.TryUse(helper);
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
            _shop.TryUse(_buying);
        }

        private void ShowBuyText()
        {
            int price = _shop.Price(_buying);
            int coins = _shop.Coins;
            _buyText.text = _text.Format(TextKeys.BuyText, NameOf(_buying), price, coins, coins - price);
        }

        private void ShowPicking()
        {
            _buttons[(int)Helper.Back].EnableInClassList(UiClasses.On, _shop.Picking);
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
