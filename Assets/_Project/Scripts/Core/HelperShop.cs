namespace Chromacolony.Core
{
    /// <summary>
    /// Which helpers the player can take on the board in play: the board must allow the helper now, and the player must have it
    /// in stock or the coins for it.
    /// </summary>
    public sealed class HelperShop
    {
        private readonly GameModel _model;
        private readonly Progress _progress;
        private readonly Prices _prices;

        public bool AnyUsable => Usable(Helper.Slot) || Usable(Helper.Shuffle) || Usable(Helper.Back);

        public HelperShop(GameModel model, Progress progress, Prices prices)
        {
            _model = model;
            _progress = progress;
            _prices = prices;
        }

        public bool Usable(Helper helper)
        {
            return _model.CanUse(helper) && _progress.PaymentFor(helper, _prices) != Payment.None;
        }
    }
}
