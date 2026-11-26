using System.Collections.Generic;
using UnityEngine;

namespace Chromacolony.Game
{
    /// <summary>The sprites the world draws and the clips the audio plays. The UI's images are named by its style sheet.</summary>
    public sealed class GameContent : ScriptableObject
    {
        [Header("Beach and board")]
        [SerializeField] private Sprite _beach;
        [SerializeField] private Sprite _castle;
        [SerializeField] private Sprite _panel;
        [SerializeField] private Sprite _slot;
        [SerializeField] private Sprite _pebble;
        [SerializeField] private Sprite _bucket;
        [SerializeField] private Sprite _bucketCover;
        [SerializeField] private Sprite _bucketRope;
        [SerializeField] private Sprite _pointer;

        [Header("Crabs")]
        [SerializeField] private Sprite _crab;
        [SerializeField] private Sprite _crabShell;
        [SerializeField] private Sprite _crabClaw;
        [SerializeField] private Sprite _crabLeg;

        [Header("Effects")]
        [SerializeField] private Sprite _sandPuff;
        [SerializeField] private Sprite _sparkle;

        [Header("Audio")]
        [SerializeField] private AudioClip[] _tracks;
        [SerializeField] private AudioClip[] _sounds;
        [SerializeField] private AudioClip[] _voiceLines;

        public Sprite Beach => _beach;

        public Sprite Castle => _castle;

        public Sprite Panel => _panel;

        public Sprite Slot => _slot;

        public Sprite Pebble => _pebble;

        public Sprite Bucket => _bucket;

        public Sprite BucketCover => _bucketCover;

        public Sprite BucketRope => _bucketRope;

        public Sprite Pointer => _pointer;

        public Sprite Crab => _crab;

        public Sprite CrabShell => _crabShell;

        public Sprite CrabClaw => _crabClaw;

        public Sprite CrabLeg => _crabLeg;

        public Sprite SandPuff => _sandPuff;

        public Sprite Sparkle => _sparkle;

        public IReadOnlyList<AudioClip> Tracks => _tracks;

        public IReadOnlyList<AudioClip> Sounds => _sounds;

        public IReadOnlyList<AudioClip> VoiceLines => _voiceLines;
    }
}
