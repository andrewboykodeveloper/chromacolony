using System;
using System.Collections.Generic;

namespace Chromacolony.Core
{
    public sealed class Mosaic
    {
        private readonly int[] _cells;
        private readonly bool[] _open;
        private readonly Queue<int> _flood = new Queue<int>();

        public Mosaic(int width, int height, IReadOnlyList<int> cells)
        {
            Width = width;
            Height = height;
            _cells = new int[width * height];
            _open = new bool[width * height];
            for (var i = 0; i < _cells.Length; i++)
            {
                _cells[i] = cells[i];
                if (cells[i] >= 0)
                {
                    Left++;
                }
            }

            for (var i = 0; i < _cells.Length; i++)
            {
                if (_cells[i] < 0 && OnEdge(i))
                {
                    Flood(i);
                }
            }
        }

        private Mosaic(Mosaic other)
        {
            Width = other.Width;
            Height = other.Height;
            _cells = (int[])other._cells.Clone();
            _open = (bool[])other._open.Clone();
            Left = other.Left;
        }

        public int Width { get; }

        public int Height { get; }

        public int Left { get; private set; }

        public int ColorAt(int cell)
        {
            return _cells[cell];
        }

        public Mosaic Copy()
        {
            return new Mosaic(this);
        }

        public bool InReach(int cell)
        {
            if (_cells[cell] < 0)
            {
                return false;
            }

            if (OnEdge(cell))
            {
                return true;
            }

            var x = cell % Width;
            return _open[cell - Width] || _open[cell + Width] || (x > 0 && _open[cell - 1]) || (x < Width - 1 && _open[cell + 1]);
        }

        public int Nearest(int color)
        {
            var best = -1;
            var bestDistance = int.MaxValue;
            for (var i = 0; i < _cells.Length; i++)
            {
                if (_cells[i] != color || !InReach(i))
                {
                    continue;
                }

                var distance = Distance(i);
                if (distance < bestDistance)
                {
                    best = i;
                    bestDistance = distance;
                }
            }

            return best;
        }

        public bool AnyInReach(int color)
        {
            return Nearest(color) >= 0;
        }

        public void Take(int cell)
        {
            if (!InReach(cell))
            {
                throw new InvalidOperationException($"the pebble at {cell} is out of reach");
            }

            _cells[cell] = -1;
            Left--;
            Flood(cell);
        }

        private int Distance(int cell)
        {
            var dx = 2 * (cell % Width) + 1 - Width;
            var dy = 2 * Height + 1 - (2 * (cell / Width) + 1);
            return dx * dx + dy * dy;
        }

        private bool OnEdge(int cell)
        {
            var x = cell % Width;
            var y = cell / Width;
            return x == 0 || y == 0 || x == Width - 1 || y == Height - 1;
        }

        private void Flood(int start)
        {
            if (_open[start])
            {
                return;
            }

            _open[start] = true;
            _flood.Enqueue(start);
            while (_flood.Count > 0)
            {
                var cell = _flood.Dequeue();
                var x = cell % Width;
                if (x > 0)
                {
                    Open(cell - 1);
                }

                if (x < Width - 1)
                {
                    Open(cell + 1);
                }

                if (cell >= Width)
                {
                    Open(cell - Width);
                }

                if (cell + Width < _cells.Length)
                {
                    Open(cell + Width);
                }
            }
        }

        private void Open(int cell)
        {
            if (_cells[cell] < 0 && !_open[cell])
            {
                _open[cell] = true;
                _flood.Enqueue(cell);
            }
        }
    }
}
