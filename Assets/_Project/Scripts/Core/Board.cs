using System;
using System.Collections.Generic;
using System.Globalization;

namespace Chromacolony.Core
{
    public static class Palette
    {
        public const string Letters = "ROYGTBPK";

        public static int Count => Letters.Length;

        public static int Of(char letter)
        {
            return Letters.IndexOf(letter);
        }
    }

    public sealed class CrewSpec
    {
        public CrewSpec(int color, int crabs, int link = -1, bool hidden = false)
        {
            Color = color;
            Crabs = crabs;
            Link = link;
            Hidden = hidden;
        }

        public int Color { get; }

        public int Crabs { get; }

        public int Link { get; }

        public bool Hidden { get; }
    }

    public sealed class BoardSpec
    {
        public BoardSpec(string name, int width, int height, IReadOnlyList<int> cells, IReadOnlyList<IReadOnlyList<CrewSpec>> columns)
        {
            Name = name;
            Width = width;
            Height = height;
            Cells = cells;
            Columns = columns;
        }

        public string Name { get; }

        public int Width { get; }

        public int Height { get; }

        public IReadOnlyList<int> Cells { get; }

        public IReadOnlyList<IReadOnlyList<CrewSpec>> Columns { get; }
    }

    public static class BoardText
    {
        public static IReadOnlyList<BoardSpec> Parse(string text)
        {
            var boards = new List<BoardSpec>();
            string name = null;
            var rows = new List<string>();
            var columns = new List<IReadOnlyList<CrewSpec>>();
            var lines = text.Replace("\r\n", "\n").Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                if (line.StartsWith("board ", StringComparison.Ordinal))
                {
                    if (name != null)
                    {
                        boards.Add(Build(name, rows, columns));
                    }

                    name = line.Substring(6).Trim();
                    rows = new List<string>();
                    columns = new List<IReadOnlyList<CrewSpec>>();
                    continue;
                }

                if (name == null)
                {
                    throw new FormatException($"line {i + 1}: no \"board\" line before it");
                }

                if (line.StartsWith("=", StringComparison.Ordinal))
                {
                    columns.Add(Column(name, columns.Count + 1, line.Substring(1)));
                }
                else if (columns.Count > 0)
                {
                    throw new FormatException($"board {name}: a mosaic row after its queue, line {i + 1}");
                }
                else
                {
                    rows.Add(line);
                }
            }

            if (name != null)
            {
                boards.Add(Build(name, rows, columns));
            }

            return boards;
        }

        private static IReadOnlyList<CrewSpec> Column(string board, int column, string line)
        {
            var crews = new List<CrewSpec>();
            foreach (var token in line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var rest = token;
                var hidden = rest.StartsWith("?", StringComparison.Ordinal);
                if (hidden)
                {
                    rest = rest.Substring(1);
                }

                var link = -1;
                var tilde = rest.IndexOf('~');
                if (tilde >= 0)
                {
                    link = Number(board, token, rest.Substring(tilde + 1));
                    rest = rest.Substring(0, tilde);
                }

                var color = rest.Length > 0 ? Palette.Of(rest[0]) : -1;
                if (color < 0)
                {
                    throw new FormatException($"board {board}: no colour in the crew \"{token}\"");
                }

                var crabs = Number(board, token, rest.Substring(1));
                if (crabs <= 0)
                {
                    throw new FormatException($"board {board}: the crew \"{token}\" has no crabs");
                }

                crews.Add(new CrewSpec(color, crabs, link, hidden));
            }

            if (crews.Count == 0)
            {
                throw new FormatException($"board {board}: column {column} of its queue has no crews");
            }

            return crews;
        }

        private static int Number(string board, string token, string text)
        {
            if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            {
                throw new FormatException($"board {board}: no number in the crew \"{token}\"");
            }

            return value;
        }

        private static BoardSpec Build(string name, List<string> rows, List<IReadOnlyList<CrewSpec>> columns)
        {
            if (rows.Count == 0 || columns.Count == 0)
            {
                throw new FormatException($"board {name}: it needs a mosaic and a queue");
            }

            var width = rows[0].Length;
            var cells = new int[width * rows.Count];
            var pebbles = new int[Palette.Count];
            for (var y = 0; y < rows.Count; y++)
            {
                if (rows[y].Length != width)
                {
                    throw new FormatException($"board {name}: row {y + 1} is {rows[y].Length} wide, not {width}");
                }

                for (var x = 0; x < width; x++)
                {
                    var c = rows[y][x];
                    var color = c == '.' ? -1 : Palette.Of(c);
                    if (c != '.' && color < 0)
                    {
                        throw new FormatException($"board {name}: no colour \"{c}\" in row {y + 1}");
                    }

                    cells[y * width + x] = color;
                    if (color >= 0)
                    {
                        pebbles[color]++;
                    }
                }
            }

            var crabs = new int[Palette.Count];
            var ropes = new Dictionary<int, List<int>>();
            for (var c = 0; c < columns.Count; c++)
            {
                foreach (var crew in columns[c])
                {
                    crabs[crew.Color] += crew.Crabs;
                    if (crew.Link < 0)
                    {
                        continue;
                    }

                    if (!ropes.TryGetValue(crew.Link, out var tied))
                    {
                        ropes[crew.Link] = tied = new List<int>();
                    }

                    tied.Add(c);
                }
            }

            for (var color = 0; color < Palette.Count; color++)
            {
                if (pebbles[color] != crabs[color])
                {
                    throw new FormatException($"board {name}: {pebbles[color]} pebbles of {Palette.Letters[color]} and {crabs[color]} crabs for them");
                }
            }

            foreach (var rope in ropes)
            {
                if (rope.Value.Count != 2 || rope.Value[0] == rope.Value[1])
                {
                    throw new FormatException($"board {name}: the rope ~{rope.Key} must tie two crews of two columns");
                }
            }

            return new BoardSpec(name, width, rows.Count, cells, columns);
        }
    }
}
