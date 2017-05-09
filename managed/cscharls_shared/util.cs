// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Reflection;

namespace CharLS
{
    internal static class util
    {
        internal const int BASIC_RESET = 64; // Default value as defined in ITU T.87, table C.2

        internal const int INT32_BITCOUNT = sizeof(int) * 8;

        internal static bool Implements<T, TBase>()
        {
#if NET20
            return typeof(TBase).IsAssignableFrom(typeof(T));
#else
            return typeof(TBase).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo());
#endif
        }

        internal static int BitWiseSign(int i)
        {
            return i >> (INT32_BITCOUNT - 1);
        }

        internal static JpegLSPresetCodingParameters ComputeDefault(int MAXVAL, int NEAR)
        {
            // Default bin sizes for JPEG-LS statistical modeling. Can be overriden at compression time, however this is rarely done.
            const int BASIC_T1 = 3;
            const int BASIC_T2 = 7;
            const int BASIC_T3 = 21;

            JpegLSPresetCodingParameters preset = new JpegLSPresetCodingParameters();

            int FACTOR = (Math.Min(MAXVAL, 4095) + 128) / 256;

            preset.Threshold1 = CLAMP(FACTOR * (BASIC_T1 - 2) + 2 + 3 * NEAR, NEAR + 1, MAXVAL);
            preset.Threshold2 = CLAMP(FACTOR * (BASIC_T2 - 3) + 3 + 5 * NEAR, preset.Threshold1, MAXVAL);
            preset.Threshold3 = CLAMP(FACTOR * (BASIC_T3 - 4) + 4 + 7 * NEAR, preset.Threshold2, MAXVAL);
            preset.MaximumSampleValue = MAXVAL;
            preset.ResetValue = BASIC_RESET;

            return preset;
        }

        internal static T Delimit<T>(int alpha)
        {
            int min = int.MinValue, max = int.MaxValue;

            var type = typeof(T);
            if (type == typeof(byte))
            {
                min = byte.MinValue;
                max = byte.MaxValue;
            }
            else if (type == typeof(sbyte))
            {
                min = sbyte.MinValue;
                max = sbyte.MaxValue;
            }
            else if (type == typeof(ushort))
            {
                min = ushort.MinValue;
                max = ushort.MaxValue;
            }
            else if (type == typeof(short))
            {
                min = short.MinValue;
                max = short.MaxValue;
            }
            else if (type == typeof(uint))
            {
                min = (int)uint.MinValue;
            }

            return (T)Convert.ChangeType(Math.Min(Math.Max(alpha, min), max), type);
        }

        internal static void Delimit<T>(int x1, int x2, int x3, out T v1, out T v2, out T v3)
        {
            int min = int.MinValue, max = int.MaxValue;

            var type = typeof(T);
            if (type == typeof(byte))
            {
                min = byte.MinValue;
                max = byte.MaxValue;
            }
            else if (type == typeof(sbyte))
            {
                min = sbyte.MinValue;
                max = sbyte.MaxValue;
            }
            else if (type == typeof(ushort))
            {
                min = ushort.MinValue;
                max = ushort.MaxValue;
            }
            else if (type == typeof(short))
            {
                min = short.MinValue;
                max = short.MaxValue;
            }
            else if (type == typeof(uint))
            {
                min = (int)uint.MinValue;
            }

            v1 = (T)Convert.ChangeType(Math.Min(Math.Max(x1, min), max), type);
            v2 = (T)Convert.ChangeType(Math.Min(Math.Max(x2, min), max), type);
            v3 = (T)Convert.ChangeType(Math.Min(Math.Max(x3, min), max), type);
        }

        private static int CLAMP(int i, int j, int MAXVAL)
        {
            return i > MAXVAL || i < j ? j : i;
        }
    }
}
