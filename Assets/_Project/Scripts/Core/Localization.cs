using System;
using System.Collections.Generic;
using System.Globalization;

namespace Chromacolony.Core
{
    public sealed class Localization
    {
        private readonly Dictionary<string, Dictionary<string, string>> _byLanguage = new Dictionary<string, Dictionary<string, string>>();
        private readonly string _fallback;

        public string Language { get; private set; }

        public Localization(string csv, string language)
        {
            List<List<string>> rows = Csv.Parse(csv);
            if (rows.Count == 0 || rows[0].Count < 2 || rows[0][0] != "key")
            {
                throw new FormatException("a localization table starts with key,<language>,...");
            }

            List<string> header = rows[0];
            _fallback = header[1];
            for (int column = 1; column < header.Count; column++)
            {
                _byLanguage[header[column]] = new Dictionary<string, string>();
            }

            for (int r = 1; r < rows.Count; r++)
            {
                List<string> row = rows[r];
                for (int column = 1; column < header.Count && column < row.Count; column++)
                {
                    _byLanguage[header[column]][row[0]] = row[column];
                }
            }

            Use(language);
        }

        public void Use(string language)
        {
            Language = _byLanguage.ContainsKey(language) ? language : _fallback;
        }

        public string Get(string key)
        {
            if (_byLanguage[Language].TryGetValue(key, out string text) && text.Length > 0)
            {
                return text;
            }

            if (_byLanguage[_fallback].TryGetValue(key, out text) && text.Length > 0)
            {
                return text;
            }

            return "[" + key + "]";
        }

        public string Format(string key, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, Get(key), args);
        }
    }
}
