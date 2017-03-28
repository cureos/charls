// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Collections.Generic;

namespace CharLS
{
    internal static class util
    {
        // Default bin sizes for JPEG-LS statistical modeling. Can be overriden at compression time, however this is rarely done.
        private const int BASIC_T1 = 3;

        private const int BASIC_T2 = 7;

        private const int BASIC_T3 = 21;

        internal const int BASIC_RESET = 64; // Default value as defined in ITU T.87, table C.2

        internal static readonly int INT32_BITCOUNT = sizeof(int) * 8;

        internal static void push_back(IList<byte> values, ushort value)
        {
            values.Add((byte)(value / 0x100));
            values.Add((byte)(value % 0x100));
        }

        internal static int log_2(int n)
        {
            var x = 0;
            while (n > 1 << x)
            {
                ++x;
            }
            return x;
        }

        internal static int Sign(int n)
        {
            return (n >> (INT32_BITCOUNT - 1)) | 1;
        }

        internal static int BitWiseSign(int i)
        {
            return i >> (INT32_BITCOUNT - 1);
        }

        internal static JpegLSPresetCodingParameters ComputeDefault(int MAXVAL, int NEAR)
        {
            JpegLSPresetCodingParameters preset = new JpegLSPresetCodingParameters();

            int FACTOR = (Math.Min(MAXVAL, 4095) + 128) / 256;

            preset.Threshold1 = CLAMP(FACTOR * (BASIC_T1 - 2) + 2 + 3 * NEAR, NEAR + 1, MAXVAL);
            preset.Threshold2 = CLAMP(FACTOR * (BASIC_T2 - 3) + 3 + 5 * NEAR, preset.Threshold1, MAXVAL);
            preset.Threshold3 = CLAMP(FACTOR * (BASIC_T3 - 4) + 4 + 7 * NEAR, preset.Threshold2, MAXVAL);
            preset.MaximumSampleValue = MAXVAL;
            preset.ResetValue = BASIC_RESET;
            return preset;
        }

        private static int CLAMP(int i, int j, int MAXVAL)
        {
            if (i > MAXVAL || i < j)
                return j;

            return i;
        }

        public static void SkipBytes(ByteStreamInfo streamInfo, long count)
        {
            streamInfo.Seek(count);
        }
    }
}
