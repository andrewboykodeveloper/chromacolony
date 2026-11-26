using System.Collections.Generic;
using Chromacolony.Foundation.Ports;

namespace Chromacolony.Tests.EditMode
{
    internal sealed class MemorySaveStorage : ISaveStorage
    {
        private readonly Dictionary<string, string> _files = new Dictionary<string, string>();

        public string Read(string key)
        {
            return _files.TryGetValue(key, out string text) ? text : null;
        }

        public void Write(string key, string text)
        {
            _files[key] = text;
        }
    }
}
