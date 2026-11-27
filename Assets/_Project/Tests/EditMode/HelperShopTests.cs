using System.Collections.Generic;
using Chromacolony.Core;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Chromacolony.Tests.EditMode
{
    public sealed class HelperShopTests
    {
        private const float Step = 0.125f;

        // With one slot, sending the yellow crew of column 1 sticks the board: no yellow pebble is in reach.
        private const string Ring = "board ring\nRRR\nRYR\nRRR\n= R4 R4\n= Y1\n";

        [Test]
        public void UsableWhenAllowedAndInStock()
        {
            Prices prices = PricesWithStock(1);
            HelperShop shop = Shop(Start(), Progress.New(prices), prices);

            Assert.That(shop.Usable(Helper.Shuffle), Is.True);
            Assert.That(shop.Usable(Helper.Slot), Is.True);
            Assert.That(shop.Usable(Helper.Back), Is.False, "no crew has left its column yet");
        }

        [Test]
        public void OutOfStockNeedsTheCoins()
        {
            Prices prices = PricesWithStock(0);
            Progress progress = Progress.New(prices);
            HelperShop shop = Shop(Start(), progress, prices);
            Assert.That(shop.Usable(Helper.Shuffle), Is.False, "no stock and no coins");

            progress.Won(0, 2, prices);

            Assert.That(shop.Usable(Helper.Shuffle), Is.True);
            Assert.That(shop.Usable(Helper.Slot), Is.False, "the extra slot costs more than the player has");
        }

        [Test]
        public void StuckBoardOffersWhatThePlayerCanTake()
        {
            Prices prices = PricesWithStock(0);
            Progress progress = Progress.New(prices);
            GameModel model = Start();
            HelperShop shop = Shop(model, progress, prices);
            Stick(model);
            Assert.That(shop.AnyUsable, Is.False, "no stock and no coins");

            progress.Won(0, 2, prices);

            Assert.That(shop.AnyUsable, Is.False, "the shuffle the coins would pay for cannot help a stuck board");
            progress.Won(0, 2, prices);
            Assert.That(shop.AnyUsable, Is.True);
            Assert.That(shop.Usable(Helper.Back), Is.True);
        }

        [Test]
        public void ExtraSlotIsPaidFromTheStockAndAdded()
        {
            Prices prices = PricesWithStock(1);
            Progress progress = Progress.New(prices);
            GameModel model = Start();
            HelperShop shop = Shop(model, progress, prices);
            var used = new List<Helper>();
            shop.Used += used.Add;

            Assert.That(shop.TryUse(Helper.Slot), Is.True);

            Assert.That(model.SlotCount, Is.EqualTo(2));
            Assert.That(progress.Stock(Helper.Slot), Is.Zero);
            Assert.That(used, Is.EqualTo(new[] { Helper.Slot }));
        }

        [Test]
        public void HelperOutOfStockIsPaidWithCoins()
        {
            Prices prices = PricesWithStock(0);
            Progress progress = Progress.New(prices);
            GameModel model = Start();
            HelperShop shop = Shop(model, progress, prices);
            progress.Won(0, 2, prices);
            bool shuffled = false;
            model.QueueShuffled += () => shuffled = true;

            Assert.That(shop.TryUse(Helper.Shuffle), Is.True);

            Assert.That(shuffled, Is.True);
            Assert.That(progress.Coins, Is.Zero);
        }

        [Test]
        public void DisallowedHelperTakesNothing()
        {
            Prices prices = PricesWithStock(2);
            Progress progress = Progress.New(prices);
            HelperShop shop = Shop(Start(), progress, prices);
            shop.TryUse(Helper.Slot);
            var used = new List<Helper>();
            shop.Used += used.Add;

            Assert.That(shop.TryUse(Helper.Slot), Is.False, "one extra slot to a board");
            Assert.That(progress.Stock(Helper.Slot), Is.EqualTo(1));
            Assert.That(used, Is.Empty);
        }

        [Test]
        public void UnaffordableHelperTakesNothing()
        {
            Prices prices = PricesWithStock(0);
            GameModel model = Start();
            HelperShop shop = Shop(model, Progress.New(prices), prices);
            bool shuffled = false;
            model.QueueShuffled += () => shuffled = true;
            var used = new List<Helper>();
            shop.Used += used.Add;

            Assert.That(shop.TryUse(Helper.Shuffle), Is.False);
            Assert.That(shuffled, Is.False);
            Assert.That(used, Is.Empty);
        }

        [Test]
        public void BackStartsAPickAndTakesNothingYet()
        {
            Prices prices = PricesWithStock(1);
            Progress progress = Progress.New(prices);
            GameModel model = Start();
            HelperShop shop = Shop(model, progress, prices);
            var used = new List<Helper>();
            shop.Used += used.Add;
            Assert.That(shop.TryUse(Helper.Back), Is.False, "no crew in a slot to send back");

            model.TrySend(0);

            Assert.That(shop.TryUse(Helper.Back), Is.True);
            Assert.That(shop.Picking, Is.True);
            Assert.That(progress.Stock(Helper.Back), Is.EqualTo(1));
            Assert.That(used, Is.Empty);
        }

        [Test]
        public void PickedCrewGoesBackAndIsPaidThen()
        {
            Prices prices = PricesWithStock(1);
            Progress progress = Progress.New(prices);
            GameModel model = Start();
            HelperShop shop = Shop(model, progress, prices);
            model.TrySend(0);
            var used = new List<Helper>();
            shop.Used += used.Add;
            shop.TryUse(Helper.Back);

            Assert.That(shop.TryBack(0), Is.True);

            Assert.That(model.SlotAt(0), Is.Null);
            Assert.That(model.Column(0), Has.Count.EqualTo(2));
            Assert.That(progress.Stock(Helper.Back), Is.Zero);
            Assert.That(shop.Picking, Is.False);
            Assert.That(used, Is.EqualTo(new[] { Helper.Back }));
        }

        [Test]
        public void PickingAnEmptySlotEndsThePickFree()
        {
            Prices prices = PricesWithStock(1);
            Progress progress = Progress.New(prices);
            GameModel model = Start(slots: 2);
            HelperShop shop = Shop(model, progress, prices);
            model.TrySend(0);
            int changes = 0;
            shop.PickingChanged += () => changes++;
            shop.TryUse(Helper.Back);

            Assert.That(shop.TryBack(1), Is.False);

            Assert.That(model.SlotAt(0), Is.Not.Null);
            Assert.That(progress.Stock(Helper.Back), Is.EqualTo(1));
            Assert.That(shop.Picking, Is.False);
            Assert.That(changes, Is.EqualTo(2));
        }

        [Test]
        public void BackWithoutAPickTakesNothing()
        {
            Prices prices = PricesWithStock(1);
            Progress progress = Progress.New(prices);
            GameModel model = Start();
            HelperShop shop = Shop(model, progress, prices);
            model.TrySend(0);

            Assert.That(shop.TryBack(0), Is.False);

            Assert.That(model.SlotAt(0), Is.Not.Null);
            Assert.That(progress.Stock(Helper.Back), Is.EqualTo(1));
        }

        [Test]
        public void EndStartOrLeaveCallsThePickOff()
        {
            Prices prices = PricesWithStock(1);
            GameModel model = Start();
            HelperShop shop = Shop(model, Progress.New(prices), prices);

            model.TrySend(0);
            shop.TryUse(Helper.Back);
            Run(model);
            model.TrySend(0);
            Run(model);
            model.TrySend(1);
            Run(model);
            Assert.That(model.Phase, Is.EqualTo(GamePhase.Won));
            Assert.That(shop.Picking, Is.False, "a won board");

            model.Start(BoardText.Parse(Ring)[0]);
            Stick(model);
            Assert.That(shop.TryUse(Helper.Back), Is.True);
            model.Start(BoardText.Parse(Ring)[0]);
            Assert.That(shop.Picking, Is.False, "a new board");

            Stick(model);
            shop.TryUse(Helper.Back);
            model.BackToMenu();
            Assert.That(shop.Picking, Is.False, "the way to the menu");
        }

        [Test]
        public void ChangedAfterUnstickSeesTheBoardInPlay()
        {
            Prices prices = PricesWithStock(1);
            GameModel model = Start();
            HelperShop shop = Shop(model, Progress.New(prices), prices);
            bool shuffleUsable = false;
            shop.Changed += () => shuffleUsable = shop.Usable(Helper.Shuffle);

            Stick(model);
            shop.TryUse(Helper.Slot);
            Assert.That(shuffleUsable, Is.True, "after the extra slot");

            model.Start(BoardText.Parse(Ring)[0]);
            Stick(model);
            shop.TryUse(Helper.Back);
            shop.TryBack(0);
            Assert.That(shuffleUsable, Is.True, "after a crew went back");
        }

        // The helpers' buttons ask this after every change on the board.
        [Test]
        public void UsableMakesNoGarbage()
        {
            Prices prices = PricesWithStock(1);
            HelperShop shop = Shop(Start(slots: 5), Progress.New(prices), prices);

            Assert.That(() => { _ = shop.Usable(Helper.Shuffle); }, Is.Not.AllocatingGCMemory());
            Assert.That(() => { _ = shop.Usable(Helper.Back); }, Is.Not.AllocatingGCMemory());
            Assert.That(() => { _ = shop.Usable(Helper.Slot); }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void ShopSaysWhenTheBoardMovesOn()
        {
            Prices prices = PricesWithStock(1);
            GameModel model = Start();
            HelperShop shop = Shop(model, Progress.New(prices), prices);
            int changes = 0;
            shop.Changed += () => changes++;

            model.TrySend(0);

            Assert.That(changes, Is.EqualTo(1));
        }

        private static Prices PricesWithStock(int stock)
        {
            return new Prices(coinsPerWin: 20, startStock: stock, slot: 30, shuffle: 20, back: 25);
        }

        private static HelperShop Shop(GameModel model, Progress progress, Prices prices)
        {
            return new HelperShop(model, progress, prices, new Pcg32(1));
        }

        private static GameModel Start(int slots = 1)
        {
            var model = new GameModel(new Rules(slots, 0.25f));
            model.Start(BoardText.Parse(Ring)[0]);
            return model;
        }

        private static void Stick(GameModel model)
        {
            model.TrySend(1);
            model.Tick(Step);
            Assert.That(model.Phase, Is.EqualTo(GamePhase.Stuck));
        }

        private static void Run(GameModel model)
        {
            for (float t = 0f; t < 10f && model.FreeSlots() == 0 && model.Phase == GamePhase.Playing; t += Step)
            {
                model.Tick(Step);
            }
        }
    }
}
