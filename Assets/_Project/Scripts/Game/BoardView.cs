using System;
using System.Collections.Generic;
using Chromacolony.Core;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Chromacolony.Game
{
    public sealed class BoardView : IDisposable
    {
        private const float CelebrateSeconds = 2.4f;

        private const int TrayBorder = 110;
        private const float TrayPixelsPerUnit = 340f;

        private const float TrayAlpha = 0.55f;

        private const float PointerTurn = 32f;
        private const float PointerBucketShare = 0.7f;
        private const float PointerNodRate = 4f;
        private const float PointerNod = 0.14f;
        private const float PointerRestX = 0.78f;
        private const float PointerRestY = 0.5f;

        private const float NestSlotShare = 0.62f;

        private const float CrabCells = 1.25f;
        private const float CrabMinSize = 0.5f;
        private const float CrabMaxSize = 0.85f;

        private const float LiftTwinkleCells = 1.1f;
        private const float LiftTwinkleMin = 0.4f;
        private const float LiftTwinkleMax = 0.8f;

        // Crabs come home in a stream, and one puff at a time is enough for it.
        private const float PuffGapSeconds = 0.12f;
        private const float PuffSize = 0.55f;

        private const float DancerMaxSize = 1.05f;
        private const float DancerSlotShare = 0.7f;
        private const float DanceSpotLift = 0.12f;
        private const float DanceEndsEarlySeconds = 0.2f;

        private readonly GameModel _model;
        private readonly Material _spriteMaterial;
        private readonly Color[] _colors;
        private readonly Sprite _slot;
        private readonly Sprite _castle;
        private readonly Sprite _pointerSprite;
        private readonly Sprite _tray;
        private readonly Sprite _softOval = SoftOval();
        private readonly Effects _effects;
        private readonly List<SpriteRenderer> _nests = new List<SpriteRenderer>();
        private readonly CrabView _crabs;
        private readonly CrewsView _crews;
        private readonly MosaicView _mosaic;
        private float _lastPuff;
        private SpriteRenderer _pointer;
        private int _pointedColumn = -1;
        private float _pointerTime;
        private float _time;
        private float _celebration;
        private int _picks;
        private GameObject _root;
        private BoardLayout _layout;

        public BoardLayout Layout => _layout;

        public Action<string> Sound { get; set; }

        private static Color Gold => new Color(1f, 0.88f, 0.55f);

        public bool Busy => _crabs.Walking > 0 || _crews.AnyLeaving() || _celebration > 0f;

        public BoardView(Camera camera, GameModel model, Material crewMaterial, Material spriteMaterial, Color[] colors, Effects effects,
            GameContent content)
        {
            _model = model;
            _spriteMaterial = spriteMaterial;
            _colors = colors;
            _effects = effects;
            _slot = content.Slot;
            _castle = content.Castle;
            _pointerSprite = content.Pointer;
            _tray = Sliced(content.Panel);
            _crabs = new CrabView(content.CrabRig, _softOval);
            _crews = new CrewsView(model, camera, crewMaterial, spriteMaterial, colors, effects, content, sound => Sound?.Invoke(sound));
            _mosaic = new MosaicView(crewMaterial, spriteMaterial, colors, content.Pebble, _softOval, effects);

            _crabs.Lifted = at =>
            {
                float twinkle = Mathf.Clamp(_layout.Cell * LiftTwinkleCells, LiftTwinkleMin, LiftTwinkleMax);
                _effects.Twinkle(at, twinkle, Gold);
                Sound?.Invoke(Sounds.Pick(_picks++));
            };
            _crabs.Entered = at =>
            {
                if (_time - _lastPuff > PuffGapSeconds)
                {
                    _lastPuff = _time;
                    _effects.Puff(at, PuffSize);
                }
            };
            model.SlotAdded += OnSlotAdded;
            model.BoardStarted += OnBoardStarted;
            model.CrabSent += OnCrabSent;
        }

        public void Attach(VisualElement panelRoot, VisualElement layer)
        {
            _crews.Attach(panelRoot, layer);
        }

        public void ShowOff(Vector2 at, bool on)
        {
            _crabs.ShowOff(at, on);
        }

        public int ColumnUnder(Vector2 world)
        {
            return _layout == null ? -1 : _layout.ColumnUnder(world);
        }

        public int SlotUnder(Vector2 world)
        {
            if (_layout == null)
            {
                return -1;
            }

            for (int s = 0; s < _model.SlotCount; s++)
            {
                if (_model.SlotAt(s) != null && _layout.SlotCovers(s, world))
                {
                    return s;
                }
            }

            return -1;
        }

        public void Clear()
        {
            _crabs.Clear();
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }

            _crews.Clear();
            _mosaic.Clear();
            _nests.Clear();
            _celebration = 0f;
            _pointer = null;
            _pointedColumn = -1;
            _layout = null;
        }

        public void Dispose()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
            }

            _model.SlotAdded -= OnSlotAdded;
            _model.BoardStarted -= OnBoardStarted;
            _model.CrabSent -= OnCrabSent;
            _crabs.Dispose();
            _crews.Dispose();
            if (_softOval != null)
            {
                Object.Destroy(_softOval.texture);
                Object.Destroy(_softOval);
            }

            if (_tray != null)
            {
                Object.Destroy(_tray);
            }
        }

        public void Celebrate()
        {
            if (_layout == null)
            {
                return;
            }

            _celebration = CelebrateSeconds;
            _mosaic.ShowPicture(_model.Board, _layout);
            List<int> colors = PictureColors(_model.Board);
            var dancers = new Color[_model.SlotCount];
            var spots = new Vector2[_model.SlotCount];
            for (int i = 0; i < dancers.Length; i++)
            {
                dancers[i] = _colors[colors[i % colors.Count]];
                spots[i] = _layout.SlotAt(i) + new Vector2(0f, DanceSpotLift);
            }

            _crabs.Dance(_layout.Gate, dancers, spots, Mathf.Min(DancerMaxSize, _layout.SlotSize * DancerSlotShare),
                CelebrateSeconds - DanceEndsEarlySeconds);
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
                _mosaic.Reassemble(CelebrateSeconds - _celebration, _layout);
            }

            _crews.Update(dt, _time);
            _mosaic.Light(WaitingColorMask(), _time, _layout);
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
                _pointer = SpriteRenderers.Create(_root.transform, _pointerSprite, _spriteMaterial, "Pointer", SortingOrder.Pointer);
                _pointer.transform.localScale = SpriteRenderers.HeightScale(_pointerSprite, _layout.BucketSize * PointerBucketShare);

                _pointer.transform.localRotation = Quaternion.Euler(0f, 0f, PointerTurn);
            }

            _pointer.enabled = column >= 0;
        }

        private static List<int> PictureColors(BoardSpec board)
        {
            var colors = new List<int>();
            foreach (int color in board.Cells)
            {
                if (color >= 0 && !colors.Contains(color))
                {
                    colors.Add(color);
                }
            }

            return colors;
        }

        private void Aim(float dt)
        {
            if (_pointer == null || _pointedColumn < 0)
            {
                return;
            }

            if (!_crews.TryGetFront(_pointedColumn, out Vector3 front))
            {
                _pointer.enabled = false;
                return;
            }

            _pointerTime += dt;
            var toward = new Vector3(-Mathf.Cos(PointerTurn * Mathf.Deg2Rad), -Mathf.Sin(PointerTurn * Mathf.Deg2Rad), 0f);
            float nod = Mathf.Abs(Mathf.Sin(_pointerTime * PointerNodRate)) * _layout.BucketSize * PointerNod;
            var rest = new Vector3(_layout.BucketSize * PointerRestX, _layout.BucketSize * PointerRestY, 0f);
            _pointer.transform.position = front + rest + toward * nod;
        }

        private void OnBoardStarted(BoardStarted started)
        {
            Build(started.Board);
        }

        private void Build(BoardSpec board)
        {
            Clear();
            _root = new GameObject("Board");
            _layout = new BoardLayout(board.Width, board.Height, _model.SlotCount, _model.ColumnCount);

            SpriteRenderer castle = SpriteRenderers.Create(_root.transform, _castle, _spriteMaterial, "Castle", SortingOrder.Castle);
            SpriteRenderers.Place(castle, _layout.CastleCenter, SpriteRenderers.HeightScale(_castle, _layout.CastleHeight));

            SpriteRenderer tray = SpriteRenderers.Create(_root.transform, _tray, _spriteMaterial, "Tray", SortingOrder.Tray);
            tray.color = new Color(1f, 1f, 1f, TrayAlpha);
            tray.drawMode = SpriteDrawMode.Sliced;
            tray.size = _layout.TraySize;
            tray.transform.position = _layout.MosaicCenter;

            for (int s = 0; s < _model.SlotCount; s++)
            {
                AddNest(s);
            }

            _mosaic.Build(board, _layout, _root.transform);
            _crews.Build(_root.transform, _layout);
        }

        private void AddNest(int slot)
        {
            SpriteRenderer nest = SpriteRenderers.Create(_root.transform, _slot, _spriteMaterial, "Slot " + slot, SortingOrder.Slot);
            SpriteRenderers.Place(nest, _layout.SlotAt(slot), SpriteRenderers.HeightScale(_slot, _layout.SlotSize * NestSlotShare));
            _nests.Add(nest);
        }

        private void OnSlotAdded(SlotAdded added)
        {
            _layout = new BoardLayout(_model.Board.Width, _model.Board.Height, _model.SlotCount, _model.ColumnCount);
            AddNest(added.Slot);
            for (int s = 0; s < _nests.Count; s++)
            {
                SpriteRenderers.Place(_nests[s], _layout.SlotAt(s), SpriteRenderers.HeightScale(_slot, _layout.SlotSize * NestSlotShare));
            }

            _crews.Relayout(_layout);
        }

        private void OnCrabSent(CrabSent sent)
        {
            SpriteRenderer pebble = _mosaic.Take(sent.Cell);
            if (pebble != null)
            {
                float size = Mathf.Clamp(_layout.Cell * CrabCells, CrabMinSize, CrabMaxSize);
                _crabs.Send(_layout.SlotAt(sent.Slot), pebble, _layout.Gate, _colors[sent.Crew.Color], size);
            }

            _crews.ShowRemaining(sent.Crew);
        }

        private int WaitingColorMask()
        {
            int mask = 0;
            for (int s = 0; s < _model.SlotCount; s++)
            {
                Crew crew = _model.SlotAt(s);
                if (crew != null && crew.Waiting)
                {
                    mask |= 1 << crew.Color;
                }
            }

            return mask;
        }

        private static Sprite SoftOval()
        {
            const int Size = 32;
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = RuntimeArt.SoftOval,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float d = new Vector2(x + 0.5f - Size / 2f, y + 0.5f - Size / 2f).magnitude / (Size / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, a * a * (3f - 2f * a)));
                }
            }

            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size, 0, SpriteMeshType.FullRect);
            sprite.name = texture.name;
            return sprite;
        }

        private static Sprite Sliced(Sprite sprite)
        {
            Rect rect = sprite.rect;
            Sprite sliced = Sprite.Create(sprite.texture, rect, new Vector2(0.5f, 0.5f), TrayPixelsPerUnit, 0, SpriteMeshType.FullRect,
                new Vector4(TrayBorder, TrayBorder, TrayBorder, TrayBorder));
            sliced.name = RuntimeArt.Tray;
            return sliced;
        }
    }
}
