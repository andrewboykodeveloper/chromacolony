using Chromacolony.Core;
using NUnit.Framework;

namespace Chromacolony.Tests.EditMode
{
    public sealed class HelperShopTests
    {
        private const float Step = 0.125f;

        // With one slot, sending the yellow crew of column 1 sticks the board: no yellow pebble is in reach.
        private const string Ring = "board ring\nRRR\nRYR\nRRR\n= R4 R4\n= Y1\n";

        [Test]
        public void AHelperIsUsableWhenTheBoardAllowsItAndThePlayerHasItInStock()
        {
            var prices = new Prices(coinsPerWin: 20, startStock: 1, slot: 30, shuffle: 20, back: 25);
            var shop = new HelperShop(Start(), Progress.New(prices), prices);

            Assert.That(shop.Usable(Helper.Shuffle), Is.True);
            Assert.That(shop.Usable(Helper.Slot), Is.True);
            Assert.That(shop.Usable(Helper.Back), Is.False, "no crew has left its column yet");
        }

        [Test]
        public void AHelperOutOfStockIsUsableOnlyWithTheCoinsForIt()
        {
            var prices = new Prices(coinsPerWin: 20, startStock: 0, slot: 30, shuffle: 20, back: 25);
            Progress progress = Progress.New(prices);
            var shop = new HelperShop(Start(), progress, prices);
            Assert.That(shop.Usable(Helper.Shuffle), Is.False, "no stock and no coins");

            progress.Won(0, 2, prices);

            Assert.That(shop.Usable(Helper.Shuffle), Is.True);
            Assert.That(shop.Usable(Helper.Slot), Is.False, "the extra slot costs more than the player has");
        }

        [Test]
        public void AStuckBoardOffersAHelperOnlyWhenThePlayerCanTakeOne()
        {
            var prices = new Prices(coinsPerWin: 20, startStock: 0, slot: 30, shuffle: 20, back: 25);
            Progress progress = Progress.New(prices);
            GameModel model = Start();
            var shop = new HelperShop(model, progress, prices);
            model.TrySend(1);
            model.Tick(Step);
            Assert.That(model.Phase, Is.EqualTo(GamePhase.Stuck));
            Assert.That(shop.AnyUsable, Is.False, "no stock and no coins");

            progress.Won(0, 2, prices);

            Assert.That(shop.AnyUsable, Is.False, "the shuffle the coins would pay for cannot help a stuck board");
            progress.Won(0, 2, prices);
            Assert.That(shop.AnyUsable, Is.True);
            Assert.That(shop.Usable(Helper.Back), Is.True);
        }

        private static GameModel Start()
        {
            var model = new GameModel(new Rules(1, 0.25f));
            model.Start(BoardText.Parse(Ring)[0]);
            return model;
        }
    }
}
