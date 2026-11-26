using System.IO;
using NUnit.Framework;
using UnityEditor;

namespace Chromacolony.Tests.EditMode
{
    public sealed class WebBuildTests
    {
        private const string Template = "Chromacolony";
        private const string TemplatePage = "Assets/WebGLTemplates/Chromacolony/index.html";

        [Test]
        public void TheWebTemplateSyncsThePersistentDataPath()
        {
            Assert.That(PlayerSettings.WebGL.template, Is.EqualTo("PROJECT:" + Template));
            Assert.That(File.Exists(TemplatePage), Is.True, $"no {TemplatePage}");
            Assert.That(File.ReadAllText(TemplatePage), Does.Match(@"(?m)^\s*config\.autoSyncPersistentDataPath = true;"));
        }
    }
}
