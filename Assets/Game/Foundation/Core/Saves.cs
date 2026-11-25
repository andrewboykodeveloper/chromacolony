using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Chromacolony.Foundation.Ports;

namespace Chromacolony.Foundation
{
    public sealed class SaveStore
    {
        private readonly ISaveStorage _storage;
        private readonly int _version;
        private readonly Dictionary<int, Func<string, string>> _migrations = new Dictionary<int, Func<string, string>>();
        private readonly HashSet<string> _kept = new HashSet<string>();

        public SaveStore(ISaveStorage storage, int version)
        {
            _storage = storage;
            _version = version;
        }

        public string LastProblem { get; private set; }

        public SaveStore Migrate(int fromVersion, Func<string, string> migration)
        {
            _migrations[fromVersion] = migration;
            return this;
        }

        public void Save(string key, string payload)
        {
            LastProblem = null;
            if (_kept.Contains(key))
            {
                LastProblem = $"{key}: not written, as this run could not use the save that is there";
                return;
            }

            try
            {
                _storage.Write(key, "v" + _version.ToString(CultureInfo.InvariantCulture) + "\n" + payload);
            }
            catch (Exception e) when (IsStorageFailure(e))
            {
                LastProblem = $"{key}: could not be written: {e.Message}";
            }
        }

        public string Load(string key)
        {
            LastProblem = null;
            _kept.Remove(key);
            string text;
            try
            {
                text = _storage.Read(key);
            }
            catch (Exception e) when (IsStorageFailure(e))
            {
                return Keep(key, $"{key}: could not be read: {e.Message}");
            }

            if (text == null)
            {
                return null;
            }

            var newline = text.IndexOf('\n');
            if (newline < 2 || text[0] != 'v' || !int.TryParse(text.Substring(1, newline - 1), NumberStyles.None, CultureInfo.InvariantCulture, out var version))
            {
                LastProblem = $"{key}: no version line";
                return null;
            }

            if (version > _version)
            {
                return Keep(key, $"{key}: saved by a newer version, {version}, than this game's {_version}");
            }

            var payload = text.Substring(newline + 1);
            for (var v = version; v < _version; v++)
            {
                if (!_migrations.TryGetValue(v, out var migration))
                {
                    return Keep(key, $"{key}: no migration from version {v}");
                }

                try
                {
                    payload = migration(payload);
                }
                catch (Exception e)
                {
                    return Keep(key, $"{key}: the migration from version {v} failed: {e.Message}");
                }
            }

            return payload;
        }

        private static bool IsStorageFailure(Exception e)
        {
            return e is IOException || e is UnauthorizedAccessException;
        }

        private string Keep(string key, string problem)
        {
            LastProblem = problem;
            _kept.Add(key);
            return null;
        }
    }
}
