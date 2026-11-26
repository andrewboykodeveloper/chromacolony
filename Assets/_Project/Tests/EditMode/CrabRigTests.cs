using System.Collections.Generic;
using System.Linq;
using Chromacolony.Game;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Chromacolony.Tests.EditMode
{
    // The crab view moves the rig's parts by index, so the prefab must keep the order and the sides it expects.
    public sealed class CrabRigTests
    {
        private const string ContentGuid = "39848b8adfcf49f19ddef5c38300522f";

        private CrabRig _rig;

        [SetUp]
        public void LoadTheRig()
        {
            GameContent content = AssetDatabase.LoadAssetAtPath<GameContent>(AssetDatabase.GUIDToAssetPath(ContentGuid));
            Assert.That(content != null, Is.True, $"no game content with GUID {ContentGuid}");
            _rig = content.CrabRig;
            Assert.That(_rig != null, Is.True, "the game content holds no crab rig");
        }

        [Test]
        public void TheLegsAreThreeOnTheLeftThenThreeOnTheRight()
        {
            Assert.That(_rig.Legs.Count, Is.EqualTo(6));
            for (int i = 0; i < _rig.Legs.Count; i++)
            {
                Transform leg = _rig.Legs[i];
                bool left = i < 3;
                Assert.That(leg.parent, Is.EqualTo(_rig.Body), $"leg {i} moves with the body");
                Assert.That(leg.localPosition.x < 0f, Is.EqualTo(left), $"leg {i} is on its side");
                Assert.That(leg.localScale.x, Is.EqualTo(left ? -1f : 1f), $"leg {i} faces its side");
            }
        }

        [Test]
        public void TheClawsAreLeftThenRight()
        {
            Assert.That(_rig.Claws.Count, Is.EqualTo(2));
            Assert.That(_rig.Claws[0].localPosition.x, Is.LessThan(0f));
            Assert.That(_rig.Claws[0].localScale.x, Is.EqualTo(-1f));
            Assert.That(_rig.Claws[1].localPosition.x, Is.GreaterThan(0f));
            Assert.That(_rig.Claws[1].localScale.x, Is.EqualTo(1f));
        }

        [Test]
        public void PaintTintsEveryPartOfTheBodyAndNotTheShadow()
        {
            SerializedProperty painted = new SerializedObject(_rig).FindProperty("_painted");
            var parts = new List<Object>();
            for (int i = 0; i < painted.arraySize; i++)
            {
                parts.Add(painted.GetArrayElementAtIndex(i).objectReferenceValue);
            }

            Assert.That(parts, Is.EquivalentTo(_rig.Body.GetComponentsInChildren<SpriteRenderer>(true)));
            Assert.That(parts, Has.No.Member(_rig.Shadow));
        }

        [Test]
        public void ThePartsDrawInTheirSortingOrder()
        {
            Assert.That(_rig.Shadow.sortingOrder, Is.EqualTo(SortingOrder.CrabShadow));
            foreach (SpriteRenderer part in _rig.Body.GetComponentsInChildren<SpriteRenderer>(true))
            {
                int expected = part.transform.parent == _rig.Body ? SortingOrder.CrabShell
                    : _rig.Claws.Contains(part.transform.parent) ? SortingOrder.CrabClaw
                    : SortingOrder.CrabLeg;
                Assert.That(part.sortingOrder, Is.EqualTo(expected), part.name);
            }
        }
    }
}
