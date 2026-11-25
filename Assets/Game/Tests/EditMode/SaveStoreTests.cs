using System.IO;
using Chromacolony.Foundation;
using Chromacolony.Foundation.Ports;
using NUnit.Framework;

namespace Chromacolony.Tests.EditMode
{
    public sealed class SaveStoreTests
    {
        private sealed class FailingStorage : ISaveStorage
        {
            public int Writes { get; private set; }

            public string Read(string key)
            {
                throw new IOException("the disk is gone");
            }

            public void Write(string key, string text)
            {
                Writes++;
            }

            public void Delete(string key)
            {
            }
        }

        private const string Key = "progress";

        [Test]
        public void ASaveIsWrittenWithAVersionLineAndReadBackWithoutIt()
        {
            var storage = new FakeSaveStorage();
            var saves = new SaveStore(storage, 2);

            saves.Save(Key, "board=3\n");

            Assert.That(storage.Read(Key), Is.EqualTo("v2\nboard=3\n"));
            Assert.That(saves.Load(Key), Is.EqualTo("board=3\n"));
        }

        [Test]
        public void AnOlderSaveIsMigratedOneVersionAtATime()
        {
            var storage = new FakeSaveStorage();
            storage.Write(Key, "v1\nboard=3\n");
            SaveStore saves = new SaveStore(storage, 3)
                .Migrate(1, text => text + "coins=0\n")
                .Migrate(2, text => text.Replace("board", "level"));

            Assert.That(saves.Load(Key), Is.EqualTo("level=3\ncoins=0\n"));
        }

        [Test]
        public void ASaveFromANewerVersionIsNeverOverwritten()
        {
            var storage = new FakeSaveStorage();
            storage.Write(Key, "v9\nboard=3\n");
            var saves = new SaveStore(storage, 1);

            Assert.That(saves.Load(Key), Is.Null);
            saves.Save(Key, "board=0\n");

            Assert.That(storage.Read(Key), Is.EqualTo("v9\nboard=3\n"));
        }

        [Test]
        public void AStorageThatFailsToReadNeitherThrowsNorIsOverwritten()
        {
            var storage = new FailingStorage();
            var saves = new SaveStore(storage, 1);

            Assert.That(saves.Load(Key), Is.Null);
            Assert.DoesNotThrow(() => saves.Save(Key, "board=1\n"));
            Assert.That(storage.Writes, Is.EqualTo(0));
        }
    }
}
