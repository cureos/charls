// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;

namespace CharLS
{
    public class FromBigEndian
    {
        public static ulong Read(int size, Subarray<byte> bytes)
        {
            switch (size)
            {
                case 4:
                    return (uint)((bytes[0] << 24) + (bytes[1] << 16) + (bytes[2] << 8) + (bytes[3] << 0));
                case 8:
                    return ((ulong)bytes[0] << 56) + ((ulong)bytes[1] << 48) + ((ulong)bytes[2] << 40)
                           + ((ulong)bytes[3] << 32) + ((ulong)bytes[4] << 24) + ((ulong)bytes[5] << 16)
                           + ((ulong)bytes[6] << 8) + ((ulong)bytes[7] << 0);
                default:
                    throw new NotSupportedException($"Invalid type size: {size}");
            }
        }
    }
}
