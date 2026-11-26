using System;
using System.Collections.Generic;
using System.IO;
using Chromacolony.Core;
using NUnit.Framework;

namespace Chromacolony.Tests.EditMode
{
    public sealed class SaveStoreTests
    {
        private sealed class FailingStorage : ISaveStorage
        {
            private readonly MemorySaveStorage _files = new MemorySaveStorage();
            private readonly Func<string, bool> _failsToRead;
            private readonly Func<string, bool> _failsToWrite;

            public FailingStorage(Func<string, bool> failsToRead, Func<string, bool> failsToWrite)
            {
                _failsToRead = failsToRead;
                _failsToWrite = failsToWrite;
            }

            public int Writes { get; private set; }

            public string Read(string key)
            {
                if (_failsToRead(key))
                {
                    throw new IOException("the disk is gone");
                }

                return _files.Read(key);
            }

            public void Write(string key, string text)
            {
                if (_failsToWrite(key))
                {
                    throw new IOException("the disk is full");
                }

                Writes++;
                _files.Write(key, text);
            }
        }

        private const string Key = "progress";

        private List<string> _problems;

        [SetUp]
        public void ListenForProblems()
        {
            _problems = new List<string>();
        }

        [Test]
        public void ASaveIsWrittenWithAVersionLineAndReadBackWithoutIt()
        {
            var storage = new MemorySaveStorage();
            var saves = new SaveStore(storage, 2, _problems.Add);

            saves.Save(Key, "board=3\n");

            Assert.That(storage.Read(Key), Is.EqualTo("v2\nboard=3\n"));
            Assert.That(saves.Load(Key), Is.EqualTo("board=3\n"));
            Assert.That(_problems, Is.Empty);
        }

        [Test]
        public void AnOlderSaveIsMigratedOneVersionAtATime()
        {
            var storage = new MemorySaveStorage();
            storage.Write(Key, "v1\nboard=3\n");
            SaveStore saves = new SaveStore(storage, 3, _problems.Add)
                .Migrate(1, text => text + "coins=0\n")
                .Migrate(2, text => text.Replace("board", "level"));

            Assert.That(saves.Load(Key), Is.EqualTo("level=3\ncoins=0\n"));
            Assert.That(_problems, Is.Empty);
        }

        [Test]
        public void AFileWithoutAVersionLineIsSetAsideAndTheGameStartsAfresh()
        {
            var storage = new MemorySaveStorage();
            storage.Write(Key, "not a save");
            var saves = new SaveStore(storage, 1, _problems.Add);

            Assert.That(saves.Load(Key), Is.Null);
            Assert.That(storage.Read(Key + ".unreadable"), Is.EqualTo("not a save"), "the file is set aside");
            Assert.That(_problems, Has.Count.EqualTo(1).And.Some.Contains("set aside"));

            saves.Save(Key, "board=1\n");
            Assert.That(storage.Read(Key), Is.EqualTo("v1\nboard=1\n"), "the game saves afresh");
        }

        [Test]
        public void ADifferentUnreadableFileGoesToTheNextFreeName()
        {
            var storage = new MemorySaveStorage();
            storage.Write(Key + ".unreadable", "an older copy");
            storage.Write(Key, "not a save");
            var saves = new SaveStore(storage, 1, _problems.Add);

            saves.Load(Key);

            Assert.That(storage.Read(Key + ".unreadable"), Is.EqualTo("an older copy"), "the older copy is kept");
            Assert.That(storage.Read(Key + ".unreadable-2"), Is.EqualTo("not a save"));
        }

        [Test]
        public void TheSameUnreadableFileIsSetAsideOnce()
        {
            var storage = new MemorySaveStorage();
            storage.Write(Key, "not a save");
            var saves = new SaveStore(storage, 1, _problems.Add);

            saves.Load(Key);
            saves.Load(Key);

            Assert.That(storage.Read(Key + ".unreadable"), Is.EqualTo("not a save"));
            Assert.That(storage.Read(Key + ".unreadable-2"), Is.Null);
        }

        [Test]
        public void AFileThatCannotBeSetAsideIsNotWrittenOver()
        {
            var storage = new FailingStorage(key => false, key => key != Key);
            storage.Write(Key, "not a save");
            var saves = new SaveStore(storage, 1, _problems.Add);

            Assert.That(saves.Load(Key), Is.Null);
            saves.Save(Key, "board=1\n");

            Assert.That(storage.Read(Key), Is.EqualTo("not a save"));
            Assert.That(_problems, Has.Count.EqualTo(1).And.Some.Contains("could not be set aside"));
        }

        [Test]
        public void ASaveFromANewerVersionIsNotWrittenOver()
        {
            var storage = new MemorySaveStorage();
            storage.Write(Key, "v9\nboard=3\n");
            var saves = new SaveStore(storage, 1, _problems.Add);

            Assert.That(saves.Load(Key), Is.Null);
            saves.Save(Key, "board=0\n");

            Assert.That(storage.Read(Key), Is.EqualTo("v9\nboard=3\n"));
            Assert.That(_problems, Has.Count.EqualTo(1).And.Some.Contains("newer version"));
        }

        [Test]
        public void ASaveWithoutAMigrationIsNotWrittenOver()
        {
            var storage = new MemorySaveStorage();
            storage.Write(Key, "v1\nboard=3\n");
            var saves = new SaveStore(storage, 2, _problems.Add);

            Assert.That(saves.Load(Key), Is.Null);
            saves.Save(Key, "board=0\n");

            Assert.That(storage.Read(Key), Is.EqualTo("v1\nboard=3\n"));
            Assert.That(_problems, Has.Count.EqualTo(1).And.Some.Contains("no migration from version 1"));
        }

        [Test]
        public void ASaveWhoseMigrationFailsIsNotWrittenOver()
        {
            var storage = new MemorySaveStorage();
            storage.Write(Key, "v1\nboard=3\n");
            SaveStore saves = new SaveStore(storage, 2, _problems.Add).Migrate(1, text => throw new FormatException("unexpected line"));

            Assert.That(saves.Load(Key), Is.Null);
            saves.Save(Key, "board=0\n");

            Assert.That(storage.Read(Key), Is.EqualTo("v1\nboard=3\n"));
            Assert.That(_problems, Has.Count.EqualTo(1).And.Some.Contains("migration from version 1 failed"));
        }

        [Test]
        public void AStorageThatFailsToReadNeitherThrowsNorIsWrittenOver()
        {
            var storage = new FailingStorage(key => true, key => false);
            var saves = new SaveStore(storage, 1, _problems.Add);

            Assert.That(saves.Load(Key), Is.Null);
            Assert.DoesNotThrow(() => saves.Save(Key, "board=1\n"));

            Assert.That(storage.Writes, Is.EqualTo(0));
            Assert.That(_problems, Has.Count.EqualTo(1).And.Some.Contains("could not be read"));
        }

        [Test]
        public void AWriteThatKeepsFailingIsReportedOnceUntilAWriteSucceeds()
        {
            bool full = true;
            var storage = new FailingStorage(key => false, key => full);
            var saves = new SaveStore(storage, 1, _problems.Add);

            saves.Save(Key, "board=1\n");
            saves.Save(Key, "board=2\n");
            Assert.That(_problems, Has.Count.EqualTo(1).And.Some.Contains("could not be written"));

            full = false;
            saves.Save(Key, "board=3\n");
            full = true;
            saves.Save(Key, "board=4\n");
            Assert.That(_problems, Has.Count.EqualTo(2), "a write that fails again after a success is reported again");
        }
    }
}
