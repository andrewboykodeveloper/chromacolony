using System.Collections.Generic;
using System.Globalization;

namespace Chromacolony.Core
{
    public sealed class ConfigTable
    {
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

        public ConfigTable(string csv)
        {
            foreach (List<string> row in Csv.Parse(csv))
            {
                if (row.Count >= 2 && row[0] != "key")
                {
                    _values[row[0]] = row[1];
                }
            }
        }

        public string String(string key)
        {
            return _values.TryGetValue(key, out string value) ? value : throw new KeyNotFoundException($"no config key: {key}");
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
