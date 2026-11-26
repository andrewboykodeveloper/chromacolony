using System;
using System.IO;
using Chromacolony.Foundation.Unity;
using NUnit.Framework;

namespace Chromacolony.Tests
{
    // The PlayMode tests play as new players, so the player's own saves wait in another folder for the whole run.
    [SetUpFixture]
    public sealed class PlayModeSaves
    {
        private bool _setAside;

        private static string Kept => FileSaveStorage.DefaultFolder + " kept during tests";

        public static void ClearTestSaves()
        {
            Assert.That(Directory.Exists(Kept), Is.True, "the player's saves are not set aside");
            if (Directory.Exists(FileSaveStorage.DefaultFolder))
            {
                Directory.Delete(FileSaveStorage.DefaultFolder, true);
            }
        }

        [OneTimeSetUp]
        public void SetThePlayersSavesAside()
        {
            if (Directory.Exists(Kept))
            {
                throw new InvalidOperationException(
                    $"{Kept} still holds the player's saves from a run that stopped early. Put them back in {FileSaveStorage.DefaultFolder} by hand, keeping anything newer there, before running the tests again.");
            }

            if (Directory.Exists(FileSaveStorage.DefaultFolder))
            {
                Directory.Move(FileSaveStorage.DefaultFolder, Kept);
            }
            else
            {
                Directory.CreateDirectory(Kept);
            }

            _setAside = true;
        }

        [OneTimeTearDown]
        public void BringThePlayersSavesBack()
        {
            if (!_setAside)
            {
                return;
            }

            ClearTestSaves();
            if (Directory.GetFileSystemEntries(Kept).Length == 0)
            {
                Directory.Delete(Kept);
            }
            else
            {
                Directory.Move(Kept, FileSaveStorage.DefaultFolder);
            }
        }
    }
}
