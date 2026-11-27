using Chromacolony.Core;
using NUnit.Framework;

namespace Chromacolony.Tests.EditMode
{
    public sealed class ProgressTests
    {
        private readonly Prices _prices = new Prices(coinsPerWin: 20, startStock: 2, slot: 30, shuffle: 20, back: 25);

        [Test]
        public void NewPlayerHasTheStartStockAndNoCoins()
        {
            Progress progress = Progress.New(_prices);

            Assert.That(progress.Coins, Is.EqualTo(0));
            Assert.That(progress.Stock(Helper.Slot), Is.EqualTo(2));
            Assert.That(progress.Board, Is.EqualTo(0));
        }

        [Test]
        public void NewPlayerHearsAllInTheDeviceLanguage()
        {
            Progress progress = Progress.New(_prices);

            Assert.That(progress.Music, Is.True);
            Assert.That(progress.Sound, Is.True);
            Assert.That(progress.Voice, Is.True);
            Assert.That(progress.Language, Is.Empty);
        }

        [Test]
        public void HelperComesFromStockThenForCoins()
        {
            Progress progress = Progress.New(_prices);

            Assert.That(progress.PaymentFor(Helper.Slot, _prices), Is.EqualTo(Payment.Stock));
            Assert.That(progress.Pay(Helper.Slot, _prices), Is.True);
            Assert.That(progress.Pay(Helper.Slot, _prices), Is.True);
            Assert.That(progress.PaymentFor(Helper.Slot, _prices), Is.EqualTo(Payment.None));
            Assert.That(progress.Pay(Helper.Slot, _prices), Is.False);

            progress.Won(0, 20, _prices);
            progress.Won(1, 20, _prices);

            Assert.That(progress.PaymentFor(Helper.Slot, _prices), Is.EqualTo(Payment.Coins));
            Assert.That(progress.Pay(Helper.Slot, _prices), Is.True);
            Assert.That(progress.Coins, Is.EqualTo(10));
        }

        [Test]
        public void WinPaysAndWrapsAfterTheLastBoard()
        {
            Progress progress = Progress.New(_prices);

            progress.Won(0, 3, _prices);
            Assert.That(progress.Board, Is.EqualTo(1));
            Assert.That(progress.Finished, Is.False);

            progress.Won(2, 3, _prices);
            Assert.That(progress.Board, Is.EqualTo(0));
            Assert.That(progress.Finished, Is.True);
            Assert.That(progress.Coins, Is.EqualTo(40));
        }

        [Test]
        public void ProgressSurvivesItsText()
        {
            Progress progress = Progress.New(_prices);
            progress.Won(4, 20, _prices);
            progress.Pay(Helper.Shuffle, _prices);
            progress.Sound = false;
            progress.Fast = true;

            Progress again = Progress.FromText(progress.ToText(), _prices, 20);

            Assert.That(again.ToText(), Is.EqualTo(progress.ToText()));
            Assert.That(again.Board, Is.EqualTo(5));
            Assert.That(again.Stock(Helper.Shuffle), Is.EqualTo(1));
            Assert.That(again.Sound, Is.False);
            Assert.That(again.Fast, Is.True);
        }

        [Test]
        public void SettingsSurviveTheirText()
        {
            Progress progress = Progress.New(_prices);
            progress.Language = "ru";
            progress.Music = false;
            progress.Voice = false;

            Progress again = Progress.FromText(progress.ToText(), _prices, 20);

            Assert.That(again.ToText(), Is.EqualTo(progress.ToText()));
            Assert.That(again.Language, Is.EqualTo("ru"));
            Assert.That(again.Music, Is.False);
            Assert.That(again.Sound, Is.True);
            Assert.That(again.Voice, Is.False);
        }

        [Test]
        public void OldSoundSwitchSetsMusicAndVoice()
        {
            Progress quiet = Progress.FromText("board=3\nsound=0\n", _prices, 20);
            Assert.That(quiet.Music, Is.False);
            Assert.That(quiet.Sound, Is.False);
            Assert.That(quiet.Voice, Is.False);

            Progress heard = Progress.FromText("board=3\nsound=1\n", _prices, 20);
            Assert.That(heard.Music, Is.True);
            Assert.That(heard.Voice, Is.True);
        }

        [TestCase("language=../x")]
        [TestCase("language=")]
        [TestCase("language=RU")]
        [TestCase("language=english")]
        public void BadLanguageCodeFallsBackToTheDevice(string line)
        {
            Assert.That(Progress.FromText(line + "\n", _prices, 20).Language, Is.Empty);
        }

        [TestCase((string)null)]
        [TestCase("")]
        [TestCase("garbage\n=\ncoins=x\nboard=99\n")]
        public void MissingOrBrokenSaveStartsANewPlayer(string text)
        {
            Progress progress = Progress.FromText(text, _prices, 20);

            Assert.That(progress.Board, Is.EqualTo(0));
            Assert.That(progress.Coins, Is.EqualTo(0));
            Assert.That(progress.Stock(Helper.Back), Is.EqualTo(2));
        }
    }
}
