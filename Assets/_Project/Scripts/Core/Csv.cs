using System;
using System.Collections.Generic;
using System.Text;

namespace Chromacolony.Foundation
{
    public static class Csv
    {
        public static List<List<string>> Parse(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;
            var fieldStarted = false;
            var i = text.Length > 0 && text[0] == '﻿' ? 1 : 0;
            for (; i < text.Length; i++)
            {
                var c = text[i];
                if (inQuotes)
                {
                    if (c != '"')
                    {
                        field.Append(c);
                    }
                    else if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }

                    continue;
                }

                switch (c)
                {
                    case '"' when !fieldStarted:
                        inQuotes = true;
                        fieldStarted = true;
                        break;
                    case ',':
                        row.Add(field.ToString());
                        field.Clear();
                        fieldStarted = false;
                        break;
                    case '\r':
                        break;
                    case '\n':
                        row.Add(field.ToString());
                        field.Clear();
                        fieldStarted = false;
                        rows.Add(row);
                        row = new List<string>();
                        break;
                    default:
                        field.Append(c);
                        fieldStarted = true;
                        break;
                }
            }

            if (inQuotes)
            {
                throw new FormatException("a quoted field has no closing quote");
            }

            if (fieldStarted || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }

            return rows;
        }
    }
}
