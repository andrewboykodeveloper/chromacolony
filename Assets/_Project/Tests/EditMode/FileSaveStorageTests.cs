using System.Collections.Generic;
using System.IO;
using Chromacolony.Core;
using Chromacolony.Game;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Chromacolony.Tests.EditMode
{
    public sealed class FileSaveStorageTests
    {
        private string _file;

        [SetUp]
        public void PutAFileWhereTheSavesFolderWouldGo()
        {
            _file = Path.GetTempFileName();
        }

        [TearDown]
        public void DeleteTheFile()
        {
            File.Delete(_file);
        }

        // The saves folder would sit inside a file, so no write can succeed.
        [Test]
        public void AWriteThatKeepsFailingIsReportedOnceAndLoggedNowhereElse()
        {
            var problems = new List<string>();
            var saves = new SaveStore(new FileSaveStorage(Path.Combine(_file, "saves")), 1, problems.Add);

            saves.Save("progress", "board=1\n");
            saves.Save("progress", "board=2\n");

            Assert.That(problems, Has.Count.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
