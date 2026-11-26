using System.Collections.Generic;
using Chromacolony.Core;
using Chromacolony.Game;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Chromacolony.Tests.EditMode
{
    internal static class ShippedData
    {
        // The data is found by GUID, which survives a move or a rename.
        private const string BoardsGuid = "cda407870c2b4b0eba7a7fc2833eba63";
        private const string ConfigGuid = "2eada14ad966444ab9641c44be4c8360";

        public static IReadOnlyList<BoardSpec> Boards()
        {
            return BoardText.Parse(Read(BoardsGuid));
        }

        public static Rules Rules()
        {
            var config = new ConfigTable(Read(ConfigGuid));
            return new Rules(config.Int(ConfigKeys.Slots), config.Float(ConfigKeys.CrabInterval));
        }

        private static string Read(string guid)
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(guid));
            Assert.That(asset, Is.Not.Null, $"no text asset with GUID {guid}");
            return asset.text;
        }
    }
}
