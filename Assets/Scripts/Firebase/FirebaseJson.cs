using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ClickerAdventure
{
    public static class FirebaseJson
    {
        public static object Parse(string json)
        {
            return new Parser(json).ParseValue();
        }

        public static Dictionary<string, object> ParseObject(string json)
        {
            return Parse(json) as Dictionary<string, object> ?? new Dictionary<string, object>();
        }

        public static string Serialize(object value)
        {
            var builder = new StringBuilder();
            WriteValue(builder, value);
            return builder.ToString();
        }

        private static void WriteValue(StringBuilder builder, object value)
        {
            switch (value)
            {
                case null:
                    builder.Append("null");
                    break;
                case string text:
                    WriteString(builder, text);
                    break;
                case bool boolean:
                    builder.Append(boolean ? "true" : "false");
                    break;
                case int or long or float or double:
                    builder.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                    break;
                case IDictionary dictionary:
                    WriteObject(builder, dictionary);
                    break;
                case IEnumerable enumerable:
                    WriteArray(builder, enumerable);
                    break;
                default:
                    WriteString(builder, value.ToString());
                    break;
            }
        }

        private static void WriteObject(StringBuilder builder, IDictionary dictionary)
        {
            builder.Append('{');
            var first = true;
            foreach (DictionaryEntry entry in dictionary)
            {
                if (!first)
                {
                    builder.Append(',');
                }

                WriteString(builder, entry.Key.ToString());
                builder.Append(':');
                WriteValue(builder, entry.Value);
                first = false;
            }

            builder.Append('}');
        }

        private static void WriteArray(StringBuilder builder, IEnumerable enumerable)
        {
            builder.Append('[');
            var first = true;
            foreach (var item in enumerable)
            {
                if (!first)
                {
                    builder.Append(',');
                }

                WriteValue(builder, item);
                first = false;
            }

            builder.Append(']');
        }

        private static void WriteString(StringBuilder builder, string value)
        {
            builder.Append('"');
            foreach (var c in value)
            {
                switch (c)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\b':
                        builder.Append("\\b");
                        break;
                    case '\f':
                        builder.Append("\\f");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (c < ' ')
                        {
                            builder.Append("\\u");
                            builder.Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            builder.Append(c);
                        }
                        break;
                }
            }

            builder.Append('"');
        }

        private sealed class Parser
        {
            private readonly string json;
            private int index;

            public Parser(string json)
            {
                this.json = json ?? string.Empty;
            }

            public object ParseValue()
            {
                SkipWhitespace();
                if (index >= json.Length)
                {
                    return null;
                }

                return json[index] switch
                {
                    '{' => ParseObjectInternal(),
                    '[' => ParseArray(),
                    '"' => ParseString(),
                    't' => ParseLiteral("true", true),
                    'f' => ParseLiteral("false", false),
                    'n' => ParseLiteral("null", null),
                    _ => ParseNumber()
                };
            }

            private Dictionary<string, object> ParseObjectInternal()
            {
                var result = new Dictionary<string, object>();
                index++;
                while (true)
                {
                    SkipWhitespace();
                    if (TryConsume('}'))
                    {
                        return result;
                    }

                    var key = ParseString();
                    SkipWhitespace();
                    Consume(':');
                    result[key] = ParseValue();
                    SkipWhitespace();
                    if (TryConsume('}'))
                    {
                        return result;
                    }

                    Consume(',');
                }
            }

            private List<object> ParseArray()
            {
                var result = new List<object>();
                index++;
                while (true)
                {
                    SkipWhitespace();
                    if (TryConsume(']'))
                    {
                        return result;
                    }

                    result.Add(ParseValue());
                    SkipWhitespace();
                    if (TryConsume(']'))
                    {
                        return result;
                    }

                    Consume(',');
                }
            }

            private string ParseString()
            {
                Consume('"');
                var builder = new StringBuilder();
                while (index < json.Length)
                {
                    var c = json[index++];
                    if (c == '"')
                    {
                        return builder.ToString();
                    }

                    if (c != '\\')
                    {
                        builder.Append(c);
                        continue;
                    }

                    if (index >= json.Length)
                    {
                        break;
                    }

                    var escaped = json[index++];
                    builder.Append(escaped switch
                    {
                        '"' => '"',
                        '\\' => '\\',
                        '/' => '/',
                        'b' => '\b',
                        'f' => '\f',
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        'u' => ParseUnicode(),
                        _ => escaped
                    });
                }

                return builder.ToString();
            }

            private char ParseUnicode()
            {
                if (index + 4 > json.Length)
                {
                    return '?';
                }

                var hex = json.Substring(index, 4);
                index += 4;
                return (char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            private object ParseNumber()
            {
                var start = index;
                while (index < json.Length && "-+0123456789.eE".IndexOf(json[index]) >= 0)
                {
                    index++;
                }

                var slice = json[start..index];
                if (slice.IndexOf('.') >= 0 || slice.IndexOf('e') >= 0 || slice.IndexOf('E') >= 0)
                {
                    return double.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0d;
                }

                return long.TryParse(slice, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l) ? l : 0L;
            }

            private object ParseLiteral(string literal, object value)
            {
                if (string.Compare(json, index, literal, 0, literal.Length, StringComparison.Ordinal) == 0)
                {
                    index += literal.Length;
                }

                return value;
            }

            private void SkipWhitespace()
            {
                while (index < json.Length && char.IsWhiteSpace(json[index]))
                {
                    index++;
                }
            }

            private bool TryConsume(char expected)
            {
                if (index < json.Length && json[index] == expected)
                {
                    index++;
                    return true;
                }

                return false;
            }

            private void Consume(char expected)
            {
                SkipWhitespace();
                if (index < json.Length && json[index] == expected)
                {
                    index++;
                }
            }
        }
    }
}
