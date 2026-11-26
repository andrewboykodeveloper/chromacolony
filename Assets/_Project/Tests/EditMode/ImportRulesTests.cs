using Chromacolony.Foundation.Editor;
using NUnit.Framework;
using UnityEditor;

namespace Chromacolony.Tests.EditMode
{
    public sealed class ImportRulesTests
    {
        [TestCase(ArtImportRules.ArtFolder)]
        [TestCase(AudioImportRules.MusicFolder)]
        public void EveryFolderAnImportRuleWatchesExists(string folder)
        {
            Assert.That(AssetDatabase.IsValidFolder(folder.TrimEnd('/')), Is.True, $"no folder {folder}");
        }
    }
}
