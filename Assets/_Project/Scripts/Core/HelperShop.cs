using System;

namespace Chromacolony.Core
{
    /// <summary>
    /// The helpers of the board in play, from the stock or, once it is out, for coins. The back helper is paid only when its
    /// pick sends a crew back.
    /// </summary>
    public sealed class HelperShop
    {
        private readonly GameModel _model;
        private readonly Progress _progress;
        private readonly Prices _prices;
        private readonly IRandom _random;

        /// <summary>
        /// Raised when what the shop offers may have changed, once the board's change is complete.
        /// </summary>
        public event Action Changed;

        public event Action<Helper> Used;

        public event Action PickingChanged;

        public bool Picking { get; private set; }

        public bool AnyUsable => Usable(Helper.Slot) || Usable(Helper.Shuffle) || Usable(Helper.Back);

        public int Coins => _progress.Coins;

        public HelperShop(GameModel model, Progress progress, Prices prices, IRandom random)
        {
            _model = model;
            _progress = progress;
            _prices = prices;
            _random = random;

            model.BoardStarted += _ => OnBoardChanged(endsPick: true);
            model.BoardWon += () => OnBoardChanged(endsPick: true);
            model.BoardStuck += () => OnBoardChanged(endsPick: true);
            model.BoardLeft += () => OnBoardChanged(endsPick: true);
            model.CrewSent += _ => OnBoardChanged(endsPick: false);
            model.CrewDone += _ => OnBoardChanged(endsPick: false);
            model.CrewBack += _ => OnBoardChanged(endsPick: false);
            model.SlotAdded += _ => OnBoardChanged(endsPick: false);
            model.QueueShuffled += () => OnBoardChanged(endsPick: false);
        }

        public bool Allowed(Helper helper)
        {
            return _model.CanUse(helper);
        }

        public bool Usable(Helper helper)
        {
            return Allowed(helper) && PaymentFor(helper) != Payment.None;
        }

        public Payment PaymentFor(Helper helper)
        {
            return _progress.PaymentFor(helper, _prices);
        }

        public int Stock(Helper helper)
        {
            return _progress.Stock(helper);
        }

        public int Price(Helper helper)
        {
            return _prices.Of(helper);
        }

        /// <summary>
        /// Pays for the extra slot or the shuffle and applies it, or starts the back helper's pick, which is paid later. Returns
        /// false, and takes nothing, when the board does not allow the helper now or the player cannot pay for it.
        /// </summary>
        public bool TryUse(Helper helper)
        {
            if (!Usable(helper))
            {
                return false;
            }

            if (helper == Helper.Back)
            {
                SetPicking(true);
                return true;
            }

            _progress.Pay(helper, _prices);
            if (helper == Helper.Slot)
            {
                _model.AddSlot();
            }
            else
            {
                _model.Shuffle(_random);
            }

            Used?.Invoke(helper);
            return true;
        }

        public void StopPicking()
        {
            SetPicking(false);
        }

        /// <summary>
        /// Ends a pick. The crew in the slot goes back and the back helper is paid, unless no crew there can go back or the
        /// player can no longer pay. Returns whether a crew went back.
        /// </summary>
        public bool TryBack(int slot)
        {
            if (!Picking)
            {
                return false;
            }

            bool taken = _model.CanBack(slot) && _progress.Pay(Helper.Back, _prices);
            if (taken)
            {
                _model.Back(slot);
            }

            SetPicking(false);
            if (taken)
            {
                Used?.Invoke(Helper.Back);
            }

            return taken;
        }

        private void OnBoardChanged(bool endsPick)
        {
            if (endsPick)
            {
                SetPicking(false);
            }

            Changed?.Invoke();
        }

        private void SetPicking(bool on)
        {
            if (Picking == on)
            {
                return;
            }

            Picking = on;
            PickingChanged?.Invoke();
        }
    }
}
