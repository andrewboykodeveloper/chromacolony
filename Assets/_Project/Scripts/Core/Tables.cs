using System;
using System.Collections.Generic;
using System.Globalization;

namespace Chromacolony.Foundation
{
    public sealed class Localization
    {
        private readonly Dictionary<string, Dictionary<string, string>> _byLanguage = new Dictionary<string, Dictionary<string, string>>();
        private readonly string _fallback;

        public Localization(string csv, string language)
        {
            var rows = Csv.Parse(csv);
            if (rows.Count == 0 || rows[0].Count < 2 || rows[0][0] != "key")
            {
                throw new FormatException("a localization table starts with key,<language>,...");
            }

            var header = rows[0];
            _fallback = header[1];
            for (var column = 1; column < header.Count; column++)
            {
                _byLanguage[header[column]] = new Dictionary<string, string>();
            }

            for (var r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                for (var column = 1; column < header.Count && column < row.Count; column++)
                {
                    _byLanguage[header[column]][row[0]] = row[column];
                }
            }

            Language = _byLanguage.ContainsKey(language) ? language : _fallback;
        }

        public string Language { get; }

        public string Get(string key)
        {
            if (_byLanguage[Language].TryGetValue(key, out var text) && text.Length > 0)
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

    public sealed class ConfigTable
    {
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

        public ConfigTable(string csv)
        {
            foreach (var row in Csv.Parse(csv))
            {
                if (row.Count >= 2 && row[0] != "key")
                {
                    _values[row[0]] = row[1];
                }
            }
        }

        public string String(string key)
        {
            return _values.TryGetValue(key, out var value) ? value : throw new KeyNotFoundException($"no config key: {key}");
        }

        public int Int(string key)
        {
            return int.Parse(String(key), CultureInfo.InvariantCulture);
        }

        public float Float(string key)
        {
            return float.Parse(String(key), CultureInfo.InvariantCulture);
        }
    }
}
