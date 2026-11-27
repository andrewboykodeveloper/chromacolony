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
            public readonly CrabRig Rig;
            public readonly Transform Root;
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

            public Transform Body => Rig.Body;

            public Crab(CrabRig rig)
            {
                Rig = rig;
                Root = rig.transform;
            }

            public void Paint(Color color)
            {
                Rig.Paint(color);
            }

            public void Stride()
            {
                for (int i = 0; i < Rig.Legs.Count; i++)
                {
                    float tripod = (i + i / LegsPerSide) % 2 == 0 ? 0f : Mathf.PI;
                    float swing = Mathf.Sin(Phase + tripod) * LegSwingDegrees;
                    Rig.Legs[i].localRotation = Quaternion.Euler(0f, 0f, LegRestDegrees + swing - LegSpreadDegrees * (i % LegsPerSide));
                }

                Body.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(Phase)) * BodyBob, 0f);
                Body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Phase) * BodyRockDegrees);
                Root.localScale = new Vector3(Size * Facing, Size, 1f);
            }

            public void Pose(float raise)
            {
                float wave = Mathf.Sin(Phase * ClawWavePerStride) * ClawWaveDegrees * (1f - raise);
                float claw = ClawRestDegrees - ClawRaiseDegrees * raise;
                Rig.Claws[0].localRotation = Quaternion.Euler(0f, 0f, claw + wave);
                Rig.Claws[1].localRotation = Quaternion.Euler(0f, 0f, claw - wave);
            }

            public void Wave(float beat)
            {
                float swing = Mathf.Sin(beat) * DanceClawSwingDegrees;
                Rig.Claws[0].localRotation = Quaternion.Euler(0f, 0f, DanceClawDegrees + swing);
                Rig.Claws[1].localRotation = Quaternion.Euler(0f, 0f, DanceClawDegrees - swing);
            }

            public void Take(int order, float height)
            {
                Carrying = true;
                Pebble.transform.SetParent(Root, true);
                Pebble.sortingOrder = order;
                _pebbleFrom = Pebble.transform.localPosition;
                _pebbleScaleFrom = Pebble.transform.localScale;
                float s = SpriteRenderers.HeightScale(Pebble.sprite, height).y;
                _pebbleScale = new Vector3(Mathf.Sign(_pebbleScaleFrom.x) * s, s, 1f);
            }

            public void Lift(float lift)
            {
                Pebble.transform.localPosition = Vector3.Lerp(_pebbleFrom, new Vector3(0f, CarryY, 0f), lift);
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
        private const float CarryY = 0.62f;

        private const float StarSize = 2.9f;

        private const int LegsPerSide = 3;
        private const float LegRestDegrees = -10f;
        private const float LegSpreadDegrees = 12f;
        private const float LegSwingDegrees = 22f;
        private const float BodyBob = 0.04f;
        private const float BodyRockDegrees = 4f;
        private const float ClawRestDegrees = 35f;
        private const float ClawRaiseDegrees = 45f;
        private const float ClawWaveDegrees = 6f;
        private const float ClawWavePerStride = 0.5f;
        private const float WalkingRaise = 0.3f;

        // Crabs set off one after another, and the lanes keep them from walking in a single file.
        private const int Lanes = 5;
        private const int LaneStride = 7;
        private const float LaneWidth = 0.16f;
        private const float GateLaneShare = 0.6f;
        private const float SendPhaseStep = 1.7f;

        private const float DanceStaggerSeconds = 0.07f;
        private const float DancePhaseStep = 0.9f;
        private const float DanceGrowSeconds = 0.15f;
        private const float DanceShrinkSeconds = 0.2f;
        private const float DanceWalkSeconds = 0.35f;
        private const float DanceBeatRate = 7f;
        private const float DanceSwayPerBeat = 0.5f;
        private const float DanceSway = 0.12f;
        private const float DanceStrideRate = 9f;
        private const float DanceHop = 0.16f;
        private const float DanceClawDegrees = -12f;
        private const float DanceClawSwingDegrees = 28f;

        private const float TurnDeadZone = 0.01f;

        // Guard the divisions by a distance and by a size.
        private const float MinDistance = 0.0001f;
        private const float MinSize = 0.01f;

        private readonly Transform _holder;
        private readonly CrabRig _prefab;
        private readonly Sprite _shadow;
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

        public CrabView(CrabRig prefab, Sprite shadow)
        {
            _holder = new GameObject("Crabs").transform;
            _prefab = prefab;
            _shadow = shadow;
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
                crab.Time = -i * DanceStaggerSeconds;
                crab.Phase = i * DancePhaseStep;
                crab.Paint(colors[i]);
                _dancing.Add(crab);
            }
        }

        public void Send(Vector2 from, SpriteRenderer pebble, Vector2 gate, Color color, float size)
        {
            Crab crab = _pool.Count > 0 ? _pool.Pop() : Make();
            crab.Root.gameObject.SetActive(true);

            float lane = ((_sent * LaneStride) % Lanes - Lanes / 2) * size * LaneWidth;
            crab.Root.position = from + new Vector2(lane, 0f);
            crab.Size = size;
            crab.Facing = 1f;
            crab.Root.localScale = new Vector3(size, size, 1f);
            crab.Target = pebble.transform.position;
            crab.Gate = gate + new Vector2(lane * GateLaneShare, 0f);
            crab.Pebble = pebble;
            crab.Leg = Leg.Out;
            crab.Time = 0f;
            crab.Phase = _sent++ * SendPhaseStep % (Mathf.PI * 2f);
            crab.Paint(color);
            _walking.Add(crab);
        }

        public void Update(float dt, float speed)
        {
            if (_star != null && _star.Root.gameObject.activeSelf)
            {
                Dance(_star, dt, endScale: 1f);
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
            float endScale = Mathf.Clamp01(_danceLeft / DanceShrinkSeconds);
            foreach (Crab crab in _dancing)
            {
                Dance(crab, dt * speed, endScale);
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
        }

        private static void Dance(Crab crab, float dt, float endScale)
        {
            crab.Time += dt;
            if (crab.Time < 0f)
            {
                return;
            }

            float grow = Mathf.Clamp01(crab.Time / DanceGrowSeconds) * endScale;
            float walk = Mathf.Clamp01(crab.Time / DanceWalkSeconds);
            float beat = crab.Time * DanceBeatRate + crab.Phase;
            float sway = Mathf.Sin(beat * DanceSwayPerBeat) * crab.Size * DanceSway;
            var at = Vector2.Lerp(crab.Gate, crab.Target, walk * walk * (3f - 2f * walk));
            crab.Root.position = at + new Vector2(sway * walk, 0f);
            crab.Facing = Mathf.Cos(beat * DanceSwayPerBeat) < 0f ? -1f : 1f;
            crab.Phase += dt * DanceStrideRate;
            crab.Stride();
            crab.Body.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(beat)) * DanceHop, 0f);
            crab.Wave(beat);
            crab.Root.localScale = new Vector3(crab.Size * crab.Facing, crab.Size, 1f) * grow;
        }

        private bool Advance(Crab crab, float dt, float speed)
        {
            float step = Speed * speed * dt;
            switch (crab.Leg)
            {
                case Leg.Out:
                    crab.Pose(WalkingRaise);
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
                    crab.Pose(Mathf.Lerp(WalkingRaise, 1f, lift));
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
            if (distance > MinDistance)
            {
                crab.Root.position = at + way / distance * moved;
                if (Mathf.Abs(way.x) > TurnDeadZone)
                {
                    crab.Facing = way.x < 0f ? -1f : 1f;
                }
            }

            crab.Phase += moved / Mathf.Max(crab.Size, MinSize) * StepsPerUnit;
            crab.Stride();
            return distance <= step;
        }

        private Crab Make()
        {
            CrabRig rig = Object.Instantiate(_prefab, _holder);
            rig.Shadow.sprite = _shadow;
            rig.gameObject.SetActive(false);
            return new Crab(rig);
        }
    }
}
