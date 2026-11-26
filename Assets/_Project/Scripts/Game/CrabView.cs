using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Chromacolony.Game
{
    public sealed class CrabView : IDisposable
    {
        private enum Leg
        {
            Out,
            Lift,
            Home,
            In,
        }

        private sealed class Crab
        {
            public readonly Transform[] Claws = new Transform[2];
            public readonly Transform[] Legs = new Transform[6];
            public Transform Root;
            public Transform Body;
            public SpriteRenderer Pebble;
            public bool Carrying;
            public Vector2 Target;
            public Vector2 Gate;
            public Leg Leg;
            public float Size;
            public float Time;
            public float Phase;
            public float Facing = 1f;
            private Vector3 _pebbleFrom;
            private Vector3 _pebbleScaleFrom;
            private Vector3 _pebbleScale;

            public void Paint(Color color)
            {
                foreach (SpriteRenderer renderer in Body.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    renderer.color = color;
                }
            }

            public void Stride()
            {
                for (int i = 0; i < 6; i++)
                {
                    float tripod = (i + i / 3) % 2 == 0 ? 0f : Mathf.PI;
                    float swing = Mathf.Sin(Phase + tripod) * 22f;
                    Legs[i].localRotation = Quaternion.Euler(0f, 0f, -10f + swing - 12f * (i % 3));
                }

                Body.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(Phase)) * 0.04f, 0f);
                Body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Phase) * 4f);
                Root.localScale = new Vector3(Size * Facing, Size, 1f);
            }

            public void Pose(float raise)
            {
                float wave = Mathf.Sin(Phase * 0.5f) * 6f * (1f - raise);
                Claws[0].localRotation = Quaternion.Euler(0f, 0f, 35f - 45f * raise + wave);
                Claws[1].localRotation = Quaternion.Euler(0f, 0f, 35f - 45f * raise - wave);
            }

            public void Wave(float beat)
            {
                float swing = Mathf.Sin(beat) * 28f;
                Claws[0].localRotation = Quaternion.Euler(0f, 0f, -12f + swing);
                Claws[1].localRotation = Quaternion.Euler(0f, 0f, -12f - swing);
            }

            public void Take(int order, float height)
            {
                Carrying = true;
                Pebble.transform.SetParent(Root, true);
                Pebble.sortingOrder = order;
                _pebbleFrom = Pebble.transform.localPosition;
                _pebbleScaleFrom = Pebble.transform.localScale;
                float s = height / Mathf.Max(Pebble.sprite.bounds.size.y, 0.0001f);
                _pebbleScale = new Vector3(Mathf.Sign(_pebbleScaleFrom.x) * s, s, 1f);
            }

            public void Lift(float lift)
            {
                Pebble.transform.localPosition = Vector3.Lerp(_pebbleFrom, new Vector3(0f, 0.62f, 0f), lift);
                Pebble.transform.localScale = Vector3.Lerp(_pebbleScaleFrom, _pebbleScale, lift);
            }

            public void Drop()
            {
                if (Carrying)
                {
                    Pebble.enabled = false;
                    Pebble.transform.SetParent(null, true);
                    Object.Destroy(Pebble.gameObject);
                }

                Carrying = false;
                Pebble = null;
            }
        }

        private const float Speed = 5.5f;
        private const float StepsPerUnit = 2.6f;
        private const float LiftSeconds = 0.18f;
        private const float EnterSeconds = 0.25f;

        private const float PebbleHeight = 0.36f;

        private const float StarSize = 2.9f;

        private readonly Transform _holder;
        private readonly Material _crewMaterial;
        private readonly Sprite _shell;
        private readonly Sprite _claw;
        private readonly Sprite _leg;
        private readonly Sprite _shadow;
        private readonly Material _shadowMaterial;
        private readonly List<Crab> _walking = new List<Crab>();
        private readonly List<Crab> _dancing = new List<Crab>();
        private readonly Stack<Crab> _pool = new Stack<Crab>();
        private int _sent;
        private float _danceLeft;
        private Crab _star;

        public int Walking => _walking.Count;

        public Action<Vector2> Lifted { get; set; }

        public Action<Vector2> Entered { get; set; }

        private static Color StarColor => new Color(0.847f, 0.373f, 0.298f);

        public CrabView(Material crewMaterial, Material spriteMaterial, GameContent content)
        {
            _holder = new GameObject("Crabs").transform;
            _crewMaterial = crewMaterial;
            _shadowMaterial = spriteMaterial;
            _shell = content.CrabShell;
            _claw = content.CrabClaw;
            _leg = content.CrabLeg;
            _shadow = SoftOval();
        }

        public void ShowOff(Vector2 at, bool on)
        {
            if (!on)
            {
                if (_star != null)
                {
                    _star.Root.gameObject.SetActive(false);
                }

                return;
            }

            if (_star == null)
            {
                _star = Make();
                _star.Size = StarSize;
                _star.Paint(StarColor);
                _star.Time = 0f;
            }

            _star.Root.gameObject.SetActive(true);
            _star.Gate = at;
            _star.Target = at;
        }

        public void Dance(Vector2 gate, Color[] colors, Vector2[] spots, float size, float seconds)
        {
            _danceLeft = seconds;
            for (int i = 0; i < colors.Length; i++)
            {
                Crab crab = _pool.Count > 0 ? _pool.Pop() : Make();
                crab.Root.gameObject.SetActive(true);
                crab.Root.position = gate;
                crab.Gate = gate;
                crab.Size = size;
                crab.Facing = i % 2 == 0 ? 1f : -1f;
                crab.Root.localScale = Vector3.zero;
                crab.Target = spots[i];
                crab.Time = -i * 0.07f;
                crab.Phase = i * 0.9f;
                crab.Paint(colors[i]);
                _dancing.Add(crab);
            }
        }

        public void Send(Vector2 from, SpriteRenderer pebble, Vector2 gate, Color color, float size)
        {
            Crab crab = _pool.Count > 0 ? _pool.Pop() : Make();
            crab.Root.gameObject.SetActive(true);

            float lane = ((_sent * 7) % 5 - 2) * size * 0.16f;
            crab.Root.position = from + new Vector2(lane, 0f);
            crab.Size = size;
            crab.Facing = 1f;
            crab.Root.localScale = new Vector3(size, size, 1f);
            crab.Target = pebble.transform.position;
            crab.Gate = gate + new Vector2(lane * 0.6f, 0f);
            crab.Pebble = pebble;
            crab.Leg = Leg.Out;
            crab.Time = 0f;
            crab.Phase = _sent++ * 1.7f % (Mathf.PI * 2f);
            crab.Paint(color);
            _walking.Add(crab);
        }

        public void Update(float dt, float speed)
        {
            if (_star != null && _star.Root.gameObject.activeSelf)
            {
                Dance(_star, dt, 100f);
            }

            for (int i = _walking.Count - 1; i >= 0; i--)
            {
                Crab crab = _walking[i];
                if (!Advance(crab, dt, speed))
                {
                    crab.Root.gameObject.SetActive(false);
                    _walking.RemoveAt(i);
                    _pool.Push(crab);
                }
            }

            if (_dancing.Count == 0)
            {
                return;
            }

            _danceLeft -= dt * speed;
            foreach (Crab crab in _dancing)
            {
                Dance(crab, dt * speed, _danceLeft);
            }

            if (_danceLeft <= 0f)
            {
                foreach (Crab crab in _dancing)
                {
                    crab.Root.gameObject.SetActive(false);
                    _pool.Push(crab);
                }

                _dancing.Clear();
            }
        }

        public void Clear()
        {
            foreach (Crab crab in _walking)
            {
                if (crab.Carrying)
                {
                    Object.Destroy(crab.Pebble.gameObject);
                }

                crab.Root.gameObject.SetActive(false);
                _pool.Push(crab);
            }

            foreach (Crab crab in _dancing)
            {
                crab.Root.gameObject.SetActive(false);
                _pool.Push(crab);
            }

            _walking.Clear();
            _dancing.Clear();
        }

        public void Dispose()
        {
            if (_holder != null)
            {
                Object.Destroy(_holder.gameObject);
            }

            if (_shadow != null)
            {
                Object.Destroy(_shadow.texture);
                Object.Destroy(_shadow);
            }
        }

        private static void Dance(Crab crab, float dt, float left)
        {
            crab.Time += dt;
            if (crab.Time < 0f)
            {
                return;
            }

            float grow = Mathf.Clamp01(crab.Time / 0.15f) * Mathf.Clamp01(left / 0.2f);
            float walk = Mathf.Clamp01(crab.Time / 0.35f);
            float beat = crab.Time * 7f + crab.Phase;
            float sway = Mathf.Sin(beat * 0.5f) * crab.Size * 0.12f;
            var at = Vector2.Lerp(crab.Gate, crab.Target, walk * walk * (3f - 2f * walk));
            crab.Root.position = at + new Vector2(sway * walk, 0f);
            crab.Facing = Mathf.Cos(beat * 0.5f) < 0f ? -1f : 1f;
            crab.Phase += dt * 9f;
            crab.Stride();
            crab.Body.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(beat)) * 0.16f, 0f);
            crab.Wave(beat);
            crab.Root.localScale = new Vector3(crab.Size * crab.Facing, crab.Size, 1f) * grow;
        }

        private bool Advance(Crab crab, float dt, float speed)
        {
            float step = Speed * speed * dt;
            switch (crab.Leg)
            {
                case Leg.Out:
                    crab.Pose(0.3f);
                    if (Walk(crab, crab.Target, step))
                    {
                        crab.Leg = Leg.Lift;
                        crab.Time = 0f;
                        crab.Take(SortingOrder.CarriedPebble, PebbleHeight);
                        Lifted?.Invoke(crab.Target);
                    }

                    return true;
                case Leg.Lift:
                    crab.Time += dt * speed;
                    float lift = Mathf.Clamp01(crab.Time / LiftSeconds);
                    crab.Pose(0.3f + 0.7f * lift);
                    crab.Lift(lift);
                    if (lift >= 1f)
                    {
                        crab.Leg = Leg.Home;
                    }

                    return true;
                case Leg.Home:
                    crab.Pose(1f);
                    if (Walk(crab, crab.Gate, step))
                    {
                        crab.Leg = Leg.In;
                        crab.Time = 0f;
                        Entered?.Invoke(crab.Gate);
                    }

                    return true;
                default:
                    crab.Time += dt * speed;
                    float gone = Mathf.Clamp01(crab.Time / EnterSeconds);
                    crab.Root.localScale = new Vector3(crab.Size * crab.Facing, crab.Size, 1f) * (1f - gone);
                    if (gone < 1f)
                    {
                        return true;
                    }

                    crab.Drop();
                    return false;
            }
        }

        private static bool Walk(Crab crab, Vector2 to, float step)
        {
            var at = (Vector2)crab.Root.position;
            Vector2 way = to - at;
            float distance = way.magnitude;
            float moved = Mathf.Min(step, distance);
            if (distance > 0.0001f)
            {
                crab.Root.position = at + way / distance * moved;
                if (Mathf.Abs(way.x) > 0.01f)
                {
                    crab.Facing = way.x < 0f ? -1f : 1f;
                }
            }

            crab.Phase += moved / Mathf.Max(crab.Size, 0.01f) * StepsPerUnit;
            crab.Stride();
            return distance <= step;
        }

        private Crab Make()
        {
            var crab = new Crab();
            crab.Root = new GameObject("Crab").transform;
            crab.Root.SetParent(_holder, false);

            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(crab.Root, false);
            shadow.transform.localPosition = new Vector3(0f, -0.3f, 0f);
            shadow.transform.localScale = new Vector3(0.8f, 0.26f, 1f);
            SpriteRenderer shade = shadow.AddComponent<SpriteRenderer>();
            shade.sprite = _shadow;
            shade.sharedMaterial = _shadowMaterial;
            shade.color = new Color(0.02f, 0.04f, 0.12f, 0.42f);
            shade.sortingOrder = SortingOrder.CrabShadow;

            crab.Body = new GameObject("Body").transform;
            crab.Body.SetParent(crab.Root, false);
            Part(_shell, crab.Body, new Vector2(0f, 0.1f), 0.78f, SortingOrder.CrabShell);
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? -1f : 1f;
                crab.Claws[side] = Joint(crab.Body, new Vector2(0.25f * sign, 0f), sign);
                Part(_claw, crab.Claws[side], new Vector2(0.05f, 0.14f), 0.3f, SortingOrder.CrabClaw);
                for (int i = 0; i < 3; i++)
                {
                    Transform hip = Joint(crab.Body, new Vector2(0.17f * sign, -0.14f - 0.07f * i), sign);
                    Part(_leg, hip, new Vector2(0.09f, -0.08f), 0.2f, SortingOrder.CrabLeg);
                    crab.Legs[side * 3 + i] = hip;
                }
            }

            crab.Root.gameObject.SetActive(false);
            return crab;
        }

        internal static Sprite SoftOval()
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

        private static Transform Joint(Transform parent, Vector2 at, float sign)
        {
            Transform joint = new GameObject("Joint").transform;
            joint.SetParent(parent, false);
            joint.localPosition = at;
            joint.localScale = new Vector3(sign, 1f, 1f);
            return joint;
        }

        private void Part(Sprite sprite, Transform parent, Vector2 at, float height, int order)
        {
            var part = new GameObject(sprite.name);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = at;
            float scale = height / Mathf.Max(sprite.bounds.size.y, 0.0001f);
            part.transform.localScale = new Vector3(scale, scale, 1f);
            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = _crewMaterial;
            renderer.sortingOrder = order;
        }
    }
}
