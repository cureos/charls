// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System.Collections.Generic;

namespace CharLS
{
    internal static class util
    {
        internal static readonly int INT32_BITCOUNT = sizeof(int) * 8;

        internal const int BASIC_RESET = 64; // Default value as defined in ITU T.87, table C.2

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
    }
}
