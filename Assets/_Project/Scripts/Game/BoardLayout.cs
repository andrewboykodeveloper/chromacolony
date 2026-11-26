using UnityEngine;

namespace Chromacolony.Game
{
    public sealed class BoardLayout
    {
        public const float HalfWidth = 5.4f;
        public const float HalfHeight = 9.6f;
        public const int VisibleDepth = 3;
        private const float MosaicCenterY = 4.48f;
        private const float MosaicMaxWidth = 9.4f;
        private const float MosaicMaxHeight = 6.3f;

        private const float CastleY = 0.22f;
        private const float SlotsY = -1.75f;
        private const float QueueTopY = -3.15f;
        private const float QueueStep = 1.2f;
        private const float RowWidth = 9.8f;

        private readonly int _width;
        private readonly int _height;
        private readonly int _slots;
        private readonly int _columns;

        public float Cell { get; }

        public Vector2 MosaicSize => new Vector2(_width * Cell, _height * Cell);

        public Vector2 MosaicCenter => new Vector2(0f, MosaicCenterY);

        public Vector2 CastleCenter => new Vector2(0f, CastleY);

        public Vector2 TraySize => MosaicSize + new Vector2(0.6f, 0.6f);

        public Vector2 Gate => CastleCenter + new Vector2(0f, -0.35f);

        public float CastleHeight => 1.5f;

        public float SlotSize => Mathf.Min(1.7f, RowWidth / _slots * 0.92f);

        public float BucketSize => Mathf.Min(1.25f, RowWidth / Mathf.Max(_columns, 1) * 0.7f);

        public BoardLayout(int width, int height, int slots, int columns)
        {
            _width = width;
            _height = height;
            _slots = slots;
            _columns = columns;
            Cell = Mathf.Min(MosaicMaxWidth / width, MosaicMaxHeight / height);
        }

        public Vector2 PebbleAt(int cell)
        {
            int x = cell % _width;
            int y = cell / _width;
            Vector2 size = MosaicSize;
            return new Vector2(-size.x / 2f + (x + 0.5f) * Cell, MosaicCenterY + size.y / 2f - (y + 0.5f) * Cell);
        }

        public Vector2 SlotAt(int slot)
        {
            float step = RowWidth / _slots;
            return new Vector2(-RowWidth / 2f + step * (slot + 0.5f), SlotsY);
        }

        public Vector2 QueueAt(int column, int depth)
        {
            float step = Mathf.Min(2.2f, RowWidth / Mathf.Max(_columns, 1));
            float x = -step * (_columns - 1) / 2f + step * column;
            return new Vector2(x, QueueTopY - QueueStep * depth);
        }

        public float QueueScale(int depth)
        {
            return Mathf.Max(0.6f, 1f - 0.14f * depth);
        }

        /// <summary>
        /// The column whose visible crews a tap at this point is on, or -1. A tap on a crew behind the front one still counts for its
        /// column.
        /// </summary>
        public int ColumnUnder(Vector2 world)
        {
            float top = QueueAt(0, 0).y + BucketSize * 0.75f;
            float bottom = QueueAt(0, VisibleDepth - 1).y - BucketSize * 0.75f;
            if (world.y > top || world.y < bottom)
            {
                return -1;
            }

            int best = -1;
            float bestDistance = float.MaxValue;
            for (int c = 0; c < _columns; c++)
            {
                float distance = Mathf.Abs(world.x - QueueAt(c, 0).x);
                if (distance < bestDistance && distance < BucketSize)
                {
                    best = c;
                    bestDistance = distance;
                }
            }

            return best;
        }

        public bool SlotCovers(int slot, Vector2 world)
        {
            return Vector2.Distance(world, SlotAt(slot)) < SlotSize * 0.6f;
        }
    }
}
