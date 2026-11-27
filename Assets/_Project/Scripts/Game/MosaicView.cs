using System.Collections.Generic;
using Chromacolony.Core;
using UnityEngine;

namespace Chromacolony.Game
{
    /// <summary>
    /// The board's picture: its pebbles, the glow on the pebbles that a waiting crew is after, and the picture that puts itself
    /// together again when the board is won.
    /// </summary>
    public sealed class MosaicView
    {
        private sealed class Piece
        {
            public SpriteRenderer Renderer;
            public float Delay;
            public bool Twinkled;
        }

        private const float PebbleCellShare = 0.94f;

        private const float BreathSpeed = 5f;
        private const float GlowWhiteness = 0.55f;
        private const float GlowAlpha = 0.55f;
        private const float GlowAlphaBreath = 0.4f;
        private const float GlowCells = 1.75f;
        private const float GlowCellsBreath = 0.25f;
        private const float LitGrowth = 0.08f;

        // The picture comes back a row at a time from the bottom: the top row starts this share of its time after the bottom one.
        private const float PictureSeconds = 1.1f;
        private const float PictureRowsShare = 0.7f;
        private const float PieceGrowSeconds = 0.22f;
        private const float PiecePopArc = 0.75f;
        private const int TwinkleEvery = 4;
        private const float TwinkleCells = 1.2f;

        private readonly Material _crewMaterial;
        private readonly Material _spriteMaterial;
        private readonly Color[] _colors;
        private readonly Sprite _pebble;
        private readonly Sprite _glow;
        private readonly Effects _effects;
        private readonly List<Piece> _picture = new List<Piece>();
        private SpriteRenderer[] _pebbles = new SpriteRenderer[0];
        private SpriteRenderer[] _glows = new SpriteRenderer[0];
        private bool[] _lit = new bool[0];
        private Vector3 _pebbleScale;
        private Transform _root;
        private IReadOnlyList<int> _cells;

        public MosaicView(Material crewMaterial, Material spriteMaterial, Color[] colors, Sprite pebble, Sprite glow, Effects effects)
        {
            _crewMaterial = crewMaterial;
            _spriteMaterial = spriteMaterial;
            _colors = colors;
            _pebble = pebble;
            _glow = glow;
            _effects = effects;
        }

        public void Build(BoardSpec board, BoardLayout layout, Transform root)
        {
            _root = root;
            _cells = board.Cells;
            _pebbleScale = SpriteRenderers.HeightScale(_pebble, layout.Cell * PebbleCellShare);
            _pebbles = new SpriteRenderer[board.Width * board.Height];
            _glows = new SpriteRenderer[_pebbles.Length];
            _lit = new bool[_pebbles.Length];
            for (int cell = 0; cell < _pebbles.Length; cell++)
            {
                int color = board.Cells[cell];
                if (color < 0)
                {
                    continue;
                }

                SpriteRenderer pebble = SpriteRenderers.Create(_root, _pebble, _crewMaterial, "Pebble " + cell, SortingOrder.Pebble);
                pebble.color = _colors[color];
                SpriteRenderers.Place(pebble, layout.PebbleAt(cell), _pebbleScale);
                _pebbles[cell] = pebble;
            }
        }

        /// <summary>Hands over the pebble a crab carries off, or null if it is gone already.</summary>
        public SpriteRenderer Take(int cell)
        {
            SpriteRenderer pebble = _pebbles[cell];
            _pebbles[cell] = null;
            return pebble;
        }

        public void Light(int colorMask, float time, BoardLayout layout)
        {
            float breath = 0.5f + 0.5f * Mathf.Sin(time * BreathSpeed);
            for (int cell = 0; cell < _pebbles.Length; cell++)
            {
                SpriteRenderer pebble = _pebbles[cell];
                bool on = pebble != null && (colorMask & (1 << _cells[cell])) != 0;
                if (on)
                {
                    Glow(cell, pebble, breath, layout);
                }
                else if (_lit[cell])
                {
                    Dim(cell, pebble);
                }

                _lit[cell] = on;
            }
        }

        /// <summary>Lays the whole picture out again, too small to see yet.</summary>
        public void ShowPicture(BoardSpec board, BoardLayout layout)
        {
            for (int cell = 0; cell < board.Cells.Count; cell++)
            {
                int color = board.Cells[cell];
                if (color < 0)
                {
                    continue;
                }

                SpriteRenderer pebble = SpriteRenderers.Create(_root, _pebble, _crewMaterial, "Picture " + cell, SortingOrder.Pebble);
                pebble.color = _colors[color];
                pebble.transform.position = layout.PebbleAt(cell);
                pebble.transform.localScale = Vector3.zero;
                int row = cell / board.Width;
                float delay = (board.Height - 1 - row) / (float)Mathf.Max(board.Height - 1, 1) * PictureSeconds * PictureRowsShare;
                _picture.Add(new Piece { Renderer = pebble, Delay = delay, Twinkled = false });
            }
        }

        public void Reassemble(float t, BoardLayout layout)
        {
            for (int i = 0; i < _picture.Count; i++)
            {
                Piece piece = _picture[i];
                float grow = Mathf.Clamp01((t - piece.Delay) / PieceGrowSeconds);
                if (grow <= 0f)
                {
                    continue;
                }

                float pop = grow < 1f ? Mathf.Sin(grow * Mathf.PI * PiecePopArc) / Mathf.Sin(Mathf.PI * PiecePopArc) : 1f;
                piece.Renderer.transform.localScale = _pebbleScale * pop;
                if (!piece.Twinkled)
                {
                    piece.Twinkled = true;
                    if (i % TwinkleEvery == 0)
                    {
                        _effects.Twinkle(piece.Renderer.transform.position, layout.Cell * TwinkleCells);
                    }
                }
            }
        }

        public void Clear()
        {
            _picture.Clear();
            _pebbles = new SpriteRenderer[0];
            _glows = new SpriteRenderer[0];
            _lit = new bool[0];
            _cells = null;
            _root = null;
        }

        private void Glow(int cell, SpriteRenderer pebble, float breath, BoardLayout layout)
        {
            SpriteRenderer glow = _glows[cell];
            if (glow == null)
            {
                glow = SpriteRenderers.Create(_root, _glow, _spriteMaterial, "Glow " + cell, SortingOrder.PebbleGlow);
                _glows[cell] = glow;
            }

            var light = Color.Lerp(_colors[_cells[cell]], Color.white, GlowWhiteness);
            light.a = GlowAlpha + GlowAlphaBreath * breath;
            glow.enabled = true;
            glow.color = light;
            glow.transform.position = pebble.transform.position;
            glow.transform.localScale = SpriteRenderers.HeightScale(_glow, layout.Cell * (GlowCells + GlowCellsBreath * breath));
            pebble.sortingOrder = SortingOrder.PebbleLit;
            pebble.transform.localScale = _pebbleScale * (1f + LitGrowth * breath);
        }

        // A dark pebble is set back once, when it goes dark, and is not touched again while it stays dark.
        private void Dim(int cell, SpriteRenderer pebble)
        {
            _glows[cell].enabled = false;
            if (pebble != null)
            {
                pebble.sortingOrder = SortingOrder.Pebble;
                pebble.transform.localScale = _pebbleScale;
            }
        }
    }
}
