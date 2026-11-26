using System.Collections.Generic;

namespace Chromacolony.Core
{
    public sealed class BoardSpec
    {
        public string Name { get; }

        public int Width { get; }

        public int Height { get; }

        public IReadOnlyList<int> Cells { get; }

        public IReadOnlyList<IReadOnlyList<CrewSpec>> Columns { get; }

        public BoardSpec(string name, int width, int height, IReadOnlyList<int> cells, IReadOnlyList<IReadOnlyList<CrewSpec>> columns)
        {
            Name = name;
            Width = width;
            Height = height;
            Cells = cells;
            Columns = columns;
        }
    }
}
