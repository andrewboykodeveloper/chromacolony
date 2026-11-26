using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Chromacolony.Core
{
    /// <summary>
    /// Writes saves with a version line and migrates older ones on load. A save without a version line is copied to a free
    /// <c>&lt;key&gt;.unreadable</c> name before the game starts afresh. A save from a newer version, one this version cannot
    /// migrate, or one the storage fails to read stays where it is and is not written over for the rest of the session. Each
    /// problem goes to the report callback, a failing write once until a write succeeds.
    /// </summary>
    public sealed class SaveStore
    {
        private const string UnreadableSuffix = ".unreadable";

        private readonly ISaveStorage _storage;
        private readonly int _version;
        private readonly Action<string> _report;
        private readonly Dictionary<int, Func<string, string>> _migrations = new Dictionary<int, Func<string, string>>();
        private readonly HashSet<string> _kept = new HashSet<string>();
        private readonly HashSet<string> _failing = new HashSet<string>();

        public SaveStore(ISaveStorage storage, int version, Action<string> report)
        {
            _storage = storage;
            _version = version;
            _report = report;
        }

        public SaveStore Migrate(int fromVersion, Func<string, string> migration)
        {
            _migrations[fromVersion] = migration;
            return this;
        }

        public void Save(string key, string payload)
        {
            if (_kept.Contains(key))
            {
                return;
            }

            try
            {
                _storage.Write(key, "v" + _version.ToString(CultureInfo.InvariantCulture) + "\n" + payload);
                _failing.Remove(key);
            }
            catch (Exception e) when (IsStorageFailure(e))
            {
                if (_failing.Add(key))
                {
                    _report($"{key}: could not be written: {e.Message}");
                }
            }
        }

        public string Load(string key)
        {
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

            int newline = text.IndexOf('\n');
            if (newline < 2 || text[0] != 'v' || !int.TryParse(text.Substring(1, newline - 1), NumberStyles.None, CultureInfo.InvariantCulture, out int version))
            {
                return SetAside(key, text, $"{key}: no version line");
            }

            if (version > _version)
            {
                return Keep(key, $"{key}: saved by a newer version, {version}, than this game's {_version}");
            }

            string payload = text.Substring(newline + 1);
            for (int v = version; v < _version; v++)
            {
                if (!_migrations.TryGetValue(v, out Func<string, string> migration))
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

        // An earlier copy may hold the player's older progress, so a different file goes to the next free name.
        private string SetAside(string key, string text, string problem)
        {
            try
            {
                string aside = key + UnreadableSuffix;
                for (int copy = 2; ; copy++)
                {
                    string earlier = _storage.Read(aside);
                    if (earlier == null)
                    {
                        _storage.Write(aside, text);
                        break;
                    }

                    if (earlier == text)
                    {
                        break;
                    }

                    aside = key + UnreadableSuffix + "-" + copy.ToString(CultureInfo.InvariantCulture);
                }

                _report($"{problem}; set aside as {aside}, and the game starts afresh");
                return null;
            }
            catch (Exception e) when (IsStorageFailure(e))
            {
                return Keep(key, $"{problem}; it could not be set aside: {e.Message}");
            }
        }

        private string Keep(string key, string problem)
        {
            _kept.Add(key);
            _report($"{problem}; kept, and not written over in this session");
            return null;
        }
    }
}
