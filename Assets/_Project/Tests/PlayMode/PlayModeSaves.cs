using System;
using System.IO;
using Chromacolony.Game;
using NUnit.Framework;
using UnityEngine;

namespace Chromacolony.Tests.PlayMode
{
    // The PlayMode tests play as new players, in a saves folder of their own, and keep their copies of it beside it.
    [SetUpFixture]
    public sealed class PlayModeSaves
    {
        private static string Root => Path.Combine(Application.temporaryCachePath, "tests");

        public static string Folder => Path.Combine(Root, "saves");

        public static string NewCopy()
        {
            return Path.Combine(Root, "copies", Guid.NewGuid().ToString("N"));
        }

        public static void Clear()
        {
            if (Directory.Exists(Folder))
            {
                Directory.Delete(Folder, true);
            }
        }

        [OneTimeSetUp]
        public void UseAFolderOfTheirOwn()
        {
            SaveFolder.UseForTests(Folder);
            Clear();
        }

        [OneTimeTearDown]
        public void LeaveNoSavesBehind()
        {
            SaveFolder.UseForTests(null);
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, true);
            }
        }
    }
}
