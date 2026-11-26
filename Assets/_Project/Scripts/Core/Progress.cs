using System;
using System.Globalization;
using System.Text;

namespace Chromacolony.Core
{
    public enum Helper
    {
        Slot,

        Shuffle,

        Back,
    }

    public enum Payment
    {
        Stock,
        Coins,
        None,
    }

    public sealed class Prices
    {
        private readonly int[] _helpers;

        public Prices(int coinsPerWin, int startStock, int slot, int shuffle, int back)
        {
            CoinsPerWin = coinsPerWin;
            StartStock = startStock;
            _helpers = new[] { slot, shuffle, back };
        }

        public int CoinsPerWin { get; }

        public int StartStock { get; }

        public int Of(Helper helper)
        {
            return _helpers[(int)helper];
        }
    }

    public sealed class Progress
    {
        private readonly int[] _stock = new int[3];

        private Progress()
        {
        }

        public int Board { get; private set; }

        public int Coins { get; private set; }

        public bool Finished { get; private set; }

        public string Language { get; set; } = string.Empty;

        public bool Music { get; set; } = true;

        public bool Sound { get; set; } = true;

        public bool Voice { get; set; } = true;

        public bool Fast { get; set; }

        public static Progress New(Prices prices)
        {
            var progress = new Progress();
            for (int h = 0; h < progress._stock.Length; h++)
            {
                progress._stock[h] = prices.StartStock;
            }

            return progress;
        }

        public int Stock(Helper helper)
        {
            return _stock[(int)helper];
        }

        public Payment PaymentFor(Helper helper, Prices prices)
        {
            return Stock(helper) > 0 ? Payment.Stock : Coins >= prices.Of(helper) ? Payment.Coins : Payment.None;
        }

        public bool Pay(Helper helper, Prices prices)
        {
            switch (PaymentFor(helper, prices))
            {
                case Payment.Stock:
                    _stock[(int)helper]--;
                    return true;
                case Payment.Coins:
                    Coins -= prices.Of(helper);
                    return true;
                default:
                    return false;
            }
        }

        public void Won(int board, int boards, Prices prices)
        {
            Coins += prices.CoinsPerWin;
            if (board + 1 >= boards)
            {
                Finished = true;
            }

            Board = (board + 1) % boards;
        }

        public string ToText()
        {
            var text = new StringBuilder();
            Line(text, "board", Board);
            Line(text, "coins", Coins);
            Line(text, "slot", Stock(Helper.Slot));
            Line(text, "shuffle", Stock(Helper.Shuffle));
            Line(text, "back", Stock(Helper.Back));
            Line(text, "finished", Finished ? 1 : 0);
            Line(text, "music", Music ? 1 : 0);
            Line(text, "sound", Sound ? 1 : 0);
            Line(text, "voice", Voice ? 1 : 0);
            Line(text, "fast", Fast ? 1 : 0);
            if (Language.Length > 0)
            {
                text.Append("language=").Append(Language).Append('\n');
            }

            return text.ToString();
        }

        public static Progress FromText(string text, Prices prices, int boards)
        {
            Progress progress = New(prices);
            if (string.IsNullOrEmpty(text))
            {
                return progress;
            }

            bool? music = null;
            bool? voice = null;
            foreach (string line in text.Split('\n'))
            {
                int equals = line.IndexOf('=');
                if (equals <= 0)
                {
                    continue;
                }

                string key = line.Substring(0, equals).Trim();
                string raw = line.Substring(equals + 1).Trim();
                if (key == "language")
                {
                    progress.Language = IsLanguage(raw) ? raw : string.Empty;
                    continue;
                }

                if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                {
                    continue;
                }

                switch (key)
                {
                    case "board":
                        progress.Board = value >= 0 && value < boards ? value : 0;
                        break;
                    case "coins":
                        progress.Coins = Math.Max(0, value);
                        break;
                    case "slot":
                        progress._stock[(int)Helper.Slot] = Math.Max(0, value);
                        break;
                    case "shuffle":
                        progress._stock[(int)Helper.Shuffle] = Math.Max(0, value);
                        break;
                    case "back":
                        progress._stock[(int)Helper.Back] = Math.Max(0, value);
                        break;
                    case "finished":
                        progress.Finished = value != 0;
                        break;
                    case "music":
                        music = value != 0;
                        break;
                    case "sound":
                        progress.Sound = value != 0;
                        break;
                    case "voice":
                        voice = value != 0;
                        break;
                    case "fast":
                        progress.Fast = value != 0;
                        break;
                }
            }

            progress.Music = music ?? progress.Sound;
            progress.Voice = voice ?? progress.Sound;
            return progress;
        }

        private static bool IsLanguage(string code)
        {
            if (code.Length < 2 || code.Length > 3)
            {
                return false;
            }

            foreach (char c in code)
            {
                if (c < 'a' || c > 'z')
                {
                    return false;
                }
            }

            return true;
        }

        private static void Line(StringBuilder text, string key, int value)
        {
            text.Append(key).Append('=').Append(value.ToString(CultureInfo.InvariantCulture)).Append('\n');
        }
    }
}
