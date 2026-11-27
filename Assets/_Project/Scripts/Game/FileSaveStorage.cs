using System.IO;
using Chromacolony.Core;

namespace Chromacolony.Game
{
    public sealed class FileSaveStorage : ISaveStorage
    {
        private readonly string _folder;

        public FileSaveStorage(string folder)
        {
            _folder = folder;
        }

        public string Read(string key)
        {
            string path = PathOf(key);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        public void Write(string key, string text)
        {
            Directory.CreateDirectory(_folder);
            string path = PathOf(key);
            string temp = path + ".tmp";
            File.WriteAllText(temp, text);
            if (File.Exists(path))
            {
                File.Replace(temp, path, null);
            }
            else
            {
                File.Move(temp, path);
            }
        }

        private string PathOf(string key)
        {
            return Path.Combine(_folder, key + ".txt");
        }
    }
}
