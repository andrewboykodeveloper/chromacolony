using System.Collections.Generic;
using UnityEngine;

namespace Chromacolony.Game
{
    public sealed class CrabRig : MonoBehaviour
    {
        [SerializeField] private Transform _body;
        [SerializeField] private Transform[] _claws;
        [SerializeField] private Transform[] _legs;
        [SerializeField] private SpriteRenderer _shadow;

        [Tooltip("The renderers that Paint tints: the shell, the claws and the legs, not the shadow.")]
        [SerializeField] private SpriteRenderer[] _painted;

        public Transform Body => _body;

        public IReadOnlyList<Transform> Claws => _claws;

        public IReadOnlyList<Transform> Legs => _legs;

        public SpriteRenderer Shadow => _shadow;

        public void Paint(Color color)
        {
            foreach (SpriteRenderer part in _painted)
            {
                part.color = color;
            }
        }
    }
}
