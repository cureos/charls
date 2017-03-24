// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;

namespace CharLS
{
    public class FromBigEndian
    {
        public static ulong Read(int size, byte[] pbyte)
        {
            switch (size)
            {
                case 4:
                    return (uint)((pbyte[0] << 24) + (pbyte[1] << 16) + (pbyte[2] << 8) + (pbyte[3] << 0));
                case 8:
                    return ((ulong)pbyte[0] << 56) + ((ulong)pbyte[1] << 48) + ((ulong)pbyte[2] << 40)
                           + ((ulong)pbyte[3] << 32) + ((ulong)pbyte[4] << 24) + ((ulong)pbyte[5] << 16)
                           + ((ulong)pbyte[6] << 8) + ((ulong)pbyte[7] << 0);
                default:
                    throw new NotSupportedException($"Invalid type size: {size}");
            }
        }
    }
}
