using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Chromacolony.Core;
using Chromacolony.Game;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chromacolony.Tests.EditMode
{
    public sealed class KeysTests
    {
        // The data, the layout, the style sheet and the content are found by GUID, which survives a move or a rename.
        private const string LocalizationGuid = "5af3dfe402a345d9896500f28f70e9d5";
        private const string ConfigGuid = "2eada14ad966444ab9641c44be4c8360";
        private const string LayoutGuid = "3006750766c945f1b82df4262fe4103d";
        private const string StyleSheetGuid = "6595bc9a084e4dd8b64d68f138744360";
        private const string ContentGuid = "39848b8adfcf49f19ddef5c38300522f";

        private static readonly Regex s_placeholder = new Regex(@"\{(\d+)[^}]*\}");
        private static readonly Regex s_styleAsset = new Regex(@"\burl\(""([^""]+)""\)");

        [Test]
        public void EveryTextKeyHasItsTextInEveryLanguageWithTheSamePlaceholders()
        {
            List<List<string>> rows = Rows(LocalizationGuid);
            List<string> languages = rows[0].Skip(1).ToList();
            var texts = new Dictionary<string, List<string>>();
            var problems = new List<string>();
            foreach (List<string> row in rows.Skip(1))
            {
                if (texts.ContainsKey(row[0]))
                {
                    problems.Add($"{row[0]} is in the localization table twice");
                }
                else if (row.Count != rows[0].Count)
                {
                    problems.Add($"{row[0]} has {row.Count - 1} texts for {languages.Count} languages");
                }
                else
                {
                    texts.Add(row[0], row.Skip(1).ToList());
                }
            }

            foreach (string key in Constants(typeof(TextKeys)))
            {
                if (!texts.TryGetValue(key, out List<string> text))
                {
                    problems.Add($"{key} is not in the localization table");
                    continue;
                }

                for (int index = 0; index < languages.Count; index++)
                {
                    if (text[index].Length == 0)
                    {
                        problems.Add($"{key} has no {languages[index]} text");
                    }
                    else if (!Placeholders(text[index]).SequenceEqual(Placeholders(text[0])))
                    {
                        problems.Add($"{key} has other placeholders in {languages[index]} than in {languages[0]}");
                    }
                }
            }

            Assert.That(problems, Is.Empty);
        }

        [Test]
        public void EveryRowOfTheLocalizationTableHasAKey()
        {
            List<string> keys = Constants(typeof(TextKeys));
            List<string> rows = Rows(LocalizationGuid).Skip(1).Select(row => row[0]).ToList();

            Assert.That(rows.Except(keys), Is.Empty, "rows of the localization table with no TextKeys constant");
        }

        [Test]
        public void TheConfigTableHasOneRowForEveryConfigKeyAndNoOther()
        {
            List<string> keys = Constants(typeof(ConfigKeys));
            List<string> rows = Rows(ConfigGuid).Skip(1).Select(row => row[0]).ToList();

            Assert.That(keys.Except(rows), Is.Empty, "ConfigKeys constants with no row in the config table");
            Assert.That(rows.Except(keys), Is.Empty, "rows of the config table with no ConfigKeys constant");
            Assert.That(rows, Is.Unique, "the keys of the config table");
        }

        [Test]
        public void EveryUiNameIsOneElementOfTheLayout()
        {
            VisualElement root = Asset<VisualTreeAsset>(LayoutGuid).Instantiate();
            var problems = new List<string>();
            foreach (string name in Constants(typeof(UiNames)))
            {
                int count = root.Query(name).ToList().Count;
                if (count != 1)
                {
                    problems.Add($"{name} names {count} elements of the layout");
                }
            }

            Assert.That(problems, Is.Empty);
        }

        [Test]
        public void EverySlotOfTheContentHoldsAnAsset()
        {
            using var content = new SerializedObject(Asset<GameContent>(ContentGuid));
            var empty = new List<string>();
            SerializedProperty property = content.GetIterator();
            while (property.NextVisible(true))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                {
                    empty.Add(property.propertyPath);
                }
            }

            Assert.That(empty, Is.Empty, "slots of the content asset with nothing in them");
        }

        [Test]
        public void EverySoundMusicTrackAndVoiceLineIsAClipOfTheContent()
        {
            GameContent content = Asset<GameContent>(ContentGuid);
            var problems = new List<string>();
            problems.AddRange(Missing(Constants(typeof(SoundKeys)), content.Sounds, "sound"));
            problems.AddRange(Missing(Constants(typeof(MusicKeys)), content.Tracks, "music track"));
            problems.AddRange(Missing(VoiceLines(), content.VoiceLines, "voice line"));

            Assert.That(problems, Is.Empty);
        }

        [Test]
        public void EveryClipOfTheContentIsNamedByOneKey()
        {
            GameContent content = Asset<GameContent>(ContentGuid);
            var problems = new List<string>();
            problems.AddRange(Unnamed(content.Sounds, Constants(typeof(SoundKeys)), "sound"));
            problems.AddRange(Unnamed(content.Tracks, Constants(typeof(MusicKeys)), "music track"));
            problems.AddRange(Unnamed(content.VoiceLines, VoiceLines(), "voice line"));
            IEnumerable<string> names = content.Sounds.Concat(content.Tracks).Concat(content.VoiceLines).Where(clip => clip != null).Select(clip => clip.name);
            problems.AddRange(names.GroupBy(name => name).Where(group => group.Count() > 1).Select(group => $"{group.Key} is in the content more than once"));

            Assert.That(problems, Is.Empty);
        }

        [Test]
        public void EveryAssetTheStyleSheetNamesLoads()
        {
            string sheet = AssetDatabase.GUIDToAssetPath(StyleSheetGuid);
            Assert.That(sheet, Is.Not.Empty, $"no style sheet with GUID {StyleSheetGuid}");
            List<Match> assets = s_styleAsset.Matches(File.ReadAllText(sheet)).Cast<Match>().ToList();
            List<string> missing = assets.Where(match => AssetDatabase.LoadMainAssetAtPath(AssetPath(match.Groups[1].Value, sheet)) == null)
                .Select(match => match.Value)
                .ToList();

            Assert.That(assets, Is.Not.Empty, $"{sheet} names no asset");
            Assert.That(missing, Is.Empty, $"assets {sheet} names that do not load");
        }

        [TestCase(StyleSheetGuid)]
        [TestCase(LayoutGuid)]
        public void TheUiNamesItsAssetsByRelativePath(string guid)
        {
            string text = File.ReadAllText(AssetDatabase.GUIDToAssetPath(guid));

            Assert.That(text, Does.Not.Contain("resource("));
            Assert.That(text, Does.Not.Contain("project://"));
        }

        private static List<string> Constants(Type keysType)
        {
            List<string> constants = keysType.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue())
                .ToList();
            Assert.That(constants, Is.Not.Empty, $"{keysType.Name} has no string constants");
            return constants;
        }

        private static List<List<string>> Rows(string guid)
        {
            return Csv.Parse(Asset<TextAsset>(guid).text);
        }

        private static List<string> VoiceLines()
        {
            List<string> languages = Rows(LocalizationGuid)[0].Skip(1).ToList();
            return Constants(typeof(LineKeys)).SelectMany(line => languages.Select(language => LineKeys.For(line, language))).ToList();
        }

        private static IEnumerable<string> Missing(IEnumerable<string> names, IEnumerable<AudioClip> clips, string kind)
        {
            var present = new HashSet<string>(clips.Where(clip => clip != null).Select(clip => clip.name));
            return names.Where(name => !present.Contains(name)).Select(name => $"no {kind} {name} in the content");
        }

        private static IEnumerable<string> Unnamed(IEnumerable<AudioClip> clips, IEnumerable<string> names, string kind)
        {
            var known = new HashSet<string>(names);
            return clips.Where(clip => clip != null && !known.Contains(clip.name)).Select(clip => $"no key names the {kind} {clip.name}");
        }

        private static T Asset<T>(string guid)
            where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            Assert.That(asset, Is.Not.Null, $"no {typeof(T).Name} with GUID {guid}");
            return asset;
        }

        private static List<int> Placeholders(string text)
        {
            return s_placeholder.Matches(text).Cast<Match>()
                .Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture))
                .Distinct()
                .OrderBy(index => index)
                .ToList();
        }

        private static string AssetPath(string url, string sheet)
        {
            var parts = new List<string>(Path.GetDirectoryName(sheet).Replace('\\', '/').Split('/'));
            foreach (string part in url.Split('?', '#')[0].Split('/'))
            {
                if (part == "..")
                {
                    parts.RemoveAt(parts.Count - 1);
                }
                else if (part != ".")
                {
                    parts.Add(part);
                }
            }

            return string.Join("/", parts);
        }
    }
}
