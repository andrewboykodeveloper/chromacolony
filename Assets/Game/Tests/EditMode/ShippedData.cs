using System.Collections.Generic;
using Chromacolony.Core;
using Chromacolony.Foundation;
using Chromacolony.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Chromacolony.Tests.EditMode
{
    internal static class ShippedData
    {
        private const string BoardsPath = "Assets/Game/Data/Boards.txt";
        private const string ConfigPath = "Assets/Game/Data/Config.csv";

        public static IReadOnlyList<BoardSpec> Boards()
        {
            return BoardText.Parse(Read(BoardsPath));
        }

        public static Rules Rules()
        {
            var config = new ConfigTable(Read(ConfigPath));
            return new Rules(config.Int(ConfigKeys.Slots), config.Float(ConfigKeys.CrabInterval));
        }

        private static string Read(string path)
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            Assert.That(asset, Is.Not.Null, $"no {path}");
            return asset.text;
        }
    }
}
