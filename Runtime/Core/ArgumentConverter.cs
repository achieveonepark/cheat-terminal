using System;
using System.Globalization;
using UnityEngine;

namespace UniTerminal.Core
{
    /// <summary>
    /// Converts string arguments to common primitive, enum and Unity types.
    /// Vectors accept "x,y,z" or "(x,y,z)". Booleans accept true/false/1/0/on/off.
    /// </summary>
    public sealed class ArgumentConverter : IArgumentConverter
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public bool CanConvert(Type type)
        {
            if (type == typeof(string) || type == typeof(bool) || type.IsEnum) return true;
            if (type == typeof(int) || type == typeof(long) || type == typeof(short)
                || type == typeof(byte) || type == typeof(float) || type == typeof(double)) return true;
            if (type == typeof(Vector2) || type == typeof(Vector3)
                || type == typeof(Vector4) || type == typeof(Color)) return true;
            return false;
        }

        public object Convert(string raw, Type type)
        {
            if (type == typeof(string)) return raw;

            if (type == typeof(bool)) return ParseBool(raw);

            if (type.IsEnum)
                return Enum.Parse(type, raw, ignoreCase: true);

            if (type == typeof(int)) return int.Parse(raw, NumberStyles.Integer, Inv);
            if (type == typeof(long)) return long.Parse(raw, NumberStyles.Integer, Inv);
            if (type == typeof(short)) return short.Parse(raw, NumberStyles.Integer, Inv);
            if (type == typeof(byte)) return byte.Parse(raw, NumberStyles.Integer, Inv);
            if (type == typeof(float)) return float.Parse(raw, NumberStyles.Float, Inv);
            if (type == typeof(double)) return double.Parse(raw, NumberStyles.Float, Inv);

            if (type == typeof(Vector2)) { var c = ParseFloats(raw, 2); return new Vector2(c[0], c[1]); }
            if (type == typeof(Vector3)) { var c = ParseFloats(raw, 3); return new Vector3(c[0], c[1], c[2]); }
            if (type == typeof(Vector4)) { var c = ParseFloats(raw, 4); return new Vector4(c[0], c[1], c[2], c[3]); }
            if (type == typeof(Color)) { var c = ParseFloats(raw, 4); return new Color(c[0], c[1], c[2], c[3]); }

            // Last resort.
            return System.Convert.ChangeType(raw, type, Inv);
        }

        private static bool ParseBool(string s)
        {
            if (s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase)
                || s.Equals("on", StringComparison.OrdinalIgnoreCase)) return true;
            if (s == "0" || s.Equals("false", StringComparison.OrdinalIgnoreCase)
                || s.Equals("off", StringComparison.OrdinalIgnoreCase)) return false;
            return bool.Parse(s);
        }

        private static float[] ParseFloats(string raw, int count)
        {
            var trimmed = raw.Trim().TrimStart('(').TrimEnd(')');
            var parts = trimmed.Split(',');
            var result = new float[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = i < parts.Length
                    ? float.Parse(parts[i].Trim(), NumberStyles.Float, Inv)
                    : (count == 4 && i == 3 ? 1f : 0f); // default alpha/w to 1
            }
            return result;
        }
    }
}
