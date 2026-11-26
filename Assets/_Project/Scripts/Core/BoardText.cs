using System;
using System.Collections.Generic;
using System.Globalization;

namespace Chromacolony.Core
{
    public static class BoardText
    {
        public static IReadOnlyList<BoardSpec> Parse(string text)
        {
            var boards = new List<BoardSpec>();
            string name = null;
            var rows = new List<string>();
            var columns = new List<IReadOnlyList<CrewSpec>>();
            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
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
            foreach (string token in line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string rest = token;
                bool hidden = rest.StartsWith("?", StringComparison.Ordinal);
                if (hidden)
                {
                    rest = rest.Substring(1);
                }

                int link = -1;
                int tilde = rest.IndexOf('~');
                if (tilde >= 0)
                {
                    link = Number(board, token, rest.Substring(tilde + 1));
                    rest = rest.Substring(0, tilde);
                }

                int color = rest.Length > 0 ? Palette.Of(rest[0]) : -1;
                if (color < 0)
                {
                    throw new FormatException($"board {board}: no colour in the crew \"{token}\"");
                }

                int crabs = Number(board, token, rest.Substring(1));
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
            if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int value))
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

            int width = rows[0].Length;
            int[] cells = new int[width * rows.Count];
            int[] pebbles = new int[Palette.Count];
            for (int y = 0; y < rows.Count; y++)
            {
                if (rows[y].Length != width)
                {
                    throw new FormatException($"board {name}: row {y + 1} is {rows[y].Length} wide, not {width}");
                }

                for (int x = 0; x < width; x++)
                {
                    char c = rows[y][x];
                    int color = c == '.' ? -1 : Palette.Of(c);
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

            int[] crabs = new int[Palette.Count];
            var ropes = new Dictionary<int, List<int>>();
            for (int c = 0; c < columns.Count; c++)
            {
                foreach (CrewSpec crew in columns[c])
                {
                    crabs[crew.Color] += crew.Crabs;
                    if (crew.Link < 0)
                    {
                        continue;
                    }

                    if (!ropes.TryGetValue(crew.Link, out List<int> tied))
                    {
                        ropes[crew.Link] = tied = new List<int>();
                    }

                    tied.Add(c);
                }
            }

            for (int color = 0; color < Palette.Count; color++)
            {
                if (pebbles[color] != crabs[color])
                {
                    throw new FormatException($"board {name}: {pebbles[color]} pebbles of {Palette.Letters[color]} and {crabs[color]} crabs for them");
                }
            }

            foreach ((int link, List<int> ends) in ropes)
            {
                if (ends.Count != 2 || ends[0] == ends[1])
                {
                    throw new FormatException($"board {name}: the rope ~{link} must tie two crews of two columns");
                }
            }

            return new BoardSpec(name, width, rows.Count, cells, columns);
        }
    }
}
