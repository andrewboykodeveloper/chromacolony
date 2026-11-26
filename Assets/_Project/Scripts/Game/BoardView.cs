using System.Collections.Generic;
using Chromacolony.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chromacolony.Game
{
    public sealed class BoardView
    {
        private sealed class CrewVisual
        {
            public Crew Crew;
            public SpriteRenderer Renderer;
            public Label Count;

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

        private sealed class Piece
        {
            public SpriteRenderer Renderer;
            public float Delay;
            public bool Twinkled;
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
        private const float CelebrateSeconds = 2.4f;
        private const float PictureSeconds = 1.1f;
        private const int Dancers = 6;
        private const string CountClass = "crew-count";
        private const string MoreClass = "crew-more";

        private const int PebbleOrder = 2;
        private const int GlowOrder = 3;
        private const int LitOrder = 4;

        private const int TrayBorder = 110;
        private const float TrayPixelsPerUnit = 340f;

        private const float TrayAlpha = 0.55f;

        private const float PointerTurn = 32f;

        private readonly Camera _camera;
        private readonly GameModel _model;
        private readonly Material _crewMaterial;
        private readonly Material _spriteMaterial;
        private readonly Color[] _colors;
        private readonly VisualElement _panelRoot;
        private readonly VisualElement _labels;
        private readonly Sprite _pebble;
        private readonly Sprite _bucket;
        private readonly Sprite _cover;
        private readonly Sprite _slot;
        private readonly Sprite _castle;
        private readonly Sprite _rope;
        private readonly Dictionary<int, CrewVisual> _crews = new Dictionary<int, CrewVisual>();
        private readonly Dictionary<int, Rope> _ropes = new Dictionary<int, Rope>();
        private readonly List<SpriteRenderer> _nests = new List<SpriteRenderer>();
        private readonly Sprite _pointerSprite;
        private readonly Sprite _sitterSprite;
        private readonly Sprite _tray;
        private readonly Sprite _glow = CrabView.SoftOval();
        private readonly Effects _effects;
        private readonly List<Label> _more = new List<Label>();
        private readonly List<Piece> _picture = new List<Piece>();
        private readonly Signs _signs = new Signs();
        private Vector3 _pebbleScale;
        private float _lastPuff;
        private SpriteRenderer _pointer;
        private int _pointedColumn = -1;
        private float _pointerTime;
        private float _time;
        private float _celebration;
        private int _picks;
        private readonly List<int> _gone = new List<int>();
        private readonly CrabView _crabs;
        private GameObject _root;
        private SpriteRenderer[] _pebbles = new SpriteRenderer[0];
        private SpriteRenderer[] _glows = new SpriteRenderer[0];
        private BoardLayout _layout;

        public BoardLayout Layout => _layout;

        public System.Action<string> Sound { get; set; }

        private static Color Gold => new Color(1f, 0.88f, 0.55f);

        public bool Busy => _crabs.Walking > 0 || _crews.Count > 0 && AnyLeaving() || _celebration > 0f;

        public BoardView(Camera camera, GameModel model, Material crewMaterial, Material spriteMaterial, Color[] colors, VisualElement panelRoot,
            VisualElement labels, Effects effects, GameContent content)
        {
            _camera = camera;
            _model = model;
            _crewMaterial = crewMaterial;
            _spriteMaterial = spriteMaterial;
            _colors = colors;
            _panelRoot = panelRoot;
            _labels = labels;
            _effects = effects;
            _pebble = content.Pebble;
            _bucket = content.Bucket;
            _cover = content.BucketCover;
            _slot = content.Slot;
            _castle = content.Castle;
            _rope = content.BucketRope;
            _pointerSprite = content.Pointer;
            _sitterSprite = content.Crab;
            _tray = Sliced(content.Panel);
            _crabs = new CrabView(crewMaterial, spriteMaterial, content);

            _crabs.Lifted = at =>
            {
                _effects.Twinkle(at, _layout == null ? 0.5f : Mathf.Clamp(_layout.Cell * 1.1f, 0.4f, 0.8f), Gold);
                Sound?.Invoke(Sounds.Pick(_picks++));
            };
            _crabs.Entered = at =>
            {
                if (_time - _lastPuff > 0.12f)
                {
                    _lastPuff = _time;
                    _effects.Puff(at, 0.55f);
                }
            };
            model.SendRefused += OnRefused;
            model.SlotAdded += OnSlotAdded;
            model.QueueShuffled += Requeue;
            model.CrewBack += OnCrewBack;
            model.BoardStarted += e => Build(e.Board);
            model.CrewSent += OnCrewSent;
            model.CrabSent += OnCrabSent;
            model.CrewWaiting += e => Tint(e.Crew);
            model.CrewDone += OnCrewDone;
            model.CrewRevealed += OnRevealed;
        }

        public void ShowOff(Vector2 at, bool on)
        {
            _crabs.ShowOff(at, on);
        }

        public int ColumnAt(Vector2 world)
        {
            if (_layout == null)
            {
                return -1;
            }

            float top = _layout.QueueAt(0, 0).y + _layout.BucketSize * 0.75f;
            float bottom = _layout.QueueAt(0, BoardLayout.VisibleDepth - 1).y - _layout.BucketSize * 0.75f;
            if (world.y > top || world.y < bottom)
            {
                return -1;
            }

            int best = -1;
            float bestDistance = float.MaxValue;
            for (int c = 0; c < _model.ColumnCount; c++)
            {
                float distance = Mathf.Abs(world.x - _layout.QueueAt(c, 0).x);
                if (distance < bestDistance && distance < _layout.BucketSize)
                {
                    best = c;
                    bestDistance = distance;
                }
            }

            return best;
        }

        public void Clear()
        {
            _crabs.Clear();
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }

            foreach (CrewVisual crew in _crews.Values)
            {
                crew.Count.RemoveFromHierarchy();
            }

            foreach (Label more in _more)
            {
                more.RemoveFromHierarchy();
            }

            _crews.Clear();
            _ropes.Clear();
            _nests.Clear();
            _more.Clear();
            _picture.Clear();
            _celebration = 0f;
            _pointer = null;
            _pointedColumn = -1;
            _pebbles = new SpriteRenderer[0];
            _glows = new SpriteRenderer[0];
            _layout = null;
        }

        public void Celebrate()
        {
            if (_layout == null)
            {
                return;
            }

            _celebration = CelebrateSeconds;
            BoardSpec board = _model.Board;
            var colors = new List<int>();
            for (int cell = 0; cell < board.Cells.Count; cell++)
            {
                int color = board.Cells[cell];
                if (color < 0)
                {
                    continue;
                }

                if (!colors.Contains(color))
                {
                    colors.Add(color);
                }

                SpriteRenderer pebble = Make(_pebble, _crewMaterial, "Picture " + cell, 2);
                pebble.color = _colors[color];
                pebble.transform.position = _layout.PebbleAt(cell);
                pebble.transform.localScale = Vector3.zero;
                int row = cell / board.Width;
                float delay = (board.Height - 1 - row) / (float)Mathf.Max(board.Height - 1, 1) * PictureSeconds * 0.7f;
                _picture.Add(new Piece { Renderer = pebble, Delay = delay, Twinkled = false });
            }

            var dancers = new Color[_model.SlotCount];
            var spots = new Vector2[_model.SlotCount];
            for (int i = 0; i < dancers.Length; i++)
            {
                dancers[i] = _colors[colors[i % colors.Count]];
                spots[i] = _layout.SlotAt(i) + new Vector2(0f, 0.12f);
            }

            _crabs.Dance(_layout.Gate, dancers, spots, Mathf.Min(1.05f, _layout.SlotSize * 0.7f), CelebrateSeconds - 0.2f);
            Sound?.Invoke(SoundKeys.Win);
        }

        public void Update(float dt, float speed)
        {
            _crabs.Update(dt, speed);
            if (_layout == null)
            {
                return;
            }

            _time += dt;
            if (_celebration > 0f)
            {
                _celebration -= dt * speed;
                Reassemble(CelebrateSeconds - _celebration);
            }

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
                Vector2 point = WorldToPanel.Point(_camera, transform.position + new Vector3(0f, -_layout.BucketSize * 0.12f * crew.Label, 0f), _panelRoot);
                crew.Count.style.left = point.x;
                crew.Count.style.top = point.y;
                crew.Count.style.scale = new Scale(new Vector3(crew.Label, crew.Label, 1f));
                crew.Count.style.opacity = crew.Leaving ? 0f : crew.Visible ? 1f : 0f;
                Sit(crew);
            }

            Light();

            for (int c = 0; c < _more.Count; c++)
            {
                int hidden = _model.Column(c).Count - BoardLayout.VisibleDepth;
                _more[c].text = hidden > 0 ? "+" + hidden : string.Empty;
                _more[c].EnableInClassList(ScreenHost.HiddenClass, hidden <= 0);
                float back = _layout.BucketSize * _layout.QueueScale(BoardLayout.VisibleDepth - 1);
                Vector2 corner = WorldToPanel.Point(_camera, _layout.QueueAt(c, BoardLayout.VisibleDepth - 1) + new Vector2(back * 0.48f, -back * 0.34f), _panelRoot);
                _more[c].style.left = corner.x;
                _more[c].style.top = corner.y;
            }

            foreach (int id in _gone)
            {
                if (_crews[id].Sitter != null)
                {
                    Object.Destroy(_crews[id].Sitter.gameObject);
                }

                Object.Destroy(_crews[id].Renderer.gameObject);
                _crews[id].Count.RemoveFromHierarchy();
                _crews.Remove(id);
            }

            foreach (Rope rope in _ropes.Values)
            {
                Tie(rope);
            }

            Aim(dt);
        }

        public void Point(int column)
        {
            _pointedColumn = column;
            if (_root == null)
            {
                return;
            }

            if (_pointer == null)
            {
                _pointer = Make(_pointerSprite, _spriteMaterial, "Pointer", 60);
                _pointer.transform.localScale = Scale(_pointerSprite, _layout.BucketSize * 0.7f);

                _pointer.transform.localRotation = Quaternion.Euler(0f, 0f, PointerTurn);
            }

            _pointer.enabled = column >= 0;
        }

        private void Aim(float dt)
        {
            if (_pointer == null || _pointedColumn < 0)
            {
                return;
            }

            IReadOnlyList<Crew> column = _model.Column(_pointedColumn);
            if (column.Count == 0 || !_crews.TryGetValue(column[0].Id, out CrewVisual front))
            {
                _pointer.enabled = false;
                return;
            }

            _pointerTime += dt;
            var toward = new Vector3(-Mathf.Cos(PointerTurn * Mathf.Deg2Rad), -Mathf.Sin(PointerTurn * Mathf.Deg2Rad), 0f);
            float nod = Mathf.Abs(Mathf.Sin(_pointerTime * 4f)) * _layout.BucketSize * 0.14f;
            var rest = new Vector3(_layout.BucketSize * 0.78f, _layout.BucketSize * 0.5f, 0f);
            _pointer.transform.position = front.Renderer.transform.position + rest + toward * nod;
        }

        private void Tie(Rope rope)
        {
            bool shown = _crews.TryGetValue(rope.A, out CrewVisual a) && _crews.TryGetValue(rope.B, out CrewVisual b) && !a.InSlot && !b.InSlot && a.Visible && b.Visible
                && a.Crew.Link >= 0 && a.Crew.Link == b.Crew.Link;
            rope.Renderer.enabled = shown;
            if (!shown)
            {
                return;
            }

            var handles = new Vector3(0f, _layout.BucketSize * 0.36f, 0f);
            Vector3 from = a.Renderer.transform.position + handles * a.Renderer.transform.localScale.y / Mathf.Max(Scale(_bucket, _layout.BucketSize).y, 0.0001f);
            Vector3 to = _crews[rope.B].Renderer.transform.position + handles * _crews[rope.B].Renderer.transform.localScale.y / Mathf.Max(Scale(_bucket, _layout.BucketSize).y, 0.0001f);
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
            Sound?.Invoke(SoundKeys.Nope);
            if (column[0].Link >= 0 && _ropes.TryGetValue(column[0].Link, out Rope rope))
            {
                int other = rope.A == column[0].Id ? rope.B : rope.A;
                if (_crews.TryGetValue(other, out CrewVisual partner))
                {
                    partner.Shake = ShakeSeconds;
                }
            }
        }

        private bool AnyLeaving()
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

        private void Build(BoardSpec board)
        {
            Clear();
            _root = new GameObject("Board");
            _layout = new BoardLayout(board.Width, board.Height, _model.SlotCount, _model.ColumnCount);

            SpriteRenderer castle = Make(_castle, _spriteMaterial, "Castle", 1);
            Place(castle, _layout.CastleCenter, Scale(_castle, _layout.CastleHeight));

            SpriteRenderer tray = Make(_tray, _spriteMaterial, "Tray", -5);
            tray.color = new Color(1f, 1f, 1f, TrayAlpha);
            tray.drawMode = SpriteDrawMode.Sliced;
            tray.size = _layout.TraySize;
            tray.transform.position = _layout.MosaicCenter;

            for (int c = 0; c < _model.ColumnCount; c++)
            {
                var more = new Label();
                more.AddToClassList(MoreClass);
                more.pickingMode = PickingMode.Ignore;
                _labels.Add(more);
                _more.Add(more);
            }

            for (int s = 0; s < _model.SlotCount; s++)
            {
                AddNest(s);
            }

            _pebbles = new SpriteRenderer[board.Width * board.Height];
            _glows = new SpriteRenderer[_pebbles.Length];
            for (int cell = 0; cell < _pebbles.Length; cell++)
            {
                int color = board.Cells[cell];
                if (color < 0)
                {
                    continue;
                }

                SpriteRenderer pebble = Make(_pebble, _crewMaterial, "Pebble " + cell, PebbleOrder);
                pebble.color = _colors[color];
                _pebbleScale = Scale(_pebble, _layout.Cell * 0.94f);
                Place(pebble, _layout.PebbleAt(cell), _pebbleScale);
                _pebbles[cell] = pebble;
            }

            for (int c = 0; c < _model.ColumnCount; c++)
            {
                IReadOnlyList<Crew> column = _model.Column(c);
                for (int depth = 0; depth < column.Count; depth++)
                {
                    Crew crew = column[depth];
                    var visual = new CrewVisual { Crew = crew };
                    visual.Renderer = Make(_bucket, _crewMaterial, "Crew " + crew.Id, 10);
                    visual.Sign = MakeSign(visual.Renderer.transform, crew.Color);
                    visual.Count = new Label();
                    visual.Count.AddToClassList(CountClass);
                    visual.Count.pickingMode = PickingMode.Ignore;
                    _labels.Add(visual.Count);
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
                        var rope = new Rope { A = visual.Crew.Id, B = other.Crew.Id, Renderer = Make(_rope, _spriteMaterial, "Rope " + link, 9) };
                        _ropes.Add(link, rope);
                        Tie(rope);
                    }
                }
            }
        }

        public int SlotAt(Vector2 world)
        {
            if (_layout == null)
            {
                return -1;
            }

            for (int s = 0; s < _model.SlotCount; s++)
            {
                if (_model.SlotAt(s) != null && Vector2.Distance(world, _layout.SlotAt(s)) < _layout.SlotSize * 0.6f)
                {
                    return s;
                }
            }

            return -1;
        }

        private void AddNest(int slot)
        {
            SpriteRenderer nest = Make(_slot, _spriteMaterial, "Slot " + slot, 0);
            Place(nest, _layout.SlotAt(slot), Scale(_slot, _layout.SlotSize * 0.62f));
            _nests.Add(nest);
        }

        private void OnSlotAdded(SlotAdded added)
        {
            _layout = new BoardLayout(_model.Board.Width, _model.Board.Height, _model.SlotCount, _model.ColumnCount);
            AddNest(added.Slot);
            for (int s = 0; s < _nests.Count; s++)
            {
                Place(_nests[s], _layout.SlotAt(s), Scale(_slot, _layout.SlotSize * 0.62f));
            }

            foreach (CrewVisual crew in _crews.Values)
            {
                if (crew.InSlot && !crew.Leaving)
                {
                    crew.Target = _layout.SlotAt(crew.Slot);
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
                Sound?.Invoke(SoundKeys.Reveal);
            }
        }

        private void OnCrewSent(CrewSent sent)
        {
            if (!_crews.TryGetValue(sent.Crew.Id, out CrewVisual visual))
            {
                return;
            }

            visual.Target = _layout.SlotAt(sent.Slot);
            visual.Scale = Scale(_bucket, _layout.BucketSize);
            visual.LabelTarget = 1f;
            visual.Visible = true;
            visual.InSlot = true;
            visual.Slot = sent.Slot;
            visual.Renderer.enabled = true;
            visual.Renderer.sortingOrder = 20;
            visual.Sign.enabled = !sent.Crew.Hidden;
            visual.Sign.sortingOrder = 21;
            IReadOnlyList<Crew> column = _model.Column(sent.Column);
            for (int depth = 0; depth < column.Count; depth++)
            {
                InQueue(_crews[column[depth].Id], sent.Column, depth);
            }
        }

        private void OnCrabSent(CrabSent sent)
        {
            SpriteRenderer pebble = _pebbles[sent.Cell];
            if (pebble != null)
            {
                float size = Mathf.Clamp(_layout.Cell * 1.25f, 0.5f, 0.85f);
                _crabs.Send(_layout.SlotAt(sent.Slot), pebble, _layout.Gate, _colors[sent.Crew.Color], size);
                _pebbles[sent.Cell] = null;
            }

            if (_crews.TryGetValue(sent.Crew.Id, out CrewVisual visual))
            {
                visual.Count.text = sent.Crew.Remaining.ToString();
            }
        }

        private void OnCrewDone(CrewDone done)
        {
            if (_crews.TryGetValue(done.Crew.Id, out CrewVisual visual))
            {
                visual.Leaving = true;
                visual.Left = LeaveSeconds;
                Sound?.Invoke(SoundKeys.Done);
                _effects.Sparkles(visual.Renderer.transform.position, _layout.BucketSize * 0.9f, 7, Color.Lerp(_colors[done.Crew.Color], Color.white, 0.8f));
            }
        }

        private void Light()
        {
            int lit = 0;
            for (int s = 0; s < _model.SlotCount; s++)
            {
                Crew crew = _model.SlotAt(s);
                if (crew != null && crew.Waiting)
                {
                    lit |= 1 << crew.Color;
                }
            }

            float breath = 0.5f + 0.5f * Mathf.Sin(_time * 5f);
            IReadOnlyList<int> cells = _model.Board.Cells;
            for (int cell = 0; cell < _pebbles.Length; cell++)
            {
                SpriteRenderer pebble = _pebbles[cell];
                SpriteRenderer glow = _glows[cell];
                bool on = pebble != null && (lit & (1 << cells[cell])) != 0;
                if (!on)
                {
                    if (glow != null)
                    {
                        glow.enabled = false;
                    }

                    if (pebble != null)
                    {
                        pebble.sortingOrder = PebbleOrder;
                        pebble.transform.localScale = _pebbleScale;
                    }

                    continue;
                }

                if (glow == null)
                {
                    glow = Make(_glow, _spriteMaterial, "Glow " + cell, GlowOrder);
                    _glows[cell] = glow;
                }

                var light = Color.Lerp(_colors[cells[cell]], Color.white, 0.55f);
                light.a = 0.55f + 0.4f * breath;
                glow.enabled = true;
                glow.color = light;
                glow.transform.position = pebble.transform.position;
                glow.transform.localScale = Scale(_glow, _layout.Cell * (1.75f + 0.25f * breath));
                pebble.sortingOrder = LitOrder;
                pebble.transform.localScale = _pebbleScale * (1f + 0.08f * breath);
            }
        }

        private void Sit(CrewVisual crew)
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
                crew.Sitter = Make(_sitterSprite, _crewMaterial, "Sitter " + crew.Crew.Id, 22);
            }

            crew.Sitter.enabled = true;
            crew.Sitter.color = _colors[crew.Crew.Color];
            float bob = Mathf.Abs(Mathf.Sin(_time * 2.4f + crew.Crew.Id)) * _layout.BucketSize * 0.05f;
            Vector3 top = crew.Renderer.transform.position + new Vector3(0f, _layout.BucketSize * 0.5f + bob, 0f);
            crew.Sitter.transform.position = top;
            crew.Sitter.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_time * 1.3f + crew.Crew.Id * 2f) * 9f);
            crew.Sitter.transform.localScale = Scale(_sitterSprite, _layout.BucketSize * 0.42f);
        }

        private void Reassemble(float t)
        {
            for (int i = 0; i < _picture.Count; i++)
            {
                Piece piece = _picture[i];
                float grow = Mathf.Clamp01((t - piece.Delay) / 0.22f);
                if (grow <= 0f)
                {
                    continue;
                }

                float pop = grow < 1f ? Mathf.Sin(grow * Mathf.PI * 0.75f) / Mathf.Sin(Mathf.PI * 0.75f) : 1f;
                piece.Renderer.transform.localScale = Scale(_pebble, _layout.Cell * 0.94f) * pop;
                if (!piece.Twinkled)
                {
                    piece.Twinkled = true;
                    if (i % 4 == 0)
                    {
                        _effects.Twinkle(piece.Renderer.transform.position, _layout.Cell * 1.2f);
                    }
                }
            }
        }

        private static Sprite Sliced(Sprite sprite)
        {
            Rect rect = sprite.rect;
            return Sprite.Create(sprite.texture, rect, new Vector2(0.5f, 0.5f), TrayPixelsPerUnit, 0, SpriteMeshType.FullRect,
                new Vector4(TrayBorder, TrayBorder, TrayBorder, TrayBorder));
        }

        private void Dress(Crew crew)
        {
            CrewVisual visual = _crews[crew.Id];
            if (crew.Hidden)
            {
                visual.Renderer.sprite = _cover;
                visual.Renderer.sharedMaterial = _spriteMaterial;
                visual.Renderer.color = Color.white;
                visual.Count.text = "?";
                visual.Sign.enabled = false;
                return;
            }

            visual.Renderer.sprite = _bucket;
            visual.Renderer.sharedMaterial = _crewMaterial;
            visual.Count.text = crew.Remaining.ToString();
            visual.Sign.enabled = visual.Renderer.enabled;
            Tint(crew);
        }

        private SpriteRenderer MakeSign(Transform bucket, int color)
        {
            var go = new GameObject("Sign");
            go.transform.SetParent(bucket, false);
            Vector3 size = _bucket.bounds.size;
            go.transform.localPosition = new Vector3(-size.x * 0.33f, size.y * 0.22f, 0f);
            float scale = size.y * 0.28f;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = _signs.For(color);
            renderer.sharedMaterial = _spriteMaterial;
            return renderer;
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
            visual.Scale = Scale(visual.Renderer.sprite, _layout.BucketSize * _layout.QueueScale(depth));
            visual.LabelTarget = _layout.QueueScale(depth);
            visual.Visible = depth < BoardLayout.VisibleDepth;
            visual.Renderer.enabled = visual.Visible;
            visual.Renderer.sortingOrder = 10 - depth;
            visual.Sign.enabled = visual.Visible && !visual.Crew.Hidden;
            visual.Sign.sortingOrder = 11 - depth;
        }

        private SpriteRenderer Make(Sprite sprite, Material material, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static void Place(SpriteRenderer renderer, Vector2 position, Vector3 scale)
        {
            renderer.transform.position = position;
            renderer.transform.localScale = scale;
        }

        private static Vector3 Scale(Sprite sprite, float height)
        {
            float s = height / Mathf.Max(sprite.bounds.size.y, 0.0001f);
            return new Vector3(s, s, 1f);
        }
    }
}
