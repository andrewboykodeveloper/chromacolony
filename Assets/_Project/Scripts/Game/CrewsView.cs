using System;
using System.Collections.Generic;
using Chromacolony.Core;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Chromacolony.Game
{
    /// <summary>
    /// The crews in their queues and places: their buckets and signs, the crab that sits on a waiting crew, the ropes between
    /// crews, and their labels.
    /// </summary>
    public sealed class CrewsView : IDisposable
    {
        private sealed class CrewVisual
        {
            public Crew Crew;
            public SpriteRenderer Renderer;

            public Vector3 Target;
            public Vector3 Scale;
            public Vector3 Place;
            public Vector3 Size;
            public bool Visible;
            public bool InSlot;
            public int Slot;
            public bool Leaving;
            public float Left;

            public float Shake;
            public float Pop;

            public SpriteRenderer Sitter;

            public SpriteRenderer Sign;

            public float Label = 1f;
            public float LabelTarget = 1f;
        }

        private sealed class Rope
        {
            public int A;
            public int B;
            public SpriteRenderer Renderer;
        }

        private const float Glide = 12f;
        private const float LeaveSeconds = 0.3f;
        private const float ShakeSeconds = 0.35f;
        private const float PopSeconds = 0.3f;

        private readonly GameModel _model;
        private readonly Material _crewMaterial;
        private readonly Material _spriteMaterial;
        private readonly Color[] _colors;
        private readonly Sprite _bucket;
        private readonly Sprite _cover;
        private readonly Sprite _rope;
        private readonly Sprite _sitterSprite;
        private readonly Effects _effects;
        private readonly Action<string> _sound;
        private readonly Signs _signs = new Signs();
        private readonly CrewLabels _labels;
        private readonly Dictionary<int, CrewVisual> _crews = new Dictionary<int, CrewVisual>();
        private readonly Dictionary<int, Rope> _ropes = new Dictionary<int, Rope>();
        private readonly List<int> _gone = new List<int>();
        private Transform _root;
        private BoardLayout _layout;

        public CrewsView(GameModel model, Camera camera, Material crewMaterial, Material spriteMaterial, Color[] colors, Effects effects,
            GameContent content, Action<string> sound)
        {
            _model = model;
            _crewMaterial = crewMaterial;
            _spriteMaterial = spriteMaterial;
            _colors = colors;
            _effects = effects;
            _sound = sound;
            _bucket = content.Bucket;
            _cover = content.BucketCover;
            _rope = content.BucketRope;
            _sitterSprite = content.Crab;
            _labels = new CrewLabels(camera);

            model.SendRefused += OnRefused;
            model.QueueShuffled += Requeue;
            model.CrewBack += OnCrewBack;
            model.CrewSent += OnCrewSent;
            model.CrewWaiting += e => Tint(e.Crew);
            model.CrewDone += OnCrewDone;
            model.CrewRevealed += OnRevealed;
        }

        public void Attach(VisualElement panelRoot, VisualElement layer)
        {
            _labels.Attach(panelRoot, layer);
        }

        public void Build(Transform root, BoardLayout layout)
        {
            _root = root;
            _layout = layout;
            _labels.AddColumns(_model.ColumnCount);
            AddCrews();
            TieRopes();
        }

        /// <summary>Moves the crews in places to where the places are now, after a place is added.</summary>
        public void Relayout(BoardLayout layout)
        {
            _layout = layout;
            foreach (CrewVisual crew in _crews.Values)
            {
                if (crew.InSlot && !crew.Leaving)
                {
                    crew.Target = _layout.SlotAt(crew.Slot);
                }
            }
        }

        public void Update(float dt, float time)
        {
            MoveCrews(dt, time);
            CountCrewsBehind();
            RemoveGoneCrews();
            foreach (Rope rope in _ropes.Values)
            {
                Tie(rope);
            }
        }

        public void ShowRemaining(Crew crew)
        {
            if (_crews.ContainsKey(crew.Id))
            {
                _labels.ShowCount(crew.Id, crew.Remaining);
            }
        }

        public bool AnyLeaving()
        {
            foreach (CrewVisual crew in _crews.Values)
            {
                if (crew.Leaving)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Where the front crew of a column stands, if the column has one on the board.</summary>
        public bool TryGetFront(int column, out Vector3 position)
        {
            IReadOnlyList<Crew> crews = _model.Column(column);
            if (crews.Count == 0 || !_crews.TryGetValue(crews[0].Id, out CrewVisual front))
            {
                position = default;
                return false;
            }

            position = front.Renderer.transform.position;
            return true;
        }

        public void Clear()
        {
            _labels.Clear();
            _crews.Clear();
            _ropes.Clear();
            _root = null;
            _layout = null;
        }

        public void Dispose()
        {
            _signs.Dispose();
        }

        private void AddCrews()
        {
            for (int c = 0; c < _model.ColumnCount; c++)
            {
                IReadOnlyList<Crew> column = _model.Column(c);
                for (int depth = 0; depth < column.Count; depth++)
                {
                    Crew crew = column[depth];
                    var visual = new CrewVisual { Crew = crew };
                    visual.Renderer = SpriteRenderers.Create(_root, _bucket, _crewMaterial, "Crew " + crew.Id, SortingOrder.Crew);
                    visual.Sign = MakeSign(visual.Renderer.transform, crew.Color);
                    _labels.AddCrew(crew.Id);
                    _crews.Add(crew.Id, visual);
                    Dress(crew);
                    InQueue(visual, c, depth);
                    visual.Place = visual.Target;
                    visual.Size = visual.Scale;
                    visual.Label = visual.LabelTarget;
                    visual.Renderer.transform.position = visual.Target;
                    visual.Renderer.transform.localScale = visual.Scale;
                }
            }
        }

        private void TieRopes()
        {
            foreach (CrewVisual visual in _crews.Values)
            {
                int link = visual.Crew.Link;
                if (link < 0 || _ropes.ContainsKey(link))
                {
                    continue;
                }

                foreach (CrewVisual other in _crews.Values)
                {
                    if (other != visual && other.Crew.Link == link)
                    {
                        SpriteRenderer renderer = SpriteRenderers.Create(_root, _rope, _spriteMaterial, "Rope " + link, SortingOrder.Rope);
                        var rope = new Rope { A = visual.Crew.Id, B = other.Crew.Id, Renderer = renderer };
                        _ropes.Add(link, rope);
                        Tie(rope);
                    }
                }
            }
        }

        private void MoveCrews(float dt, float time)
        {
            float glide = 1f - Mathf.Exp(-Glide * dt);
            _gone.Clear();
            foreach (CrewVisual crew in _crews.Values)
            {
                Transform transform = crew.Renderer.transform;
                crew.Place = Vector3.Lerp(crew.Place, crew.Target, glide);
                crew.Shake = Mathf.Max(0f, crew.Shake - dt);
                crew.Pop = Mathf.Max(0f, crew.Pop - dt);
                float wiggle = crew.Shake > 0f ? Mathf.Sin(crew.Shake * 60f) * 0.1f * crew.Shake / ShakeSeconds : 0f;
                transform.position = crew.Place + new Vector3(wiggle, 0f, 0f);
                if (crew.Leaving)
                {
                    crew.Left -= dt;
                    float t = Mathf.Clamp01(crew.Left / LeaveSeconds);
                    transform.localScale = crew.Scale * t;
                    if (crew.Left <= 0f)
                    {
                        _gone.Add(crew.Crew.Id);
                    }
                }
                else
                {
                    crew.Size = Vector3.Lerp(crew.Size, crew.Scale, glide);
                    float bump = crew.Pop > 0f ? 1f + 0.25f * Mathf.Sin(crew.Pop / PopSeconds * Mathf.PI) : 1f;
                    transform.localScale = crew.Size * bump;
                }

                crew.Label = Mathf.Lerp(crew.Label, crew.LabelTarget, glide);
                Vector3 label = transform.position + new Vector3(0f, -_layout.BucketSize * 0.12f * crew.Label, 0f);
                _labels.PlaceCount(crew.Crew.Id, label, crew.Label, !crew.Leaving && crew.Visible);
                Sit(crew, time);
            }
        }

        private void CountCrewsBehind()
        {
            float back = _layout.BucketSize * _layout.QueueScale(BoardLayout.VisibleDepth - 1);
            for (int c = 0; c < _model.ColumnCount; c++)
            {
                int hidden = _model.Column(c).Count - BoardLayout.VisibleDepth;
                _labels.ShowMore(c, hidden, _layout.QueueAt(c, BoardLayout.VisibleDepth - 1) + new Vector2(back * 0.48f, -back * 0.34f));
            }
        }

        private void RemoveGoneCrews()
        {
            foreach (int id in _gone)
            {
                if (_crews[id].Sitter != null)
                {
                    Object.Destroy(_crews[id].Sitter.gameObject);
                }

                Object.Destroy(_crews[id].Renderer.gameObject);
                _labels.RemoveCrew(id);
                _crews.Remove(id);
            }
        }

        private void Tie(Rope rope)
        {
            bool shown = _crews.TryGetValue(rope.A, out CrewVisual a) && _crews.TryGetValue(rope.B, out CrewVisual b)
                && !a.InSlot && !b.InSlot && a.Visible && b.Visible && a.Crew.Link >= 0 && a.Crew.Link == b.Crew.Link;
            rope.Renderer.enabled = shown;
            if (!shown)
            {
                return;
            }

            // The rope hangs from the buckets' handles, which grow and shrink with the buckets.
            var handles = new Vector3(0f, _layout.BucketSize * 0.36f, 0f);
            float bucketScale = Mathf.Max(SpriteRenderers.HeightScale(_bucket, _layout.BucketSize).y, 0.0001f);
            Transform partner = _crews[rope.B].Renderer.transform;
            Vector3 from = a.Renderer.transform.position + handles * a.Renderer.transform.localScale.y / bucketScale;
            Vector3 to = partner.position + handles * partner.localScale.y / bucketScale;
            Vector3 middle = (from + to) / 2f;
            float length = Vector3.Distance(from, to);
            float width = Mathf.Max(_rope.bounds.size.x, 0.0001f);
            float thick = _layout.BucketSize * 0.22f / Mathf.Max(_rope.bounds.size.y, 0.0001f);
            rope.Renderer.transform.position = middle;
            rope.Renderer.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg);
            rope.Renderer.transform.localScale = new Vector3(length / width, thick, 1f);
        }

        private void OnRefused(SendRefused refused)
        {
            IReadOnlyList<Crew> column = _model.Column(refused.Column);
            if (column.Count == 0 || !_crews.TryGetValue(column[0].Id, out CrewVisual front))
            {
                return;
            }

            front.Shake = ShakeSeconds;
            _sound(SoundKeys.Nope);
            if (column[0].Link >= 0 && _ropes.TryGetValue(column[0].Link, out Rope rope))
            {
                int other = rope.A == column[0].Id ? rope.B : rope.A;
                if (_crews.TryGetValue(other, out CrewVisual partner))
                {
                    partner.Shake = ShakeSeconds;
                }
            }
        }

        private void Requeue()
        {
            for (int c = 0; c < _model.ColumnCount; c++)
            {
                IReadOnlyList<Crew> column = _model.Column(c);
                for (int depth = 0; depth < column.Count; depth++)
                {
                    if (_crews.TryGetValue(column[depth].Id, out CrewVisual visual))
                    {
                        visual.InSlot = false;
                        Dress(column[depth]);
                        InQueue(visual, c, depth);
                    }
                }
            }
        }

        private void OnCrewBack(CrewBack back)
        {
            Requeue();
            if (_crews.TryGetValue(back.Crew.Id, out CrewVisual visual))
            {
                visual.Pop = PopSeconds;
            }
        }

        private void OnRevealed(CrewRevealed revealed)
        {
            Dress(revealed.Crew);
            if (_crews.TryGetValue(revealed.Crew.Id, out CrewVisual visual))
            {
                visual.Pop = PopSeconds;
                _sound(SoundKeys.Reveal);
            }
        }

        private void OnCrewSent(CrewSent sent)
        {
            if (!_crews.TryGetValue(sent.Crew.Id, out CrewVisual visual))
            {
                return;
            }

            visual.Target = _layout.SlotAt(sent.Slot);
            visual.Scale = SpriteRenderers.HeightScale(_bucket, _layout.BucketSize);
            visual.LabelTarget = 1f;
            visual.Visible = true;
            visual.InSlot = true;
            visual.Slot = sent.Slot;
            visual.Renderer.enabled = true;
            visual.Renderer.sortingOrder = SortingOrder.CrewInSlot;
            visual.Sign.enabled = !sent.Crew.Hidden;
            visual.Sign.sortingOrder = SortingOrder.CrewSignInSlot;
            IReadOnlyList<Crew> column = _model.Column(sent.Column);
            for (int depth = 0; depth < column.Count; depth++)
            {
                InQueue(_crews[column[depth].Id], sent.Column, depth);
            }
        }

        private void OnCrewDone(CrewDone done)
        {
            if (_crews.TryGetValue(done.Crew.Id, out CrewVisual visual))
            {
                visual.Leaving = true;
                visual.Left = LeaveSeconds;
                _sound(SoundKeys.Done);
                Color sparkle = Color.Lerp(_colors[done.Crew.Color], Color.white, 0.8f);
                _effects.Sparkles(visual.Renderer.transform.position, _layout.BucketSize * 0.9f, 7, sparkle);
            }
        }

        private void Sit(CrewVisual crew, float time)
        {
            bool waiting = crew.InSlot && !crew.Leaving && crew.Crew.Waiting;
            if (!waiting)
            {
                if (crew.Sitter != null)
                {
                    crew.Sitter.enabled = false;
                }

                return;
            }

            if (crew.Sitter == null)
            {
                string name = "Sitter " + crew.Crew.Id;
                crew.Sitter = SpriteRenderers.Create(_root, _sitterSprite, _crewMaterial, name, SortingOrder.Sitter);
            }

            crew.Sitter.enabled = true;
            crew.Sitter.color = _colors[crew.Crew.Color];
            float bob = Mathf.Abs(Mathf.Sin(time * 2.4f + crew.Crew.Id)) * _layout.BucketSize * 0.05f;
            Vector3 top = crew.Renderer.transform.position + new Vector3(0f, _layout.BucketSize * 0.5f + bob, 0f);
            crew.Sitter.transform.position = top;
            crew.Sitter.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 1.3f + crew.Crew.Id * 2f) * 9f);
            crew.Sitter.transform.localScale = SpriteRenderers.HeightScale(_sitterSprite, _layout.BucketSize * 0.42f);
        }

        private void Dress(Crew crew)
        {
            CrewVisual visual = _crews[crew.Id];
            if (crew.Hidden)
            {
                visual.Renderer.sprite = _cover;
                visual.Renderer.sharedMaterial = _spriteMaterial;
                visual.Renderer.color = Color.white;
                _labels.ShowHidden(crew.Id);
                visual.Sign.enabled = false;
                return;
            }

            visual.Renderer.sprite = _bucket;
            visual.Renderer.sharedMaterial = _crewMaterial;
            _labels.ShowCount(crew.Id, crew.Remaining);
            visual.Sign.enabled = visual.Renderer.enabled;
            Tint(crew);
        }

        private SpriteRenderer MakeSign(Transform bucket, int color)
        {
            SpriteRenderer sign = SpriteRenderers.Create(bucket, _signs.For(color), _spriteMaterial, "Sign", 0);
            Vector3 size = _bucket.bounds.size;
            sign.transform.localPosition = new Vector3(-size.x * 0.33f, size.y * 0.22f, 0f);
            float scale = size.y * 0.28f;
            sign.transform.localScale = new Vector3(scale, scale, 1f);
            return sign;
        }

        private void Tint(Crew crew)
        {
            if (crew.Hidden || !_crews.TryGetValue(crew.Id, out CrewVisual visual))
            {
                return;
            }

            Color color = _colors[crew.Color];
            if (crew.Waiting)
            {
                Color.RGBToHSV(color, out float h, out float s, out float v);
                color = Color.HSVToRGB(h, s * 0.95f, v * 0.88f);
            }

            visual.Renderer.color = color;
        }

        private void InQueue(CrewVisual visual, int column, int depth)
        {
            visual.Target = _layout.QueueAt(column, depth);
            visual.Scale = SpriteRenderers.HeightScale(visual.Renderer.sprite, _layout.BucketSize * _layout.QueueScale(depth));
            visual.LabelTarget = _layout.QueueScale(depth);
            visual.Visible = depth < BoardLayout.VisibleDepth;
            visual.Renderer.enabled = visual.Visible;
            visual.Renderer.sortingOrder = SortingOrder.Crew - depth;
            visual.Sign.enabled = visual.Visible && !visual.Crew.Hidden;
            visual.Sign.sortingOrder = SortingOrder.CrewSign - depth;
        }
    }
}
