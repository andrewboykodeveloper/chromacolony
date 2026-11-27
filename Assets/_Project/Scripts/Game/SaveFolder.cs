using System.IO;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace Chromacolony.Game
{
    // Tests point the game at a folder of their own, so a test run never touches a player's saves.
    [AutoStaticsCleanup]
    internal static partial class SaveFolder
    {
        private static string s_testFolder;

        public static string Current => s_testFolder ?? Path.Combine(Application.persistentDataPath, "saves");

        public static void UseForTests(string folder)
        {
            s_testFolder = folder;
        }
    }
}
